﻿// Copyright © 2015-2022 Alex Kukhtin. All rights reserved.


using System;
using System.Threading.Tasks;
using System.Dynamic;
using System.Text;
using System.Web.Mvc;
using System.Web;
using System.Net.Http.Headers;

using Microsoft.AspNet.Identity;

using Newtonsoft.Json;

using A2v10.Infrastructure;
using A2v10.Request;
using A2v10.Reports;
using A2v10.Web.Identity;
using A2v10.Interop;
using A2v10.Web.Base;

namespace A2v10.Web.Mvc.Controllers
{

	public class DesktopReport
	{
		public String Base;
		public String Report;
		public String Id;
		public String Format;
		public Int64 UserId;
		public Int32 TenantId;
		public Int64 CompanyId;
		public Boolean AddContentDisposition;
	}

	[Authorize]
	[ExecutingFilter]
	[CheckMobileFilter]
	public class ReportController : Controller, IControllerProfiler, IControllerTenant, IControllerLocale
	{
		readonly A2v10.Request.BaseController _baseController = new BaseController();
        ReportHelper _reportHelper = null;
		PdfReportHelper _pdfReportHelper = null;
		private readonly ReportHelperInfo _reportInfo;

		public ReportController()
		{
			_baseController.Host.StartApplication(false);
			_reportInfo = new ReportHelperInfo(_baseController.Host);
		}

		private ReportHelper ReportHelper
		{
			get
			{
				if (_reportHelper == null)
					_reportHelper = new ReportHelper(_baseController.Host);
				return _reportHelper;
			}
		}

		private PdfReportHelper PdfReportHelper
		{
			get
			{
				if (_pdfReportHelper == null)
					_pdfReportHelper = new PdfReportHelper(_baseController.Host);
				return _pdfReportHelper;
			}
		}

		public Int64 UserId => User.Identity.GetUserId<Int64>();
		public Int32 TenantId => User.Identity.GetUserTenantId();
		public String UserSegment => User.Identity.GetUserSegment();
		public Int64 CompanyId => _baseController.UserStateManager.UserCompanyId(TenantId, UserId);
		public IProfiler Profiler => _baseController.Host.Profiler;

		readonly static String[] _internalActions = new String[] { /*"GetReport", for profile query*/ "ViewerEvent", "PrintReport", "ExportReport", "Interaction" };

		public Boolean SkipRequest(String Url)
		{
			foreach (var a in _internalActions) {
				if (Url.Contains($"/{a}/"))
					return true;
			}
			return false;
		}

		[HttpGet]
		public async Task Show(String Base, String Rep, String id)
		{
			try
			{
				var url = $"/_report/{Base.RemoveHeadSlash()}/{Rep}/{id}";


				RequestModel rm = await RequestModel.CreateFromBaseUrl(_baseController.Host, url);
				var rep = rm.GetReport();
				switch (rep.type)
				{
					case RequestReportType.stimulsoft:
						ShowStimulsoft(rep, Rep);
						break;
					case RequestReportType.pdf:
						ReportInfo ri = await GetReportInfo(url, id, CreateParamsFromQueryString());
						await ShowPdf(ri, Rep);
						break;
				}
			} 
			catch (Exception ex)
			{
				Response.ContentType = "text/html";
				Response.ContentEncoding = Encoding.UTF8;
				if (ex.InnerException != null)
					ex = ex.InnerException;
				Response.Write(ex.Message);
			}
		}

		async Task ShowPdf(ReportInfo ri, String repName)
		{
			Response.ContentType = MimeTypes.Application.Pdf;

			using (var ms = PdfReportHelper.Build(ri.ReportPath, ri.DataModel.Root))
			{
				await ms.CopyToAsync(Response.OutputStream);
			}
		}

		void ShowStimulsoft(RequestReport rep, String repName)
		{ 
			ReportHelper.SetupLicense();
			try
			{
				rep.CheckPermissions(_baseController.UserStateManager.GetUserPermissions(), _baseController.Host.IsDebugConfiguration);

				MvcHtmlString result = null;
				using (var pr = Profiler.CurrentRequest.Start(ProfileAction.Report, $"render: {repName}"))
				{
					result  = ReportHelper.ShowViewer(this);
				}

				var sb = new StringBuilder(ResourceHelper.StiReportHtml);
				sb.Replace("$(StiReport)", result.ToHtmlString());
				sb.Replace("$(Lang)", _baseController.CurrentLang);
				sb.Replace("$(Title)", _baseController.Localize(rep.name ?? repName)); 

				Response.Output.Write(sb.ToString());
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
					ex = ex.InnerException;
				Response.Write(ex.Message);
			}
		}

		async Task<ReportInfo> GetReportInfo(String url, String id, ExpandoObject prms)
		{
			var rc = new ReportContext()
			{
				UserId = UserId,
				TenantId = TenantId,
			};
			if (_baseController.Host.IsMultiCompany)
				rc.CompanyId = CompanyId;
			return await _reportInfo.GetReportInfo(rc, url, id, prms);
		}

		async Task<ReportInfo> GetReportInfoDesktop(DesktopReport dr, String url, ExpandoObject prms)
		{
			var rc = new ReportContext()
			{
				UserId = dr.UserId,
				TenantId = dr.TenantId,
			};
			if (_baseController.Host.IsMultiCompany)
				rc.CompanyId = dr.CompanyId;
			return await _reportInfo.GetReportInfo(rc, url, dr.Id, prms);
		}

		ExpandoObject CreateParamsFromQueryString()
		{
			var eo = new ExpandoObject();
			if (Request == null)
				return eo;
			if (Request.QueryString.Count == 0)
				return eo;
			eo.Append(_baseController.CheckPeriod(Request.QueryString), toPascalCase: true);
			eo.RemoveKeys("rep,Rep,base,Base,Format,format");
			return eo;
		}

		public async Task ExportDesktop(DesktopReport rep, HttpResponseBase response)
		{
			// TODO: query string ???
			try
			{
				using (var rr = Profiler.CurrentRequest.Start(ProfileAction.Report, $"export: {rep.Report}"))
				{
					var url = $"/_report/{rep.Base.RemoveHeadSlash()}/{rep.Report}/{rep.Id}";
					ReportInfo ri = await GetReportInfoDesktop(rep, url, CreateParamsFromQueryString());
					ExportReportResult err = null;
					switch (ri.Type)
					{
						case RequestReportType.stimulsoft:
							ReportHelper.SetupLicense();
							err = await ReportHelper.ExportStiReportStreamAsync(ri, rep.Format, response.OutputStream);
							break;
						case RequestReportType.xml:
							throw new NotImplementedException("ExportDesktop. RequestReportType.xml");
						case RequestReportType.json:
							throw new NotImplementedException("ExportDesktop. RequestReportType.json");
					}
					if (err != null)
					{
						response.ContentType = err.ContentType;
						if (rep.AddContentDisposition)
						{
							var cdh = new ContentDispositionHeaderValue("attachment")
							{
								FileNameStar = $"{_baseController.Localize(ri.Name)}.{err.Extension}"
							};
							response.Headers.Add("Content-Disposition", cdh.ToString());
						}
					}

				}
			}
			catch (Exception ex)
			{
				response.ContentType = "text/html";
				response.ContentEncoding = Encoding.UTF8;
				if (ex.InnerException != null)
					ex = ex.InnerException;
				response.Write(ex.Message);
			}
		}

		[HttpGet]
		[OutputCache(Duration = 0)]
		public async Task<ActionResult> Export(String Base, String Rep, String id, String Format)
		{
			try
			{
				using (var rr = Profiler.CurrentRequest.Start(ProfileAction.Report, $"export: {Rep}"))
				{
					var url = $"/_report/{Base.RemoveHeadSlash()}/{Rep}/{id}";
					ReportInfo ri = await GetReportInfo(url, id, CreateParamsFromQueryString());

					switch (ri.Type)
					{
						case RequestReportType.stimulsoft:
							ReportHelper.SetupLicense();
							return ReportHelper.ExportStiReport(ri, Format, saveFile: true);
						case RequestReportType.pdf:
							return ExportPdfReport(ri, saveFile: true);
						case RequestReportType.xml:
							return ExportXmlReport(ri);
						case RequestReportType.json:
							return ExportJsonReport(ri);
					}
				}
			}
			catch (Exception ex)
			{
				Response.ContentType = "text/html";
				Response.ContentEncoding = Encoding.UTF8;
				if (ex.InnerException != null)
					ex = ex.InnerException;
				Response.Write(ex.Message);
			}
			return new EmptyResult();
		}


		[HttpGet]
		[OutputCache(Duration = 0)]
		public async Task<ActionResult> Print(String Base, String Rep, String id, String Format)
		{
			try
			{
				var url = $"/_report/{Base.RemoveHeadSlash()}/{Rep}/{id}";
				ReportInfo ri = await GetReportInfo(url, id, CreateParamsFromQueryString());

				switch (ri.Type)
				{
					case RequestReportType.stimulsoft:
						ReportHelper.SetupLicense();
						return ReportHelper.ExportStiReport(ri, Format, saveFile: false);
					case RequestReportType.pdf:
						return ExportPdfReport(ri, saveFile: false);
					default:
						throw new NotImplementedException("ReportController.Print. ri.Type");
				}
			}
			catch (Exception ex)
			{
				Response.ContentType = "text/html";
				Response.ContentEncoding = Encoding.UTF8;
				if (ex.InnerException != null)
					ex = ex.InnerException;
				Response.Write(ex.Message);
			}
			return new EmptyResult();
		}

		ActionResult ExportPdfReport(ReportInfo ri, Boolean saveFile)
		{
			if (saveFile) {
				var cdh = new ContentDispositionHeaderValue("attachment")
				{
					FileNameStar = _baseController.Localize(ri.Name) + ".pdf"
				};
				Response.Headers.Add("Content-Disposition", cdh.ToString());
			}
			var stream = PdfReportHelper.Build(ri.ReportPath, ri.DataModel.Root);
			return new FileStreamResult(stream, MimeTypes.Application.Pdf);
		}

		ActionResult ExportJsonReport(ReportInfo ri)
		{
			String json = JsonConvert.SerializeObject(ri.DataModel.Root, Formatting.Indented,
				new JsonSerializerSettings()
				{
					NullValueHandling = NullValueHandling.Ignore,
				});
			if (!String.IsNullOrEmpty(ri.Name))
				return File(Encoding.UTF8.GetBytes(json), MimeTypes.Application.Json, $"{ri.Name}.json");
			else
				return Content(json, "application/json", Encoding.UTF8);
		}

		ActionResult ExportXmlReport(ReportInfo ri)
		{
			if (ri.XmlSchemaPathes == null)
				throw new RequestModelException("The xml-schemes are not specified");
			foreach (var path in ri.XmlSchemaPathes)
			{
				if (!System.IO.File.Exists(path))
					throw new RequestModelException($"File not found '{path}'");
			}
			if (String.IsNullOrEmpty(ri.Encoding))
				throw new RequestModelException("The xml encoding is not specified");
			var xmlCreator = new XmlCreator(ri.XmlSchemaPathes, ri.DataModel, ri.Encoding)
			{
				Validate = ri.Validate
			};
			var bytes = xmlCreator.CreateXml();
			if (xmlCreator.HasErrors)
				throw new Exception(xmlCreator.ErrorMessage);
			return File(bytes, "text/xml", $"{ri.Name}.xml");
		}

		public async Task<ActionResult> GetReport()
		{
			try
			{
				var vrp = ReportHelper.GetViewerRequestParams();
				var Rep = vrp.Param("Rep");
				var Base = vrp.Param("Base");
				var id = vrp.Route("Id");
				var url = $"/_report/{Base.RemoveHeadSlash()}/{Rep}/{id}";

				//TODO: profile var token = Profiler.BeginReport("create");
				var prms = new ExpandoObject();
				prms.Append(_baseController.CheckPeriod(vrp.QueryString), toPascalCase: true);
				prms.RemoveKeys("Rep,rep,Base,base,Format,format");

				ReportInfo ri = await GetReportInfo(url, id, prms);
				//TODO: image settings var rm = rm.ImageInfo;
				if (ri == null)
					throw new InvalidProgramException("invalid data");
				var path = ri.ReportPath;
				using (var stream = ri.GetStream(_baseController.Host.ApplicationReader))
				{
					return ReportHelper.CreateReportResult(stream, ri);
				}
			}
			catch (Exception ex)
			{
				String msg = ex.Message;
				Int32 x = msg.IndexOf(": error");
				if (x != -1)
					msg = msg.Substring(x + 7).Trim();
				return new HttpStatusCodeResult(500, msg);
			}
		}

		public ActionResult ViewerEvent()
		{
			return ReportHelper.ViewerEvent();
		}

		public ActionResult PrintReport()
		{
			return ReportHelper.PrintReport();
		}

		public ActionResult ExportReport()
		{
			return ReportHelper.ExportReport();
		}

		public ActionResult Interaction()
		{
			return ReportHelper.Interaction();
		}

		#region IControllerTenant
		public void StartTenant()
		{
			var host = ServiceLocator.Current.GetService<IApplicationHost>();
			host.TenantId = TenantId;
			host.UserId = UserId;
			host.UserSegment = UserSegment;
		}
		#endregion

		#region IControllerLocale
		public void SetLocale()
		{
			var locale = User.Identity.GetUserLocale();
			_baseController.SetUserLocale(locale);
		}
		#endregion
	}

	public class StimulsoftController : ReportController
	{

	}
}
