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
using System.Text;
using System.Xml.Linq;

namespace PoEDlgExplorer
{
	public class FlowChartNode
	{
		public enum NodeType
		{
			TalkNode,
			PlayerResponseNode,
			TriggerConversationNode,
			ScriptNode,
			BankNode,
		}

		public enum NodeDisplayType
		{
			Hidden,
			Bark,
			Conversation,
		}

		public enum NodePersistence
		{
			None,
			MarkAsRead,
			OncePerConversation,
			OnceEver
		}

		public readonly NodeType Type;
		public readonly int Id;
		public readonly NodePersistence Persistence;
		public readonly NodeDisplayType DisplayType;
		public readonly string Condition;

		private readonly IList<string> _onEnterScripts = new List<string>();
		private readonly IList<string> _onExitScripts = new List<string>();
		private readonly IList<string> _onUpdateScripts = new List<string>();
		private readonly IList<DialogueLink> _links;

		public int LinkCount { get { return _links.Count; } }

		public FlowChartNode(XElement xFlowChartNode)
		{
			string type = xFlowChartNode.Attribute(Misc.XsiNs + "type").Value;
			Type = (NodeType)Enum.Parse(typeof(NodeType), type);

			Id = xFlowChartNode.IntElement("NodeID").Value;
			_links = xFlowChartNode.Element("Links").Elements().Select(x => new DialogueLink(Id, x)).ToList();
			Condition = FormatConditionalExpression(xFlowChartNode.Element("Conditionals"));

			if (Type == NodeType.BankNode)
			{
				Persistence = NodePersistence.None;
				DisplayType = NodeDisplayType.Hidden;
			}
			else
			{
				string persistence = xFlowChartNode.Element("Persistence").Value;
				Persistence = (NodePersistence)Enum.Parse(typeof(NodePersistence), persistence);

				string displayType = xFlowChartNode.Element("DisplayType").Value;
				DisplayType = (NodeDisplayType)Enum.Parse(typeof(NodeDisplayType), displayType);
			}

			foreach (var xScriptCall in xFlowChartNode.Element("OnEnterScripts").Elements())
				_onEnterScripts.Add(FormatScriptCall(xScriptCall));
			foreach (var xScriptCall in xFlowChartNode.Element("OnExitScripts").Elements())
				_onExitScripts.Add(FormatScriptCall(xScriptCall));
			foreach (var xScriptCall in xFlowChartNode.Element("OnUpdateScripts").Elements())
				_onUpdateScripts.Add(FormatScriptCall(xScriptCall));
		}

		public IEnumerable<string> GetOnEnterScripts()
		{
			return _onEnterScripts;
		}

		public IEnumerable<string> GetOnExitScripts()
		{
			return _onExitScripts;
		}

		public IEnumerable<string> GetOnUpdateScripts()
		{
			return _onUpdateScripts;
		}

		public DialogueLink GetLink(int index)
		{
			return _links[index];
		}

		public string GetBrief()
		{
			var sb = new StringBuilder();

			if (Condition != null)
				sb.Append("(").Append(Condition).Append(") ");

			sb.Append(string.Format("-> [ node-{0:00} ", Id));

			if (Type != NodeType.TalkNode)
				sb.Append(Type).Append(" ");

			if (DisplayType != NodeDisplayType.Hidden && DisplayType != NodeDisplayType.Conversation)
				sb.Append(DisplayType).Append(" ");

			if (Persistence != NodePersistence.None)
				sb.Append(Persistence).Append(" ");

			sb.Append("]");

			return sb.ToString();
		}

		private static string FormatConditionalExpression(XElement xExpr)
		{
			XAttribute exprType = xExpr.Attribute(Misc.XsiNs + "type");
			bool isConditionalExpression;

			if (xExpr.Name == "Conditionals")
			{
				isConditionalExpression = true;
			}
			else if (xExpr.Name == "ExpressionComponent")
			{
				if (exprType.Value == "ConditionalCall")
					isConditionalExpression = false;
				else if (exprType.Value == "ConditionalExpression")
					isConditionalExpression = true;
				else
					throw new ArgumentException("Unsupported ExpressionComponent type: " + exprType.Value);
			}
			else
			{
				throw new ArgumentException("Not a conditional: " + xExpr.Name);
			}

			if (isConditionalExpression)
			{
				XElement xComponents = xExpr.Element("Components");
				var elements = xComponents.Elements().ToArray();

				if (elements.Length == 0)
				{
					return null;
				}
				else if (elements.Length == 1)
				{
					return FormatConditionalExpression(elements[0]);
				}
				else
				{
					var sb = new StringBuilder();
					if (xExpr.Name != "Conditionals")
						sb.Append("(");

					sb.Append(FormatConditionalExpression(elements[0]));

					string op = (xExpr.Element("Operator").Value == "Or" ? " || " : " && ");
					for (int i = 1; i < elements.Length; i++)
						sb.Append(op).Append(FormatConditionalExpression(elements[i]));

					if (xExpr.Name != "Conditionals")
						sb.Append(")");
					return sb.ToString();
				}
			}
			else
			{
				return FormatConditionalCall(xExpr);
			}
		}

		private static string FormatConditionalCall(XElement xConditionalCall)
		{
			bool negate = xConditionalCall.BoolElement("Not").Value;

			IList<string> args;
			string functionName = TokenizeCall(xConditionalCall, out args);
			return FormatCondition(functionName, args, negate);
		}

		private static string FormatScriptCall(XElement xScriptCall)
		{
			if (xScriptCall.Name != "ScriptCall")
				throw new ArgumentException("Not a ScriptCall");

			XElement xData = xScriptCall.Elements().Single();
			if (xData.Name != "Data")
				throw new ArgumentException("Expected a single Data element, got " + xData.Name);

			IList<string> args;
			string functionName = TokenizeCall(xScriptCall, out args);
			return FormatAction(functionName, args);
		}

		private static string TokenizeCall(XElement xCall, out IList<string> args)
		{
			XElement xData = xCall.Element("Data");

			string signature = xData.Element("FullName").Value;
			int beginIndex = signature.IndexOf(" ", StringComparison.Ordinal) + 1;
			int endIndex = signature.IndexOf("(", StringComparison.Ordinal);
			string functionName = signature.Substring(beginIndex, endIndex - beginIndex);
			args = xData.Element("Parameters").Elements().Select(x => x.Value).ToList();

			return functionName;
		}

		private static string FormatCondition(string functionName, IList<string> args, bool negate)
		{
			bool negateHandled = false;
			string result;

			if (functionName == "IsGlobalValue")
			{
				if (args.Count != 3)
					throw new ArgumentException("IsGlobalValue takes 3 arguments");

				var compOp = (CompOp)Enum.Parse(typeof(CompOp), args[1]);
				if (negate)
				{
					compOp = compOp.Negated();
					negateHandled = true;
				}

				if (args[0].StartsWith("b")
					&& (compOp == CompOp.EqualTo || compOp == CompOp.NotEqualTo)
					&& (args[2] == "0" || args[2] == "1"))
				{
					if (args[2] == "1")
						result = (compOp == CompOp.EqualTo ? args[0] : ("!" + args[0]));
					else
						result = (compOp == CompOp.EqualTo ? ("!" + args[0]) : args[0]);
				}
				else
				{
					result = args[0] + " " + compOp.ToMathOp() + " " + args[2];
				}
			}
			else if (functionName == "IsPlayerAttributeScoreValue")
			{
				if (args.Count != 3)
					throw new ArgumentException("IsPlayerAttributeScoreValue takes 3 arguments");

				var compOp = (CompOp)Enum.Parse(typeof(CompOp), args[1]);
				result = args[0] + " " + compOp.ToMathOp() + " " + args[2];
			}
			else if (functionName == "IsPlayerBackground")
			{
				if (args.Count != 1)
					throw new ArgumentException("IsPlayerBackground takes 1 argument");

				result = args[0];
			}
			else if (functionName == "ReputationTagRankGreater")
			{
				if (args.Count != 3)
					throw new ArgumentException("ReputationTagRankGreater takes 3 arguments");

				if (args[1] == "Positive" || args[1].Length == 0)
					result = "Rep(" + args[0] + " > " + args[2] + ")";
				else if (args[1] == "Negative")
					result = "Rep(" + args[0] + " < -" + args[2] + ")";
				else
					throw new ArgumentOutOfRangeException("Invalid reputation axis: " + args[1]);
			}
			else if (functionName == "HasConversationNodeBeenPlayed")
			{
				if (args.Count != 2)
					throw new ArgumentException("HasConversationNodeBeenPlayed takes 2 arguments");

				result = "NodePlayed(" + FilterFunctionArgument(args[0]) + ", " + args[1] + ")";
			}
			else
			{
				result = FormatFunction(functionName, args);
			}

			if (negate && !negateHandled)
				result = "!(" + result + ")";

			return result;
		}

		private static string FormatAction(string functionName, IList<string> args)
		{
			string result;

			if (functionName == "DispositionAddPoints")
			{
				if (args.Count != 2)
					throw new ArgumentException("DispositionAddPoints takes 2 arguments");

				result = args[0] + " += " + args[1];
			}
			else
			{
				result = FormatFunction(functionName, args);
			}

			return result;
		}

		private static string FormatFunction(string functionName, IList<string> args)
		{
			var sb = new StringBuilder();
			sb.Append(functionName).Append("(");

			for (int i = 0; i < args.Count; i++)
			{
				sb.Append(FilterFunctionArgument(args[i]));
				if (i + 1 < args.Count)
					sb.Append(", ");
			}

			return sb.Append(")").ToString();
		}

		private static string FilterFunctionArgument(string arg)
		{
			// chop GUIDs
			Guid guid;
			if (Guid.TryParseExact(arg, "D", out guid))
				arg = arg.Substring(0, arg.IndexOf('-'));

			// chop paths
			if (arg.EndsWith(".conversation", StringComparison.OrdinalIgnoreCase))
			{
				arg = arg.Substring(arg.LastIndexOf("/", StringComparison.Ordinal) + 1);
				arg = arg.Substring(0, arg.Length - ".conversation".Length);
			}
			if (arg.EndsWith(".quest", StringComparison.OrdinalIgnoreCase))
			{
				arg = arg.Substring(arg.LastIndexOf("/", StringComparison.Ordinal) + 1);
				arg = arg.Substring(0, arg.Length - ".quest".Length);
			}

			return arg;
		}

	}
}
