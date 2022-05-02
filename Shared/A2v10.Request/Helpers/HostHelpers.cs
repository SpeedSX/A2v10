﻿// Copyright © 2015-2021 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Newtonsoft.Json;

using System.Threading.Tasks;
using System.Text;
using System.Dynamic;

using A2v10.Data.Interfaces;
using A2v10.Infrastructure;

namespace A2v10.Request
{
	public static class HostHelpers
	{
		public static String AppStyleSheetsLink(this IApplicationHost host, String controllerName)
		{
			controllerName = controllerName ?? throw new ArgumentNullException(nameof(controllerName));
			// TODO _host AssestsDistionary
			var files = host.ApplicationReader.EnumerateFiles("_assets", "*.css");
			if (files == null)
				return String.Empty;
			// at least one file
			foreach (var f in files)
				return $"<link  href=\"/{controllerName.ToLowerInvariant()}/appstyles\" rel=\"stylesheet\" />";
			return String.Empty;
		}

		public static String AppLinks(this IApplicationHost host)
		{
			String appLinks = host.ApplicationReader.ReadTextFile(String.Empty, "links.json");
			if (appLinks != null)
			{
				// with validation
				Object links = JsonConvert.DeserializeObject<List<Object>>(appLinks);
				return JsonConvert.SerializeObject(links);
			}
			return "[]";
		}


		public static String GetAppData(this IApplicationHost host, ILocalizer localizer, IUserLocale userLocale)
		{
			var appJson = host.ApplicationReader.ReadTextFile(String.Empty, "app.json");
			if (appJson != null)
			{
				if (appJson.Contains("$(")) {
					var sb = new StringBuilder(appJson);
					sb.Replace("$(lang)", userLocale.Language)
					  .Replace("$(lang2)", userLocale.Language2);
					appJson = sb.ToString();
				}
				// with validation
				ExpandoObject app = JsonConvert.DeserializeObject<ExpandoObject>(appJson);
				app.Set("embedded", host.Embedded);
				return localizer.Localize(null, JsonConvert.SerializeObject(app));
			}

			ExpandoObject defAppData = new ExpandoObject();
			defAppData.Set("version", host.AppVersion);
			defAppData.Set("title", "A2v10 Web Application");
			defAppData.Set("copyright", host.Copyright);
			defAppData.Set("embedded", host.Embedded);
			return JsonConvert.SerializeObject(defAppData);
		}

		public static String CustomAppHead(this IApplicationHost host)
		{
			String head = host.ApplicationReader.ReadTextFile("_layout", "_head.html");
			if (head == null)
				return String.Empty;
			return head.Replace("$(UserName)", host.UserName);
		}

		public static String CustomAppScripts(this IApplicationHost host)
		{
			String scripts = host.ApplicationReader.ReadTextFile("_layout", "_scripts.html");
			return scripts != null ?  host.GetAppSettings(scripts) : String.Empty;
		}

		public static String CustomManifest(this IApplicationHost host)
		{
			var manifestPath = Path.Combine(host.HostingPath, "manifest.json");
			return File.Exists(manifestPath) ? "<link rel=\"manifest\" href=\"/manifest.json\">" : null;
		}

		public static String AppleTouchIcon(this IApplicationHost host)
		{
			var touchIconPath = Path.Combine(host.HostingPath, "touch-icon-iphone.png");
			return File.Exists(touchIconPath) ? "<link rel=\"apple-touch-icon\"  href=\"/touch-icon-iphone.png\">" : null;
		}

		public static void ReplaceMacros(this IApplicationHost host, StringBuilder sb, String controllerName = "_shell")
		{
			sb.Replace("$(Build)", host.AppBuild);
			sb.Replace("$(LayoutHead)", host.CustomAppHead());
			sb.Replace("$(AppleTouchIcon)", host.AppleTouchIcon());
			sb.Replace("$(LayoutManifest)", host.CustomManifest());
			sb.Replace("$(AssetsStyleSheets)", host.AppStyleSheetsLink(controllerName));
			sb.Replace("$(HelpUrl)", host.HelpUrl);
			sb.Replace("$(Description)", host.AppDescription);
			var theme = host.Theme;
			sb.Replace("$(ColorScheme)", theme?.ColorScheme ?? null);
			sb.Replace("$(Theme)", theme?.FileName ?? null);
			sb.Replace("$(ThemeTimeStamp)", theme?.ThemeTimeStamp ?? null);
		}


		public static Task ProcessDbEvents(this IApplicationHost host, IDbContext dbContext, String source)
		{
			return ProcessDbEventsCommand.ProcessDbEvents(dbContext, source, host.IsAdminMode);
		}

		public static ITypeChecker CheckTypes(this IApplicationHost host, String path, String typesFile, IDataModel model)
		{
			if (!host.IsDebugConfiguration)
				return null;
			if (String.IsNullOrEmpty(typesFile))
				return null;
			var tc = new TypeChecker(host.ApplicationReader, path);
			tc.CreateChecker(typesFile, model);
			return tc;
		}

	}
}
