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

namespace PoEDlgExplorer
{
	public static class CompOpEx
	{
		public static string ToMathOp(this CompOp op)
		{
			switch (op)
			{
				case CompOp.EqualTo:
					return "==";
				case CompOp.NotEqualTo:
					return "!=";
				case CompOp.LessThan:
					return "<";
				case CompOp.LessThanOrEqualTo:
					return "<=";
				case CompOp.GreaterThan:
					return ">";
				case CompOp.GreaterThanOrEqualTo:
					return ">=";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static CompOp Negated(this CompOp op)
		{
			switch (op)
			{
				case CompOp.EqualTo:
					return CompOp.NotEqualTo;
				case CompOp.NotEqualTo:
					return CompOp.EqualTo;
				case CompOp.LessThan:
					return CompOp.GreaterThanOrEqualTo;
				case CompOp.LessThanOrEqualTo:
					return CompOp.GreaterThan;
				case CompOp.GreaterThan:
					return CompOp.LessThanOrEqualTo;
				case CompOp.GreaterThanOrEqualTo:
					return CompOp.LessThan;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public enum CompOp
	{
		EqualTo,
		NotEqualTo,
		LessThan,
		LessThanOrEqualTo,
		GreaterThan,
		GreaterThanOrEqualTo
	}
}
