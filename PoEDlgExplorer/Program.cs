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
using PoEDlgExplorer.XmlModel;

namespace PoEDlgExplorer
{
	static class Program
	{
		private enum Command
		{
			None,
			PickLine,
			Rewind,
			ToggleAudio,
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
						if (numDialogueLines > 0)
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
				}
				else if (keyInfo.Key == ConsoleKey.Spacebar)
				{
					command = Command.ToggleAudio;
				}
			} while (command == Command.None);

			return command;
		}

		private static Conversation LoadConversation(string conversationTag, string localization)
		{
			FileInfo conversationFile = ResourceLocator.FindConversation(conversationTag);
			if (conversationFile == null)
				throw new ArgumentException("Conversation " + conversationTag + " file not found");

			return LoadConversationByPath(conversationFile, localization);
		}

		private static Conversation LoadConversationByPath(FileInfo conversationFile, string localization)
		{
			string tag = Path.GetFileNameWithoutExtension(conversationFile.Name);

			FileInfo stringTableFile = ResourceLocator.FindStringTableByPath(conversationFile, localization);
			if (stringTableFile == null)
				throw new ArgumentException("String table file not found for conversation " + tag + " (" + localization + ")");

			var conversationData = Xml.Deserialize<ConversationData>(conversationFile.FullName);
			var stringTable = Xml.Deserialize<StringTable>(stringTableFile.FullName);

			return new Conversation(tag, conversationData, stringTable);
		}

		private static void PrintNodeInfo(FlowChartNode node, int indendation)
		{
			var space = new string(' ', indendation);

			Console.WriteLine("{0}{1}", space, node.GetBrief());
			foreach (var script in node.OnEnterScripts)
				Console.WriteLine("{0}  on enter  : {1}", space, script.Format());
			foreach (var script in node.OnExitScripts)
				Console.WriteLine("{0}  on exit   : {1}", space, script.Format());
			foreach (var script in node.OnUpdateScripts)
				Console.WriteLine("{0}  on update : {1}", space, script.Format());
		}

		private static void LoadNode(Conversation conversation, int nodeId, bool playAudio)
		{
			FlowChartNode node = conversation.FindNode(nodeId);
			Console.WriteLine("[node-{0:00}]", node.NodeID);

			FileInfo audioFile = ResourceLocator.FindVocalization(conversation.Tag, nodeId);
			if (audioFile != null)
				Console.WriteLine("[audio]");

			StringTable.Entry text = conversation.FindText(node.NodeID);
			if (text != null)
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("\n{0}", text.Format());
				Console.ForegroundColor = ConsoleColor.Gray;
			}
			Console.WriteLine("\n\n");

			if (node.Links.Count == 0)
			{
				Console.WriteLine("(End. Hit BACKSPACE to rewind)");
			}
			else
			{
				for (int i = 0; i < node.Links.Count; i++)
				{
					FlowChartLink link = node.Links[i];
					text = conversation.FindText(link.ToNodeID);

					Console.Write("({0}) {1} ", i + 1, link.GetBrief());

					if (ResourceLocator.FindVocalization(conversation.Tag, link.ToNodeID) != null)
						Console.Write("[audio] ");

					PrintNodeInfo(conversation.FindNode(link.ToNodeID), 0);

					if (!link.PointsToGhost)
						Console.ForegroundColor = ConsoleColor.White;
					Console.WriteLine("{0}\n\n", (node.Links.Count == 1 ? "[continue]" : text.Format()));
					if (!link.PointsToGhost)
						Console.ForegroundColor = ConsoleColor.Gray;
				}
			}

			if ((audioFile != null) && playAudio)
				AudioServer.Play(audioFile);
		}

		// ReSharper disable once UnusedMember.Local
		private static void ValidateAllConversations()
		{
			IList<FileInfo> conversationFiles = ResourceLocator.FindAllConversations();
			int missingFileCount = 0;
			int unparsableFileCount = 0;

			foreach (var conversationFile in conversationFiles)
			{
				try
				{
					LoadConversationByPath(conversationFile, "en");
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
					Console.Error.WriteLine("Error: {0}", e.Message);
					unparsableFileCount++;
				}
			}

			Console.WriteLine("total conversation files: {0}", conversationFiles.Count);
			Console.WriteLine("files without stringtable: {0}", missingFileCount);
			Console.WriteLine("unparsable files: {0}", unparsableFileCount);
			Console.WriteLine();
		}

		static void Main(string[] args)
		{
			//if (args.Length == 0)
			//	args = new string[]
			//	{
			//		@"c:\Games\Pillars of Eternity\PillarsOfEternity_Data\data\conversations\companions\companion_cv_durance_v2.conversation"
			//	};

			try
			{
				if (args.Length > 0)
					ResourceLocator.Initialize(args[0]);
				else
					ResourceLocator.Initialize(Settings.Instance.GamePath);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error: " + e);
				Console.ReadKey(true);
				return;
			}

			// ValidateAllConversations();

			if (args.Length == 0 || !args[0].EndsWith(".conversation"))
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
			Console.WriteLine("SPACE:     toggle audio");
			Console.WriteLine("ESC:       quit");
			Console.WriteLine("\n");

			Conversation conversation;
			try
			{
				string localization = Settings.Instance.Localization;
				conversation = LoadConversationByPath(new FileInfo(args[0]), localization);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error: " + e);
				Console.ReadKey(true);
				return;
			}

			bool audioEnabled = Settings.Instance.PlayAudio;

			var nodeStack = new Stack<int>();
			nodeStack.Push(0);
			LoadNode(conversation, nodeStack.Peek(), audioEnabled);

			Command command;
			do
			{
				FlowChartNode node = conversation.FindNode(nodeStack.Peek());
				int pickedLine;
				command = ReadInput(node.Links.Count, out pickedLine);

				switch (command)
				{
					case Command.PickLine:
						AudioServer.Stop();
						Console.Clear();

						FlowChartLink link = node.Links[pickedLine - 1];
						nodeStack.Push(link.ToNodeID);

						LoadNode(conversation, nodeStack.Peek(), audioEnabled);
						break;
					case Command.Rewind:
						if (nodeStack.Count > 1)
						{
							AudioServer.Stop();
							Console.Clear();

							nodeStack.Pop();
							LoadNode(conversation, nodeStack.Peek(), false);
						}
						break;
					case Command.QuitProgram:
						break;
					case Command.ToggleAudio:
						if (audioEnabled)
						{
							audioEnabled = false;
							AudioServer.Stop();
						}
						else
						{
							audioEnabled = true;
							FileInfo audioFile = ResourceLocator.FindVocalization(conversation.Tag, node.NodeID);
							if (audioFile != null)
								AudioServer.Play(audioFile);
						}
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			} while (command != Command.QuitProgram);

			AudioServer.Kill();
		}
	}
}
