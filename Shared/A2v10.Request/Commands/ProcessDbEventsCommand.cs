﻿// Copyright © 2021 Alex Kukhtin. All rights reserved.

using System;
using System.Dynamic;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;

using A2v10.Data.Interfaces;
using A2v10.Infrastructure;
using IApplicationHost = A2v10.Infrastructure.IApplicationHost;

namespace A2v10.Request
{
	public class DbEventBase
	{
		public Guid Id { get; set; }
	}

	public class DbEvent : DbEventBase
	{
		public Int64 ItemId { get; set; }
		public String Path { get; set; }
		public String Command { get; set; }
		public String Source { get; set; }

		public String JsonParams { get; set; }
	}

	public class DbEventError : DbEventBase
	{
		public Int64 ItemId { get; set; }
		public String ErrorMessage { get; set; }
	}


	public class ProcessDbEventsCommand : IServerCommand
	{
		private readonly IDbContext _dbContext;
		private readonly IApplicationHost _host;

		public ProcessDbEventsCommand(IApplicationHost host, IDbContext dbContext)
		{
			_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
			_host = host ?? throw new ArgumentNullException(nameof(host));
		}

		static async Task ProcessEvent(DbEvent evt, BaseController ctrl)
		{
			// json, data, from evt
			var eo = new ExpandoObject();
			eo.Set("baseUrl", $"/_simple{evt.Path}");
			eo.Set("cmd", evt.Command);
			if (evt.ItemId != 0)
				eo.Set("Id", evt.ItemId);
			if (!String.IsNullOrEmpty(evt.JsonParams))
				eo.Set("data", JsonConvert.DeserializeObject<ExpandoObject>(evt.JsonParams));

			var json = JsonConvert.SerializeObject(eo, JsonHelpers.StandardSerializerSettings);
			await ctrl.Data("invoke",
				prms => {
					prms.Set("UserId", 0 /*system*/);
					if (evt.ItemId != 0)
						prms.Set("Id", evt.ItemId);
				},
				json,
				null
			);
			var evtBase = new DbEventBase() { Id = evt.Id };
			ctrl.DbContext.Execute<DbEventBase>(evt.Source, "a2sys.[DbEvent.Complete]", evtBase);
		}


		static async Task ProcessEvent(CancellationToken token, DbEvent evt, Boolean isAdminMode)
		{
			//var loc = new ServiceLocator();
			IServiceLocator loc = ServiceLocator.Current;
			//var host = loc.GetService<IApplicationHost>();
			var dbContext = loc.GetService<IDbContext>();
			//host.SetAdmin(isAdminMode);
			//host.StartApplication(isAdminMode);
			var ctrl = new BaseController();
			if (token.IsCancellationRequested)
				return;
			try
			{
				await ProcessEvent(evt, ctrl);
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
					ex = ex.InnerException;
				var evtError = new DbEventError() { Id = evt.Id, ErrorMessage = ex.Message };
				dbContext.Execute<DbEventError>(evt.Source, "a2sys.[DbEvent.Error]", evtError);
			}
		}

		public async Task<ServerCommandResult> Execute(RequestCommand cmd, ExpandoObject dataToExec)
		{
			await ProcessDbEvents(_dbContext, cmd.CurrentSource, _host.IsAdminMode);
			return new ServerCommandResult("{}"); /* empty result */
		}

		public static async Task ProcessDbEvents(IDbContext dbContext, String source, bool isAdminMode)
		{
			var eventList = await dbContext.LoadListAsync<DbEvent>(source, "a2sys.[DbEvent.Fetch]", null);
			if (eventList == null || eventList.Count == 0)
				return;
			var token = new CancellationToken();
			foreach (var evt in eventList)
			{
				await ProcessEvent(token, evt, isAdminMode);
			}
			/*
			var currentContext = HttpContext.Current;
			//foreach (var evt in eventList)
			//{
				HostingEnvironment.QueueBackgroundWorkItem(async token => {
					HttpContext.Current = currentContext;
				});
			//}
			*/
		}
	}
}
