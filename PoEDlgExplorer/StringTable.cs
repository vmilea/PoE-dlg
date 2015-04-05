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

namespace PoEDlgExplorer
{
	public struct StringTableEntry
	{
		public readonly int Id;
		public readonly string DefaultText;
		public readonly string FemaleText;

		public StringTableEntry(int id, string defaultText, string femaleText = null)
		{
			Id = id;
			DefaultText = defaultText;
			FemaleText = femaleText;
		}

		public string Format()
		{
			if (FemaleText == null)
				return DefaultText;
			else
				return string.Format("{0} / fem: {1}", DefaultText, FemaleText);
		}
	}
}
