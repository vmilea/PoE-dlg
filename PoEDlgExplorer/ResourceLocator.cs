using System;
using System.Collections.Generic;
using System.IO;

namespace PoEDlgExplorer
{
	public static class ResourceLocator
	{
		private class Vocalization
		{
			private readonly IDictionary<string, FileInfo> _files = new Dictionary<string, FileInfo>(4);

			public FileInfo FindVariant(string variant)
			{
				FileInfo file;
				return _files.TryGetValue(variant, out file) ? file : null;
			}

			public void AddVariant(string variant, FileInfo file)
			{
				if (_files.ContainsKey(variant))
					throw new ArgumentException("Already have vocalization variant " + variant);

				_files[variant] = file;
			}

			internal static bool TryParse(string fileName, out string conversationTag, out int nodeId, out string suffix)
			{
				int index = fileName.LastIndexOf('_');
				int suffixIndex = -1;

				while (index != -1 && !IsNodeId(fileName, index + 1))
				{
					suffixIndex = index + 1;
					index = fileName.LastIndexOf('_', index - 1);
				}

				if (index == -1)
				{
					conversationTag = null;
					nodeId = -1;
					suffix = null;
					return false;
				}

				conversationTag = fileName.Substring(0, index);
				nodeId = int.Parse(fileName.Substring(index + 1, 4));

				if (suffixIndex == -1)
				{
					suffix = "";
				}
				else
				{
					suffix = fileName.Substring(suffixIndex);
					suffix = suffix.Substring(0, suffix.LastIndexOf('.'));
				}
				return true;
			}

			private static bool IsNodeId(string str, int index)
			{
				return (index + 3 < str.Length)
					&& char.IsDigit(str[index]) && char.IsDigit(str[index + 1])
					&& char.IsDigit(str[index + 2]) && char.IsDigit(str[index + 3]);
			}
		}

		private const string DataSubpath = @"PillarsOfEternity_Data\data\";

		private static DirectoryInfo _gameDir;
		private static IDictionary<string, IDictionary<int, Vocalization>> _audioFileDescriptors;

		public static DirectoryInfo GameDir { get { return _gameDir; } }

		public static void Initialize(string gamePath)
		{
			int index = gamePath.LastIndexOf(DataSubpath, StringComparison.Ordinal);
			if (index == -1)
				_gameDir = new DirectoryInfo(gamePath);
			else
				_gameDir = new DirectoryInfo(gamePath.Substring(0, index));

			if (!new DirectoryInfo(_gameDir.FullName + DataSubpath).Exists)
				throw new FileNotFoundException("Invalid game dir: " + _gameDir.FullName);

			LoadVocalizationFiles();
		}

		public static IList<FileInfo> FindAllConversations()
		{
			var dir = new DirectoryInfo(_gameDir.FullName + DataSubpath + @"conversations\");

			var files = new List<FileInfo>();
			RecursiveSearchAll(dir, "*.conversation", files);
			return files;
		}

		public static FileInfo FindConversation(string conversationTag)
		{
			var dir = new DirectoryInfo(_gameDir.FullName + DataSubpath + @"conversations\");
			return RecursiveSearch(dir, conversationTag + ".conversation");
		}

		public static FileInfo FindStringTable(string conversationTag, string localization)
		{
			var dir = new DirectoryInfo(_gameDir.FullName + DataSubpath
				+ @"localized\" + localization + @"\text\conversations\");
			return RecursiveSearch(dir, conversationTag + ".stringtable");
		}

		public static FileInfo FindStringTableByPath(FileInfo conversationFile, string localization)
		{
			var file = new FileInfo(conversationFile.FullName
				.Replace(@"data\conversations\", @"data\localized\" + localization + @"\text\conversations\")
				.Replace(".conversation", ".stringtable"));
			return file.Exists ? file : null;
		}

		public static FileInfo FindVocalization(string conversationTag, int nodeId, string variant = "")
		{
			IDictionary<int, Vocalization> nodeDescriptors;
			if (!_audioFileDescriptors.TryGetValue(conversationTag, out nodeDescriptors))
				return null;

			Vocalization descriptor;
			if (!nodeDescriptors.TryGetValue(nodeId, out descriptor))
				return null;

			return descriptor.FindVariant(variant);
		}

		private static void LoadVocalizationFiles()
		{
			_audioFileDescriptors = new Dictionary<string, IDictionary<int, Vocalization>>();

			var dir = new DirectoryInfo(_gameDir.FullName + DataSubpath + @"audio\vocalization\vo wav files\");
			var files = new List<FileInfo>();
			RecursiveSearchAll(dir, "*.ogg", files);

			foreach (var file in files)
			{
				// skip generic voices
				if (file.Directory.Name.StartsWith("generic"))
					continue;

				string conversationTag;
				int nodeId;
				string suffix;

				if (Vocalization.TryParse(file.Name, out conversationTag, out nodeId, out suffix))
				{
					IDictionary<int, Vocalization> nodeDescriptors;
					if (!_audioFileDescriptors.TryGetValue(conversationTag, out nodeDescriptors))
					{
						nodeDescriptors = new Dictionary<int, Vocalization>();
						_audioFileDescriptors[conversationTag] = nodeDescriptors;
					}

					Vocalization main;
					if (!nodeDescriptors.TryGetValue(nodeId, out main))
					{
						var vocalization = new Vocalization();
						vocalization.AddVariant(suffix, file);
						nodeDescriptors[nodeId] = vocalization;
					}
					else
					{
						if (main.FindVariant(suffix) != null)
						{
							// skip duplicates

							//Console.WriteLine("Skip duplicate '{0}' for {1} node {2} ( {3})", suffix, conversationTag, nodeId,
							//	mainDescriptor.Files.Keys.Aggregate("", (accum, key) => accum + "'" + key + "' "));
						}
						else
						{
							//Console.WriteLine("Add variant '{0}' for {1} node {2} ( {3})", suffix, conversationTag, nodeId,
							//	mainDescriptor.Files.Keys.Aggregate("", (accum, key) => accum + "'" + key + "' "));
							main.AddVariant(suffix, file);
						}
					}
				}
				else
				{
					//Console.WriteLine("Skip {0}", file.Name);
				}
			}
		}

		private static FileInfo RecursiveSearch(DirectoryInfo dir, string searchPattern)
		{
			FileInfo[] files = dir.GetFiles(searchPattern);
			if (files.Length > 0)
				return files[0];

			foreach (var subdir in dir.GetDirectories())
			{
				FileInfo file = RecursiveSearch(subdir, searchPattern);
				if (file != null) return file;
			}
			return null;
		}

		private static void RecursiveSearchAll(DirectoryInfo dir, string searchPattern, IList<FileInfo> outFound)
		{
			foreach (FileInfo file in dir.GetFiles(searchPattern))
				outFound.Add(file);

			foreach (DirectoryInfo subdir in dir.GetDirectories())
				RecursiveSearchAll(subdir, searchPattern, outFound);
		}
	}
}
