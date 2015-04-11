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
	[Serializable]
	public struct ScriptCallData
	{
		public string FullName;
		public List<string> Parameters;

		[XmlIgnore]
		public string FunctionName
		{
			get
			{
				int beginIndex = FullName.IndexOf(" ", StringComparison.Ordinal) + 1;
				int endIndex = FullName.IndexOf("(", StringComparison.Ordinal);
				return FullName.Substring(beginIndex, endIndex - beginIndex);
			}
		}

		public string FormatAction()
		{
			string result;

			switch (FunctionName)
			{
				case "SetGlobalValue":
					if (Parameters.Count != 2)
						throw new ArgumentException("SetGlobalValue takes 2 arguments");

					result = Parameters[0] + " = " + Parameters[1];
					break;

				case "IncrementGlobalValue":
					if (Parameters.Count != 2)
						throw new ArgumentException("IncrementGlobalValue takes 2 arguments");

					result = Parameters[0] + " += " + Parameters[1];
					break;

				case "DispositionAddPoints":
					if (Parameters.Count != 2)
						throw new ArgumentException("DispositionAddPoints takes 2 arguments");

					result = Parameters[0] + " += " + Parameters[1];
					break;

				default:
					result = FormatFunction();
					break;
			}

			return result;
		}

		public string FormatCondition(bool negate)
		{
			string result;
			bool negateHandled = false;
			ComparisonOperator comparisonOperator;

			switch (FunctionName)
			{
				case "IsGlobalValue":
					if (Parameters.Count != 3)
						throw new ArgumentException("IsGlobalValue takes 3 arguments");

					comparisonOperator = (ComparisonOperator)Enum.Parse(typeof(ComparisonOperator), Parameters[1]);
					if (negate)
					{
						comparisonOperator = comparisonOperator.Negated();
						negateHandled = true;
					}

					if (Parameters[0].StartsWith("b")
						&& (comparisonOperator == ComparisonOperator.EqualTo || comparisonOperator == ComparisonOperator.NotEqualTo)
						&& (Parameters[2] == "0" || Parameters[2] == "1"))
					{
						if (Parameters[2] == "1")
							result = (comparisonOperator == ComparisonOperator.EqualTo ? Parameters[0] : ("!" + Parameters[0]));
						else
							result = (comparisonOperator == ComparisonOperator.EqualTo ? ("!" + Parameters[0]) : Parameters[0]);
					}
					else
					{
						result = Parameters[0] + " " + comparisonOperator.ToMathOp() + " " + Parameters[2];
					}
					break;

				case "IsPlayerAttributeScoreValue":
					if (Parameters.Count != 3)
						throw new ArgumentException("IsPlayerAttributeScoreValue takes 3 arguments");

					comparisonOperator = (ComparisonOperator)Enum.Parse(typeof(ComparisonOperator), Parameters[1]);
					result = Parameters[0] + " " + comparisonOperator.ToMathOp() + " " + Parameters[2];
					break;

				case "IsPlayerBackground":
					if (Parameters.Count != 1)
						throw new ArgumentException("IsPlayerBackground takes 1 argument");

					result = Parameters[0];
					break;

				case "ReputationTagRankGreater":
					if (Parameters.Count != 3)
						throw new ArgumentException("ReputationTagRankGreater takes 3 arguments");

					if (Parameters[1] == "Positive" || Parameters[1].Length == 0)
						result = "Rep(" + Parameters[0] + " > " + Parameters[2] + ")";
					else if (Parameters[1] == "Negative")
						result = "Rep(" + Parameters[0] + " < -" + Parameters[2] + ")";
					else
						throw new ArgumentOutOfRangeException("Invalid reputation axis: " + Parameters[1]);
					break;

				case "HasConversationNodeBeenPlayed":
					if (Parameters.Count != 2)
						throw new ArgumentException("HasConversationNodeBeenPlayed takes 2 arguments");

					result = "NodePlayed(" + FilterFunctionArgument(Parameters[0]) + ", " + Parameters[1] + ")";
					break;

				default:
					result = FormatFunction();
					break;
			}

			if (negate && !negateHandled)
			{
				if (result.Contains("="))
					result = "!(" + result + ")";
				else
					result = "!" + result;
			}
			return result;
		}

		private string FormatFunction()
		{
			var sb = new StringBuilder();
			sb.Append(FunctionName).Append("(");

			for (int i = 0; i < Parameters.Count; i++)
			{
				sb.Append(FilterFunctionArgument(Parameters[i]));
				if (i + 1 < Parameters.Count)
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

	[Serializable]
	public struct ScriptCall
	{
		public ScriptCallData Data;

		public string Format()
		{
			return Data.FormatAction();
		}
	}
}
