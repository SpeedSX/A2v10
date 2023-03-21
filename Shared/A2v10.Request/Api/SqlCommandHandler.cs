﻿// Copyright © 2020-2021 Alex Kukhtin. All rights reserved.

using System;
using System.Dynamic;
using System.Threading.Tasks;

using A2v10.Data.Interfaces;
using A2v10.Infrastructure;
using Newtonsoft.Json;

namespace A2v10.Request.Api
{
	public class SqlCommandHandler : ApiCommandHandler
	{
		private readonly IDbContext _dbContext;
		private readonly ApiSqlCommand _command;
		public SqlCommandHandler(IServiceLocator serviceLocator, ApiSqlCommand command, Boolean wrap)
		{
			_dbContext = serviceLocator.GetService<IDbContext>();
			_command = command;
			_wrap = wrap;
		}

		public override Task<IApiResponse> ExecuteAsync(IApiRequest request)
		{
			switch (_command.Action)
			{
				case SqlCommandAction.LoadModel:
					return LoadModelAsync(request);
				case SqlCommandAction.UpdateModel:
					return UpdateModelAsync(request);
				case SqlCommandAction.ExecuteSql:
					return ExecuteSqlAsync(request);
			}
			throw new ApiV2Exception($"invalid sql action {_command.Action}");
		}

		private async Task<IApiResponse> LoadModelAsync(IApiRequest request)
		{ 
			var sql = $"[{_command.RealSchema}].[{_command.Model}.Load]";

			var prms = CreateParameters(request);
			var dm = await _dbContext.LoadModelAsync(_command.RealSource, sql, prms, _command.CommandTimeout);

			return ModelResponse(dm);
		}

		private async Task<IApiResponse> UpdateModelAsync(IApiRequest request)
		{
			var sql = $"[{_command.RealSchema}].[{_command.Model}.Update]";

			var prms = CreateParameters(request);
			var dm = await _dbContext.SaveModelAsync(_command.RealSource, sql, request.Body, prms);

			return ModelResponse(dm);
		}


		private async Task<IApiResponse> ExecuteSqlAsync(IApiRequest request)
		{
			var sql = $"[{_command.RealSchema}].[{_command.Procedure}]";
			var prms = CreateParameters(request);
			var dmRoot = await _dbContext.ExecuteAndLoadExpandoAsync(_command.RealSource, sql, prms, _command.CommandTimeout);
			Object retObj = dmRoot;
			if (!String.IsNullOrEmpty(_command.Returns))
				retObj = dmRoot.Eval<Object>(_command.Returns);

			var resp = new ApiResponse()
			{
				ContentType = MimeTypes.Application.Json,
			};

			if (retObj != null)
				resp.Body = JsonConvert.SerializeObject(Wrap(retObj), JsonHelpers.CompactSerializerSettings);
			else
				resp.Body = "{\"success\": true}";
			return resp;
		}

		ApiResponse ModelResponse(IDataModel dm)
		{ 
			Object model = dm.Root;
			if (!String.IsNullOrEmpty(_command.Returns))
				model = dm.Eval<Object>(_command.Returns);

			return new ApiResponse()
			{
				ContentType = MimeTypes.Application.Json,
				Body = JsonConvert.SerializeObject(Wrap(model), JsonHelpers.CompactSerializerSettings)
			};
		}

		ExpandoObject CreateParameters(IApiRequest request)
		{
			var rq = new ExpandoObject();
			rq.Set("id", _command.RealId);
			rq.Set("body", request.Body);
			rq.Set("query", request.Query);
			rq.Set("config", request.Config);

			var eo = new ExpandoObject();
			eo.Set("UserId", request.UserId);
			if (request.TenantId != null)
				eo.Set("TenantId", request.TenantId);

			if (_command.Parameters != null)
			{
				foreach (var p in _command.Parameters)
				{
					// key - name, value => path
					eo.Set(p.Key, rq.Eval<Object>(p.Value.ToString()));
				}
			}
			return eo;
		}
	}
}
