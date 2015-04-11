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
using PoEDlgExplorer.XmlModel;

namespace PoEDlgExplorer
{
	public sealed class Conversation
	{
		public readonly string Tag;
		public readonly ConversationData Data;
		public readonly StringTable StringTable;

		public IDictionary<int, FlowChartNode> Nodes { get { return Data.NodeMap; } }

		public IDictionary<int, StringTable.Entry> Text { get { return StringTable.EntryMap; } }

		public Conversation(string tag, ConversationData conversationData, StringTable stringTable)
		{
			Tag = tag;
			Data = conversationData;
			StringTable = stringTable;
		}

		public FlowChartNode FindNode(int nodeId)
		{
			return (Nodes.ContainsKey(nodeId) ? Nodes[nodeId] : null);
		}

		public StringTable.Entry FindText(int nodeId)
		{
			return (Text.ContainsKey(nodeId) ? Text[nodeId] : null);
		}
	}
}
