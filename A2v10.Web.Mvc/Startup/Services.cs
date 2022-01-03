﻿// Copyright © 2021 Alex Kukhtin. All rights reserved.

using System;
using System.Web;

using A2v10.Data;
using A2v10.Data.Interfaces;
using A2v10.Infrastructure;
using A2v10.Messaging;
using A2v10.Request;
using A2v10.Web.Base;
using A2v10.Web.Config;
using A2v10.Web.Identity;
using A2v10.Web.Mvc.Config;
using A2v10.Web.Script;
using A2v10.Workflow;

namespace A2v10.Web.Mvc.Startup
{
	public class StartOptions
	{
		public IProfiler Profiler;
		public ITokenProvider TokenProvider;
	}

	public static class Services
	{

		public static void StartServices(Action<StartOptions> opts)
		{
			// DI ready
			ServiceLocator.Start = (IServiceLocator locator) =>
			{
				var startOptions = new StartOptions();
				opts?.Invoke(startOptions);

				IProfiler profiler = startOptions.Profiler ?? new WebProfiler();
				IUserLocale userLocale = new WebUserLocale();
				IApplicationHost host = new WebApplicationHost(profiler, userLocale, locator);
				ILocalizer localizer = new WebLocalizer(host, userLocale);

				ITokenProvider tokenProvider = startOptions.TokenProvider;
				IDbContext dbContext = new SqlDbContext(
					profiler as IDataProfiler,
					host as IDataConfiguration,
					localizer as IDataLocalizer,
					host as ITenantManager,
					tokenProvider);
				IDataScripter scripter = new VueDataScripter(host, localizer);
				ILogger logger = new WebLogger(host, dbContext);
				IMessageService emailService = new IdentityEmailService(logger, host);
                ISmsService smsService = new SmsService(dbContext, logger);
                IUserStateManager userStateManager = new EmptyUserStateManager();
                IMessaging messaging = new MessageProcessor(host, dbContext, emailService, smsService, logger);
				IWorkflowEngine workflowEngine = new WorkflowEngine(host, dbContext, messaging);
				IScriptProcessor scriptProcessor = new ScriptProcessor(scripter, host);
				IHttpService httpService = new HttpService();


                locator.RegisterService<IDbContext>(dbContext);
				locator.RegisterService<IProfiler>(profiler);
				locator.RegisterService<IApplicationHost>(host);
				locator.RegisterService<ILocalizer>(localizer);
				locator.RegisterService<IDataScripter>(scripter);
				locator.RegisterService<ILogger>(logger);
				locator.RegisterService<IMessageService>(emailService);
				locator.RegisterService<ISmsService>(smsService);
				locator.RegisterService<IMessaging>(messaging);
				locator.RegisterService<IWorkflowEngine>(workflowEngine);
				locator.RegisterService<IScriptProcessor>(scriptProcessor);
				locator.RegisterService<IHttpService>(httpService);
				locator.RegisterService<IUserLocale>(userLocale);
                locator.RegisterService<IUserStateManager>(userStateManager);
				if (tokenProvider != null)
					locator.RegisterService<ITokenProvider>(tokenProvider);

				host.StartApplication(false);
				HttpContext.Current.Items.Add("ServiceLocator", locator);

			};

			ServiceLocator.GetCurrentLocator = () =>
			{
				var locator = HttpContext.Current.Items["ServiceLocator"];
				if (locator == null)
					new ServiceLocator();
				return HttpContext.Current.Items["ServiceLocator"] as IServiceLocator;
			};
		}
	}
}
