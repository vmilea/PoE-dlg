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

namespace PoEDlgExplorer.XmlModel
{
	public enum ComparisonOperator
	{
		EqualTo,
		NotEqualTo,
		LessThan,
		LessThanOrEqualTo,
		GreaterThan,
		GreaterThanOrEqualTo
	}

	public static class ComparisonOperatorEx
	{
		public static string ToMathOp(this ComparisonOperator op)
		{
			switch (op)
			{
				case ComparisonOperator.EqualTo:
					return "==";
				case ComparisonOperator.NotEqualTo:
					return "!=";
				case ComparisonOperator.LessThan:
					return "<";
				case ComparisonOperator.LessThanOrEqualTo:
					return "<=";
				case ComparisonOperator.GreaterThan:
					return ">";
				case ComparisonOperator.GreaterThanOrEqualTo:
					return ">=";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static ComparisonOperator Negated(this ComparisonOperator op)
		{
			switch (op)
			{
				case ComparisonOperator.EqualTo:
					return ComparisonOperator.NotEqualTo;
				case ComparisonOperator.NotEqualTo:
					return ComparisonOperator.EqualTo;
				case ComparisonOperator.LessThan:
					return ComparisonOperator.GreaterThanOrEqualTo;
				case ComparisonOperator.LessThanOrEqualTo:
					return ComparisonOperator.GreaterThan;
				case ComparisonOperator.GreaterThan:
					return ComparisonOperator.LessThanOrEqualTo;
				case ComparisonOperator.GreaterThanOrEqualTo:
					return ComparisonOperator.LessThan;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
