/*******************************************************************************
 * Copyright 2015 Valentin Milea
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 ******************************************************************************/

using System.Collections.Generic;
using System.Xml.Linq;
using System.Reflection;
using System.IO;

namespace PoEDlgExplorer
{
	public static class Settings
	{
		private static readonly IDictionary<string, string> Entries = new Dictionary<string, string>();

		public static void Load()
		{
			Entries.Clear();

			string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase);
			XElement xSettings = XElement.Load(dir + @"\Settings.xml");
			foreach (var xEntry in xSettings.Elements("Entry"))
			{
				Entries[xEntry.Attribute("key").Value] = xEntry.Attribute("value").Value;
			}
		}

		public static string GetString(string key)
		{
			return Entries[key];
		}

		public static int GetInt(string key)
		{
			return int.Parse(Entries[key]);
		}
	}
}
