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
using System.Xml.Linq;

namespace PoEDlgExplorer
{
	public class Conversation
	{
		private readonly IDictionary<int, FlowChartNode> _nodes;
		private readonly IDictionary<int, StringTableEntry> _stringTable;

		public int NodeCount { get { return _nodes.Count; } }

		public Conversation(XElement xConversation, XElement xStringTable)
		{
			_stringTable = ParseStringTable(xStringTable);

			_nodes = new Dictionary<int, FlowChartNode>();
			foreach (var xNode in xConversation.Element("Nodes").Elements())
			{
				var node = new FlowChartNode(xNode);
				_nodes[node.Id] = node;
			}
		}

		public FlowChartNode GetStartNode()
		{
			return _nodes[0];
		}

		public FlowChartNode GetNode(int nodeId)
		{
			return _nodes[nodeId];
		}

		public StringTableEntry GetStringEntry(int nodeId)
		{
			return _stringTable[nodeId];
		}

		private static IDictionary<int, StringTableEntry> ParseStringTable(XElement xStringTable)
		{
			var entries = new Dictionary<int, StringTableEntry>();

			foreach (var entry in xStringTable.Elements("Entries").Elements())
			{
				int id = entry.IntElement("ID").Value;
				string defaultText = entry.Element("DefaultText").Value;
				string femaleText = entry.Element("FemaleText").Value;

				if (entries.ContainsKey(id))
					throw new ArgumentException("String entry ID is not unique");

				entries[id] = new StringTableEntry(id, defaultText,
					(femaleText.Length == 0 ? null : femaleText));
			}

			return entries;
		}
	}
}
