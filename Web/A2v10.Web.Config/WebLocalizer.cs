﻿// Copyright © 2015-2020 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using A2v10.Data.Interfaces;
using A2v10.Infrastructure;

namespace A2v10.Web.Config
{

	class LocaleMapItem
	{
		public ConcurrentDictionary<String, String> Map { get; }
		public Boolean Loaded { get; set; }

		public LocaleMapItem()
		{
			Map = new ConcurrentDictionary<String, String>();
		}
	}

	struct LocalePath
	{
		public String Path;
		public Boolean IsFileSystem;

		public LocalePath(String path, Boolean fs)
		{
			Path = path;
			IsFileSystem = fs;
		}

	}

	internal class WebDictionary
	{
		ConcurrentDictionary<String, LocaleMapItem> _maps = new ConcurrentDictionary<String, LocaleMapItem>();
		FileSystemWatcher _watcher_system = null;
		FileSystemWatcher _watcher_app = null;

		public IDictionary<String, String> GetLocalizerDictionary(IApplicationConfig config, IApplicationHost host, String locale)
		{
			var map = GetCurrentMap(locale);
			if (map.Loaded)
				return map.Map;
			map.Loaded = true;

			foreach (var localePath in GetLocalizerFilePath(config, host, locale))
			{
				IEnumerable<String> lines = localePath.IsFileSystem ?
					File.ReadAllLines(localePath.Path) :
					host.ApplicationReader.FileReadAllLines(localePath.Path);

				foreach (var line in lines)
				{
					if (String.IsNullOrWhiteSpace(line))
						continue;
					if (line.StartsWith(";"))
						continue;
					Int32 pos = line.IndexOf('=');
					if (pos != -1)
					{
						var key = line.Substring(0, pos);
						var val = line.Substring(pos + 1);
						map.Map.AddOrUpdate(key, val, (k, oldVal) => val);
					}
					else
						throw new InvalidDataException($"Invalid dictionary string '{line}'");
				}
			}
			return map.Map;
		}

		IEnumerable<LocalePath> GetLocalizerFilePath(IApplicationConfig config, IApplicationHost host, String locale)
		{
			// locale may be "uk_UA"
			var dirPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Localization");
			var appReader = host.ApplicationReader;
			///var appPath = Path.GetFullPath(Path.Combine(_host.AppPath, _host.AppKey ?? String.Empty, "_localization"));
			var appPath = appReader.MakeFullPath("_localization", String.Empty);
			if (!Directory.Exists(dirPath))
				dirPath = null;

			if (!appReader.DirectoryExists(appPath))
				appPath = null;

			CreateWatchers(config, host, dirPath, appReader.IsFileSystem ? appPath : null);
			if (dirPath != null)
			{
				foreach (var s in Directory.EnumerateFiles(dirPath, $"*.{locale}.txt"))
					yield return new LocalePath(s, true);
			}
			var appFiles = appReader.EnumerateFiles(appPath, $"*.{locale}.txt");
			if (appFiles != null)
			{
				foreach (var s in appFiles)
					yield return new LocalePath(s, false);
			}
			// simple locale: uk
			if (locale.Length > 2)
			{
				locale = locale.Substring(0, 2);
				if (dirPath != null)
				{
					foreach (var s in Directory.EnumerateFiles(dirPath, $"*.{locale}.txt"))
						yield return new LocalePath(s, true);
				}
				appFiles = appReader.EnumerateFiles(appPath, $"*.{locale}.txt");
				if (appFiles != null)
				{
					foreach (var s in appFiles)
						yield return new LocalePath(s, false);
				}
			}
		}


		LocaleMapItem GetCurrentMap(String locale)
		{
			return _maps.GetOrAdd(locale, (key) => new LocaleMapItem());
		}


		void CreateWatchers(IApplicationConfig config, IApplicationHost host2, String dirPath, String appPath)
		{
			if (_watcher_system != null)
				return;
			if (!config.IsDebugConfiguration)
				return;
			if (!String.IsNullOrEmpty(dirPath))
			{
				// FileName can be in 8.3 format!
				_watcher_system = new FileSystemWatcher(dirPath, "*.*")
				{
					NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes
				};
				_watcher_system.Changed += _watcher_Changed;
				_watcher_system.Created += _watcher_Changed;
				_watcher_system.Deleted += _watcher_Changed;
				_watcher_system.EnableRaisingEvents = true;
			}

			if (!String.IsNullOrEmpty(appPath))
			{
				// FileName can be in 8.3 format!
				_watcher_app = new FileSystemWatcher(appPath, "*.*")
				{
					NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.Attributes
				};
				_watcher_app.Changed += _watcher_Changed;
				_watcher_app.Created += _watcher_Changed;
				_watcher_app.Deleted += _watcher_Changed;
				_watcher_app.EnableRaisingEvents = true;
			}
		}

		private void _watcher_Changed(Object sender, FileSystemEventArgs e)
		{
			_maps.Clear();
		}
	}


	public class WebLocalizer : BaseLocalizer, IDataLocalizer
	{
		private readonly IApplicationHost _host;
		private readonly IApplicationConfig _config;

		private static readonly Lazy<WebDictionary> _webDictionary = new Lazy<WebDictionary>(()=>new WebDictionary(), isThreadSafe:true);

		public WebLocalizer(IApplicationConfig config, IApplicationHost host)
			:base()
		{
			_config = config;
			_host = host;
		}

		protected override IDictionary<String, String> GetLocalizerDictionary(String locale)
		{
			return _webDictionary.Value.GetLocalizerDictionary(_config, _host, locale);
		}

		String IDataLocalizer.Localize(String content)
		{
			return Localize(null, content, true);
		}
	}
}
