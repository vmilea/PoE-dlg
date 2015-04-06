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

			return new Conversation(tag,
				XElement.Load(conversationFile.FullName), XElement.Load(stringTableFile.FullName));
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

		private static void LoadNode(Conversation conversation, int nodeId, bool playAudio)
		{
			FlowChartNode node = conversation.GetNode(nodeId);
			Console.WriteLine("[node-{0:00}]", node.Id);

			StringTableEntry text;
			if (conversation.HasStringEntry(nodeId))
			{
				FileInfo audioFile = ResourceLocator.FindVocalization(conversation.Tag, nodeId);
				if (audioFile != null)
				{
					Console.WriteLine("[vocalized]");
					if (playAudio)
						AudioServer.Play(audioFile);
				}

				Console.WriteLine();
				text = conversation.GetStringEntry(node.Id);
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("{0}", text.Format());
				Console.ForegroundColor = ConsoleColor.Gray;
			}
			Console.WriteLine("\n\n");

			if (node.LinkCount == 0)
			{
				Console.WriteLine("(End. Hit BACKSPACE to rewind)");
			}
			else
			{
				for (int i = 0; i < node.LinkCount; i++)
				{
					DialogueLink link = node.GetLink(i);
					text = conversation.GetStringEntry(link.TargetId);

					Console.Write("({0}) {1} ", i + 1, link.GetBrief());

					PrintNodeInfo(conversation.GetNode(link.TargetId), 0);

					Console.ForegroundColor = ConsoleColor.White;
					Console.WriteLine("{0}\n\n", text.Format());
					Console.ForegroundColor = ConsoleColor.Gray;
				}
			}
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
			Settings.Load();

			if (args.Length == 0)
				args = new string[]
				{
					@"c:\Games\Pillars of Eternity\PillarsOfEternity_Data\data\conversations\companions\companion_cv_durance_v2.conversation"
				};

			try
			{
				if (args.Length > 0)
					ResourceLocator.Initialize(args[0]);
				else
					ResourceLocator.Initialize(Settings.GetString("poe_path"));
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
				string localization = Settings.GetString("localization");
				conversation = LoadConversationByPath(new FileInfo(args[0]), localization);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error: " + e);
				Console.ReadKey(true);
				return;
			}

			bool playAudio = Settings.GetBool("play_audio");

			var nodeStack = new Stack<int>();
			nodeStack.Push(0);
			LoadNode(conversation, nodeStack.Peek(), playAudio);

			Command command;
			do
			{
				FlowChartNode node = conversation.GetNode(nodeStack.Peek());
				int pickedLine;
				command = ReadInput(node.LinkCount, out pickedLine);

				switch (command)
				{
					case Command.PickLine:
						DialogueLink link = node.GetLink(pickedLine - 1);
						nodeStack.Push(link.TargetId);
						AudioServer.Stop();
						Console.Clear();
						LoadNode(conversation, nodeStack.Peek(), playAudio);
						break;
					case Command.Rewind:
						if (nodeStack.Count > 1)
						{
							nodeStack.Pop();
							AudioServer.Stop();
							Console.Clear();
							LoadNode(conversation, nodeStack.Peek(), false);
						}
						break;
					case Command.QuitProgram:
						break;
					case Command.ToggleAudio:
						if (playAudio)
						{
							playAudio = false;
							AudioServer.Stop();
						}
						else
						{
							playAudio = true;
							FileInfo audioFile = ResourceLocator.FindVocalization(conversation.Tag, node.Id);
							if (audioFile != null)
								AudioServer.Play(audioFile);
						}
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			} while (command != Command.QuitProgram);
		}
	}
}
