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
	public enum LogicalOperator
	{
		And,
		Or
	}

	public abstract class ExpressionComponent
	{
		public abstract string Format();
	}

	[Serializable]
	public class ConditionalCall : ExpressionComponent
	{
		public ScriptCallData Data;
		public bool Not;
		public LogicalOperator Operator;

		public override string Format()
		{
			return Data.FormatCondition(Not);
		}
	}

	[Serializable]
	[XmlInclude(typeof(ConditionalCall)), XmlInclude(typeof(ConditionalExpression))]
	public class ConditionalExpression : ExpressionComponent
	{
		public LogicalOperator Operator;
		public List<ExpressionComponent> Components;

		public override string Format()
		{
			if (Components.Count == 0)
			{
				return "";
			}
			else if (Components.Count == 1)
			{
				return Components[0].Format();
			}
			else
			{
				var sb = new StringBuilder();
				sb.Append("(");
				sb.Append(Components[0].Format());

				string op = (Operator == LogicalOperator.And ? " && " : " || ");
				for (int i = 1; i < Components.Count; i++)
					sb.Append(op).Append(Components[i].Format());

				sb.Append(")");
				return sb.ToString();
			}
		}
	}
}
