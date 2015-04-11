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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace PoEDlgExplorer.XmlModel
{
	[Serializable]
	[XmlRoot("StringTableFile")]
	public sealed class StringTable
	{
		[Serializable]
		public class Entry
		{
			public int ID;
			public string DefaultText;
			public string FemaleText;

			public string Format()
			{
				if (FemaleText.Length == 0)
					return DefaultText;
				else
					return string.Format("{0} / fem: {1}", DefaultText, FemaleText);
			}
		}

		public string Name;
		public int NextEntryID;
		public int EntryCount;
		public List<Entry> Entries;

		[XmlIgnore]
		private IDictionary<int, Entry> _entryMap;

		public Entry this[int id]
		{
			get { return EntryMap[id]; }
		}

		[XmlIgnore]
		public IDictionary<int, Entry> EntryMap
		{
			get
			{
				if (_entryMap == null)
				{
					if (Entries.Count != EntryCount)
						throw new ArgumentException("Wrong EntryCount");
					_entryMap = Entries.ToDictionary(x => x.ID);
				}
				return _entryMap;
			}
		}
	}
}
