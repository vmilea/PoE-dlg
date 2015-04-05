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
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace PoEDlgExplorer
{
	static class Program
	{
		private enum Command
		{
			None,
			PickLine,
			Rewind,
			QuitProgram,
		}

		private static Command ReadInput(int numDialogueLines, out int pickedLine)
		{
			int accumulator = 0;
			pickedLine = 0;

			Command command = Command.None;
			do
			{
				ConsoleKeyInfo keyInfo = Console.ReadKey(true);

				if (keyInfo.Key == ConsoleKey.Escape)
				{
					command = Command.QuitProgram;
				}
				else if (keyInfo.Key == ConsoleKey.Backspace || keyInfo.Key == ConsoleKey.Subtract)
				{
					if (accumulator == 0)
					{
						command = Command.Rewind;
					}
					else
					{
						accumulator /= 10;
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
						Console.Write(' ');
						Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
					}
				}
				else if (keyInfo.Key == ConsoleKey.Spacebar)
				{
					if (numDialogueLines == 1)
					{
						pickedLine = 1;
						command = Command.PickLine;
					}
				}
				else if (keyInfo.Key == ConsoleKey.Enter)
				{
					if (numDialogueLines == 1)
						accumulator = 1;

					if (1 <= accumulator && accumulator <= numDialogueLines)
					{
						pickedLine = accumulator;
						command = Command.PickLine;
					}
					else
					{
						if (numDialogueLines == 0)
							Console.WriteLine("Hit BACKSPACE to rewind\n");
						else
							Console.WriteLine("Valid range: {0} .. {1}\n", 1, numDialogueLines);

						Debug.Assert(accumulator == 0);
					}
				}
				else if ('0' <= keyInfo.KeyChar && keyInfo.KeyChar <= '9')
				{
					int total = 10 * accumulator + (keyInfo.KeyChar - '0');

					if (0 < total && total <= numDialogueLines)
					{
						Console.Write(keyInfo.KeyChar);
						accumulator = total;

						// pick dialogue line number if unambiguous
						if (10 * accumulator > numDialogueLines)
						{
							pickedLine = accumulator;
							command = Command.PickLine;
						}
					}
					else if (numDialogueLines == 0)
					{
						Console.WriteLine("Hit BACKSPACE to rewind\n");
					}
				}
			} while (command == Command.None);

			return command;
		}

		private static Conversation LoadConversation(string filePath, string language)
		{
			string localizedFilePath = filePath
				.Replace(@"data\conversations\", @"data\localized\" + language + @"\text\conversations\")
				.Replace(".conversation", ".stringtable");

			var conversationFile = new FileInfo(filePath);
			var localizedFile = new FileInfo(localizedFilePath);

			if (conversationFile.Extension != ".conversation")
				throw new ArgumentException("Not a conversation file: " + filePath);
			if (!conversationFile.Exists)
				throw new FileNotFoundException("File not found: " + conversationFile.FullName);
			if (!localizedFile.Exists)
				throw new FileNotFoundException("File not found: " + localizedFile.FullName);

			return new Conversation(XElement.Load(filePath), XElement.Load(localizedFilePath));
		}

		private static void PrintNodeInfo(FlowChartNode node, int indendation)
		{
			var space = new string(' ', indendation);

			Console.WriteLine("{0}{1}", space, node.GetBrief());
			foreach (var script in node.GetOnEnterScripts())
				Console.WriteLine("{0}  on enter  : {1}", space, script);
			foreach (var script in node.GetOnExitScripts())
				Console.WriteLine("{0}  on exit   : {1}", space, script);
			foreach (var script in node.GetOnUpdateScripts())
				Console.WriteLine("{0}  on update : {1}", space, script);
		}

		private static void PrintNode(Conversation conversation, int nodeId)
		{
			FlowChartNode node = conversation.GetNode(nodeId);

			Console.WriteLine("[node-{0:00}]\n\n", node.Id);

			for (int i = 0; i < node.LinkCount; i++)
			{
				DialogueLink link = node.GetLink(i);
				StringTableEntry se = conversation.GetStringEntry(link.TargetId);

				Console.Write("({0}) {1} ", i + 1, link.GetBrief());

				PrintNodeInfo(conversation.GetNode(link.TargetId), 0);
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("{0}\n\n", se.Format());
				Console.ForegroundColor = ConsoleColor.Gray;
			}
		}

		// ReSharper disable once UnusedMember.Local
		private static void ValidateAllConversations(string rootPath)
		{
			int fileCount = 0;
			int missingFileCount = 0;
			int unparsableFileCount = 0;

			Action<DirectoryInfo> traverse = null;
			traverse = dirInfo =>
			{
				foreach (var file in dirInfo.GetFiles("*.conversation"))
				{
					fileCount++;
					try
					{
						LoadConversation(file.FullName, "en");
					}
					catch (FileNotFoundException)
					{
						// ignore
						missingFileCount++;
					}
					catch (IOException e)
					{
						Console.Error.WriteLine("I/O error: {0}", e.Message);
						unparsableFileCount++;
					}
					catch (ArgumentException e)
					{
						Console.Error.WriteLine("Couldn't parse {0}. Error: {1}", file.Name, e.Message);
						unparsableFileCount++;
					}
				}

				foreach (var subdir in dirInfo.GetDirectories())
					traverse(subdir);
			};

			traverse(new DirectoryInfo(rootPath));

			Console.WriteLine("total conversation files: {0}", fileCount);
			Console.WriteLine("files without stringtable: {0}", missingFileCount);
			Console.WriteLine("unparsable files: {0}", unparsableFileCount);
			Console.WriteLine();
		}

		static void Main(string[] args)
		{
			Settings.Load();

			// string poePath = Settings.GetString("poe_path");
			// ValidateAllConversations(poePath + @"PillarsOfEternity_Data\data\conversations\");

			if (args.Length == 0)
			{
				Console.WriteLine("Usage: PoEDlgExplorer.exe <conversation path>");
				Console.ReadKey(true);
				return;
			}

			Console.WriteLine("[HOW-TO]");
			Console.WriteLine();
			Console.WriteLine("0..9:      select a dialogue line");
			Console.WriteLine("ENTER:     commit when there are 10+ options");
			Console.WriteLine("BACKSPACE: rewind");
			Console.WriteLine("ESC:       quit");
			Console.WriteLine("\n");

			Conversation conversation;
			try
			{
				string localization = Settings.GetString("localization");
				conversation = LoadConversation(args[0], localization);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Parsing error: " + e);
				Console.ReadKey(true);
				return;
			}

			var nodeStack = new Stack<int>();
			nodeStack.Push(0);

			bool quit = false;
			do
			{
				PrintNode(conversation, nodeStack.Peek());

				FlowChartNode node = conversation.GetNode(nodeStack.Peek());
				int pickedLine;
				Command command = ReadInput(node.LinkCount, out pickedLine);

				switch (command)
				{
					case Command.PickLine:
						DialogueLink link = node.GetLink(pickedLine - 1);
						nodeStack.Push(link.TargetId);
						break;
					case Command.Rewind:
						if (nodeStack.Count > 1)
							nodeStack.Pop();
						break;
					case Command.QuitProgram:
						quit = true;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				Console.Clear();
			} while (!quit);
		}
	}
}
