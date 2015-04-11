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
	public struct Bookmark
	{
		public BookmarkType Type;
		public string Name;
		public int NodeID;
		public int GhostNodeParentID;
	}

	[Serializable]
	[
		XmlInclude(typeof(DialogueLink)),
		XmlInclude(typeof(DialogueNode)),
		XmlInclude(typeof(TalkNode)),
		XmlInclude(typeof(PlayerResponseNode)),
		XmlInclude(typeof(ScriptNode)),
		XmlInclude(typeof(TriggerConversationNode)),
		XmlInclude(typeof(BankNode))]
	public class FlowChartData
	{
		public int NextNodeID;
		public List<FlowChartNode> Nodes;
		public List<Bookmark> Bookmarks;

		[XmlIgnore]
		private IDictionary<int, FlowChartNode> _nodeMap;

		[XmlIgnore]
		public IDictionary<int, FlowChartNode> NodeMap
		{
			get { return _nodeMap ?? (_nodeMap = Nodes.ToDictionary(x => x.NodeID)); }
		}

		[XmlIgnore]
		public ICollection<int> NodeIDs
		{
			get { return NodeMap.Keys; }
		}

		public FlowChartNode FindNode(int nodeID)
		{
			return (NodeMap.ContainsKey(nodeID) ? NodeMap[nodeID] : null);
		}
	}
}
