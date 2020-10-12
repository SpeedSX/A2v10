﻿// Copyright © 2015-2020 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;

using Stimulsoft.Report.Mvc;

using A2v10.Data.Interfaces;
using A2v10.Infrastructure;
using A2v10.Request;
using System.Configuration;
using Stimulsoft.Report;
using System.IO;

namespace A2v10.Reports
{
	public class ReportContext
	{
		public Int64 UserId;
		public Int32 TenantId;
		public Int64 CompanyId;
	}

	public class ExportReportResult
	{
		public String ContentType;
		public String Extension;
	}
	

	public class ReportInfo
	{
		public IDataModel DataModel { get; set; }
		public String Name { get; set; }
		public ExpandoObject Variables { get; set; }

		public String ReportPath;
		public Byte[] ReportStream;
		public RequestReportType Type;
		public IList<String> XmlSchemaPathes;
		public String Encoding;
		public Boolean Validate;

		public Stream GetStream(IApplicationReader reader)
		{
			if (!String.IsNullOrEmpty(ReportPath))
				return reader.FileStreamFullPathRO(ReportPath);
			else if (ReportStream != null)
				return new MemoryStream(ReportStream);
			throw new InvalidOperationException("Invalid report stream mode");
		}
	}

	public sealed class ReportHelper
	{
		private readonly IApplicationHost _host;
		private readonly IApplicationConfig _config;
		private readonly IDbContext _dbContext;
		private readonly ILocalizer _localizer;

		public ReportHelper()
		{
			// DI ready
			IServiceLocator locator = ServiceLocator.Current;
			_host = locator.GetService<IApplicationHost>();
			_config = locator.GetService<IApplicationConfig>();
			_dbContext = locator.GetService<IDbContext>();
			_localizer = locator.GetService<ILocalizer>();
		}

		public async Task<ReportInfo> GetReportInfo(ReportContext context, String url, String id, ExpandoObject prms)
		{
			var appReader = _host.ApplicationReader;
			var ri = new ReportInfo();
			RequestModel rm = await RequestModel.CreateFromBaseUrl(_host, false, url);
			var rep = rm.GetReport();
			ri.Type = rep.type;

			if (rep.type == RequestReportType.xml)
			{
				if (rep.xmlSchemas != null)
				{
					ri.XmlSchemaPathes = new List<String>();
					foreach (var schema in rep.xmlSchemas)
						ri.XmlSchemaPathes.Add(appReader.MakeFullPath(rep.Path, schema + ".xsd"));
				}

				ri.Encoding = rep.encoding;
				ri.Validate = rep.validate;
			}

			prms.Set("UserId", context.UserId);
			if (_host.IsMultiTenant || context.TenantId != 0 /*hack for desktop*/)
				prms.Set("TenantId", context.TenantId);
			if (_host.IsMultiCompany)
				prms.Set("CompanyId", context.CompanyId);
			prms.Set("Id", id);
			prms.AppendIfNotExists(rep.parameters);

			ri.DataModel = await _dbContext.LoadModelAsync(rep.CurrentSource, rep.ReportProcedure, prms);

			CheckTypes(rep.Path, rep.checkTypes, ri.DataModel);

			// after query
			ExpandoObject vars = rep.variables;
			if (vars == null)
				vars = new ExpandoObject();
			vars.Set("UserId", context.UserId);
			if (_host.IsMultiTenant)
				vars.Set("TenantId", context.TenantId);
			if (_host.IsMultiCompany)
				prms.Set("CompanyId", context.CompanyId);
			vars.Set("Id", id);
			ri.Variables = vars;
			var repName = _localizer.Localize(null, String.IsNullOrEmpty(rep.name) ? rep.ReportName : rep.name);
			if (ri.DataModel != null && ri.DataModel.Root != null)
				repName = ri.DataModel.Root.Resolve(repName);
			ri.Name = repName;

			if (rep.ReportFromDataModel)
			{
				ri.ReportStream = ri.DataModel.Eval<Byte[]>(rep.ReportExpression);
				if (ri.ReportStream == null)
					throw new InvalidDataException($"Expression '{rep.ReportName}'  is null");
			}
			else if (rep.HasPath)
				ri.ReportPath = appReader.MakeFullPath(rep.Path, rep.ReportName + ".mrt");

			return ri;
		}

		// saveFileDialog: true -> download
		// saveFileDialog: false -> show
		public StiMvcActionResult ExportStiReport(ReportInfo ri, String format, Boolean saveFile = true)
		{
			var targetFormat = (format ?? "pdf").ToLowerInvariant();
			using (var stream = ri.GetStream(_host.ApplicationReader))
			{
				var r = StiReportExtensions.CreateReport(stream, ri.Name);
				r.AddDataModel(ri.DataModel);
				if (ri.Variables != null)
					r.AddVariables(ri.Variables);
				if (targetFormat == "pdf")
					return StiMvcReportResponse.ResponseAsPdf(r, StiReportExtensions.GetPdfExportSettings(), saveFileDialog: saveFile);
				else if (format == "excel")
					return StiMvcReportResponse.ResponseAsExcel2007(r, StiReportExtensions.GetDefaultXlSettings(), saveFileDialog: saveFile);
				else if (format == "word")
					return StiMvcReportResponse.ResponseAsWord2007(r, StiReportExtensions.GetDefaultWordSettings(), saveFileDialog: saveFile);
				else if (format == "opentext")
					return StiMvcReportResponse.ResponseAsOdt(r, StiReportExtensions.GetDefaultOdtSettings(), saveFileDialog: saveFile);
				else if (format == "opensheet")
					return StiMvcReportResponse.ResponseAsOds(r, StiReportExtensions.GetDefaultOdsSettings(), saveFileDialog: saveFile);
				else
					throw new NotImplementedException($"Format '{targetFormat}' is not supported in this version");
			}
		}

		public ExportReportResult ExportStiReportStream(ReportInfo ri, String format, Stream output)
		{
			var rr = new ExportReportResult();
			var targetFormat = (format ?? "pdf").ToLowerInvariant();
			using (var stream = ri.GetStream(_host.ApplicationReader))
			{
				var r = StiReportExtensions.CreateReport(stream, ri.Name);
				r.AddDataModel(ri.DataModel);
				if (ri.Variables != null)
					r.AddVariables(ri.Variables);
				if (targetFormat == "pdf")
				{
					r.Render();
					r.ExportDocument(StiExportFormat.Pdf, output, StiReportExtensions.GetDefaultPdfSettings());
					rr.ContentType = "application/pdf";
					rr.Extension = "pdf";
				}
				else if (format == "excel")
				{
					r.Render();
					r.ExportDocument(StiExportFormat.Excel2007, output, StiReportExtensions.GetDefaultXlSettings());
					rr.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
					rr.Extension = "xlsx";
				}
				else if (format == "word")
				{
					r.Render();
					r.ExportDocument(StiExportFormat.Word2007, output, StiReportExtensions.GetDefaultWordSettings());
					rr.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
					rr.Extension = "docx";
				}
				else if (format == "opentext")
				{
					r.Render();
					r.ExportDocument(StiExportFormat.Odt, output, StiReportExtensions.GetDefaultOdtSettings());
					rr.ContentType = "application/vnd.oasis.opendocument.text";
					rr.Extension = "odt";
				}
				else if (format == "opensheet")
				{
					r.Render();
					r.ExportDocument(StiExportFormat.Ods, output, StiReportExtensions.GetDefaultOdsSettings());
					rr.ContentType = "application/vnd.oasis.opendocument.spreadsheet";
					rr.Extension = "ods";
				}
				else
					throw new NotImplementedException($"Format '{targetFormat}' is not supported in this version");
			}
			return rr;
		}

		private Boolean _licenseSet = false;

		public void SetupLicense()
		{
			if (_licenseSet)
				return;
			_licenseSet = true;
			var lic = ConfigurationManager.AppSettings["stimulsoft.license"];
			if (!String.IsNullOrEmpty(lic))
				Stimulsoft.Base.StiLicense.LoadFromString(lic);
		}

		ITypeChecker CheckTypes(String path, String typesFile, IDataModel model)
		{
			if (!_config.IsDebugConfiguration)
				return null;
			if (String.IsNullOrEmpty(typesFile))
				return null;
			var tc = new TypeChecker(_host.ApplicationReader, path);
			tc.CreateChecker(typesFile, model);
			return tc;
		}

	}
}
