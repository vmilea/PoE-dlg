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

using System.Xml.Linq;

namespace PoEDlgExplorer
{
	public static class Misc
	{
		public static readonly XNamespace XsiNs = "http://www.w3.org/2001/XMLSchema-instance";
	}

	public static class XElementEx
	{
		public static int? IntElement(this XElement xElement, string name)
		{
			var x = xElement.Element(name);
			return x == null ? (int?)null : int.Parse(x.Value);
		}

		public static int? IntAttribute(this XElement xElement, string name)
		{
			var x = xElement.Attribute(name);
			return x == null ? (int?)null : int.Parse(x.Value);
		}

		public static bool? BoolElement(this XElement xElement, string name)
		{
			var x = xElement.Element(name);
			return x == null ? (bool?)null : bool.Parse(x.Value);
		}

		public static bool? BoolAttribute(this XElement xElement, string name)
		{
			var x = xElement.Attribute(name);
			return x == null ? (bool?)null : bool.Parse(x.Value);
		}
	}
}
