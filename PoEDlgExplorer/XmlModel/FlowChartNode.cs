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
using System.Text;
using System.Xml.Serialization;

namespace PoEDlgExplorer.XmlModel
{
	public enum BookmarkType
	{
		Unassigned,
		Node,
		GhostNode
	}

	public abstract class FlowChartNode
	{
		public int NodeID;
		public string Comments;
		public int PackageID;
		public int ContainerNodeID;
		public List<FlowChartLink> Links;
		public ConditionalExpression Conditionals;
		public List<ScriptCall> OnEnterScripts;
		public List<ScriptCall> OnExitScripts;
		public List<ScriptCall> OnUpdateScripts;

		[XmlIgnore]
		public bool IsRoot { get { return NodeID == 0; } }

		public string GetBrief()
		{
			var sb = new StringBuilder();

			sb.Append(string.Format("[ node-{0:00} ", NodeID));
			ExtendBrief(sb);
			sb.Append("]");

			return sb.ToString();
		}

		protected virtual void ExtendBrief(StringBuilder sb)
		{
		}
	}

	[Serializable]
	public class FlowChartLink
	{
		public int FromNodeID;
		public int ToNodeID;
		public bool PointsToGhost;

		public string GetBrief()
		{
			var sb = new StringBuilder();
			sb.Append("[ ");

			if (PointsToGhost)
				sb.Append("PointsToGhost ");

			ExtendBrief(sb);
			sb.Append("]");
			return sb.ToString();
		}

		protected virtual void ExtendBrief(StringBuilder sb)
		{
		}
	}
}
