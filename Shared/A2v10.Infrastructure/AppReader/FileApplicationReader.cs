﻿// Copyright © 2015-2019 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace A2v10.Infrastructure
{
	public class FileApplicationReader : IApplicationReader
	{
		private String AppPath { get;}
		private String AppKey { get; }

		public Boolean IsFileSystem => true;

		public Boolean EmulateBox { get; set; }


		public FileApplicationReader(String appPath, String appKey)
		{
			AppPath = appPath;
			AppKey = appKey;
		}

		public async Task<String> ReadTextFileAsync(String path, String fileName)
		{
			String fullPath = GetFullPath(path, fileName);
			if (!File.Exists(fullPath))
				return null;
			using var tr = new StreamReader(fullPath);
			return await tr.ReadToEndAsync();
		}

		public String ReadTextFile(String path, String fileName)
		{
			String fullPath = GetFullPath(path, fileName);
			if (!File.Exists(fullPath))
				return null;
			using var tr = new StreamReader(fullPath);
			return tr.ReadToEnd();
		}

		public IEnumerable<String> EnumerateFiles(String path, String searchPattern)
		{
			if (String.IsNullOrEmpty(path))
				return null;
			var fullPath = GetFullPath(path, String.Empty);
			if (!Directory.Exists(fullPath))
				return null;
			return Directory.EnumerateFiles(fullPath, searchPattern);
		}

		public Boolean FileExists(String fullPath)
		{
			return File.Exists(fullPath);
		}

		public Boolean DirectoryExists(String fullPath)
		{
			return Directory.Exists(fullPath);
		}


		public String FileReadAllText(String fullPath)
		{
			return File.ReadAllText(fullPath);
		}

		/*
		public IEnumerable<String> FileReadAllLines(String fullPath)
		{
			return File.ReadAllLines(fullPath);
		}
		*/

		/*
		public Stream FileStream(String path, String fileName)
		{
			String fullPath = GetFullPath(path, fileName);
			if (FileExists(fullPath))
				return new FileStream(fullPath, FileMode.Open);
			return null;
		}*/

		public Stream FileStreamFullPathRO(String fullPath)
		{
			return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		public String MakeFullPath(String path, String fileName)
		{
			return GetFullPath(path, fileName);
		}

		String GetFullPath(String path, String fileName)
		{
			String appKey = AppKey;
			if (fileName.StartsWith("/"))
			{
				path = String.Empty;
				fileName = fileName.Remove(0, 1);
			}
			if (appKey != null)
				appKey = "/" + appKey;

			if (path.StartsWith("$"))
			{
				path = path.Replace("$", "../");
			}

			String fullPath = Path.Combine($"{AppPath}{appKey}", path, fileName);

			if (EmulateBox)
			{
				var ext = Path.GetExtension(fullPath);
				if (!String.IsNullOrEmpty(ext))
				{
					var boxPath = $"{fullPath.Substring(0, fullPath.Length - ext.Length)}.box{ext}";
					var boxFullPath = Path.GetFullPath(boxPath);
					if (File.Exists(boxFullPath))
						return boxFullPath;
				}
			}

			return Path.GetFullPath(fullPath);
		}

		public String CombineRelativePath(String path1, String path2)
		{
			return Path.GetFullPath(Path.Combine(path1, path2));
		}
		public String CombinePath(String path1, String path2, String fileName)
		{
			return Path.Combine(path1, path2, fileName);
		}
	}
}
