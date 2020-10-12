﻿// Copyright © 2015-2020 Alex Kukhtin. All rights reserved.

using System;
using System.Configuration;
using System.Globalization;
using System.Threading;

using A2v10.BackgroundTasks;
using A2v10.Data;
using A2v10.Data.Interfaces;
using A2v10.Infrastructure;
using A2v10.Messaging;
using A2v10.Workflow;

namespace BackgroundProcessor
{
	public class Program
	{
		static void Main(String[] args)
		{
			BackgroundTasksManager _manager = null;
			try
			{
				var defaultCulture = ConfigurationManager.AppSettings["defaultCulture"];
				if (defaultCulture != null)
				{
					var cultureInfo = new CultureInfo(defaultCulture);
					CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
					CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
				}

				StartServices();
				var loc = ServiceLocator.Current;
				ILogger logger = loc.GetService<ILogger>();
				IDbContext dbContext = loc.GetService<IDbContext>();
				IApplicationConfig config = loc.GetService<IApplicationConfig>();
				IApplicationHost host = loc.GetService<IApplicationHost>();
				IMessaging messaging = loc.GetService<IMessaging>();
				Console.WriteLine("Service started");
				_manager = new BackgroundTasksManager(config, host, dbContext, logger, messaging);
				logger.LogBackground($"CurrentCulutre: {Thread.CurrentThread.CurrentCulture}");
				host.StartApplication(false);
				_manager.Start();
				_manager.StartTasksFromConfig();
				Console.WriteLine("Press any key to stop service...");
				Console.Read();
				_manager.Stop();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				_manager?.Stop();
			}
		}

		private static IServiceLocator _currentService;

		static void StartServices()
		{
			ServiceLocator.Start = (IServiceLocator loc) =>
			{
				var profiler = new NullProfiler();
				var localizer = new NullLocalizer();
				var config = new BackgroundApplicationConfig();
				var host = new BackgroundApplicationHost(config, profiler);
				var dbContext = new SqlDbContext(profiler, host, localizer);
				var logger = new BackgroundLogger(dbContext);
				var workflow = new WorkflowEngine(config, host, dbContext, null);
				var emailService = new EmailService(logger, config);
				var messaging = new MessageProcessor(host, dbContext, emailService, logger);

				loc.RegisterService<IProfiler>(profiler);
				loc.RegisterService<ILocalizer>(localizer);
				loc.RegisterService<IDbContext>(dbContext);
				loc.RegisterService<ILogger>(logger);
				loc.RegisterService<IApplicationHost>(host);
				loc.RegisterService<IApplicationConfig>(config);
				loc.RegisterService<IWorkflowEngine>(workflow);
				loc.RegisterService<IMessaging>(messaging);
			};

			ServiceLocator.GetCurrentLocator = () =>
			{
				if (_currentService == null)
					_currentService = new ServiceLocator();
				return _currentService;
			};
		}
	}
}
