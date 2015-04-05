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
using System.Text;
using System.Xml.Linq;

namespace PoEDlgExplorer
{
	public struct DialogueLink
	{
		public enum QuestionMode
		{
			ShowOnce,
			ShowAlways,
			ShowNever
		}

		public readonly int TargetId;
		public readonly bool PointsToGhost;
		public readonly bool PlayQuestionNodeVo;
		public readonly QuestionMode QuestionNodeTextDisplay;

		public DialogueLink(int fromNodeId, XElement xFlowChartLink)
		{
			if (xFlowChartLink.Attribute(Misc.XsiNs + "type").Value != "DialogueLink")
				throw new ArgumentException("Not a DialogueLink");

			if (fromNodeId != xFlowChartLink.IntElement("FromNodeID"))
				throw new ArgumentException(
					"Invalid FromNodeID: expected " + fromNodeId + ", got " + xFlowChartLink.IntElement("FromNodeID"));

			TargetId = xFlowChartLink.IntElement("ToNodeID").Value;
			PointsToGhost = xFlowChartLink.BoolElement("PointsToGhost").Value;
			PlayQuestionNodeVo = xFlowChartLink.BoolElement("PlayQuestionNodeVO").Value;
			QuestionNodeTextDisplay = (QuestionMode)Enum.Parse(typeof(QuestionMode),
				xFlowChartLink.Element("QuestionNodeTextDisplay").Value);
		}

		public string GetBrief()
		{
			var sb = new StringBuilder();
			sb.Append("[ link ");

			if (QuestionNodeTextDisplay != QuestionMode.ShowOnce)
				sb.Append(QuestionNodeTextDisplay).Append(" ");

			if (PointsToGhost)
				sb.Append("PointsToGhost ");
			if (!PlayQuestionNodeVo)
				sb.Append("!PlayQuestionNodeVO ");
			sb.Append("]");

			return sb.ToString();
		}
	}
}
