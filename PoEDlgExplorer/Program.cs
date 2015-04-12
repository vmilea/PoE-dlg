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
			Confirm,
			ToggleAudio,
			QuitProgram,
		}

		private struct ConversationLink
		{
			public readonly Conversation Conversation;
			public readonly FlowChartLink Link;

			public ConversationLink(Conversation conversation, FlowChartLink link)
			{
				Conversation = conversation;
				Link = link;
			}

			public ConversationLink(Conversation conversation, int fromNodeID, int toNodeID)
				: this(conversation, new FlowChartLink(fromNodeID, toNodeID))
			{
			}
		}

		private static Command ReadInput(FlowChartNode node, out FlowChartLink pickedLink)
		{
			int accumulator = 0;
			int pickedLine = -1;
			int numDialogueLines = node.Links.Count + node.ChildIDs.Count;

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
						pickedLine = accumulator - 1;
						command = Command.PickLine;
					}
					else
					{
						if (numDialogueLines > 0)
							Console.WriteLine("Valid range: {0} .. {1}\n", 1, numDialogueLines);
						else
							command = Command.Confirm;

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
							pickedLine = accumulator - 1;
							command = Command.PickLine;
						}
					}
				}
				else if (keyInfo.Key == ConsoleKey.Spacebar)
				{
					command = Command.ToggleAudio;
				}
			} while (command == Command.None);

			if (pickedLine < node.Links.Count)
				pickedLink = (pickedLine == -1 ? null : node.Links[pickedLine]);
			else
				pickedLink = new FlowChartLink(node.NodeID, node.ChildIDs[pickedLine - node.Links.Count]);

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
				throw new FileNotFoundException("String table file not found for conversation " + tag + " (" + localization + ")");

			var conversationData = Xml.Deserialize<ConversationData>(conversationFile.FullName);
			var stringTable = Xml.Deserialize<StringTable>(stringTableFile.FullName);

			return new Conversation(tag, conversationData, stringTable);
		}

		private static void PrintNode(ConversationLink conversationLink, bool playAudio)
		{
			PrintNode(conversationLink.Conversation, conversationLink.Link, playAudio);
		}

		private static void PrintNode(Conversation conversation, FlowChartLink parentLink, bool playAudio)
		{
			FlowChartNode node = conversation.Nodes[parentLink.ToNodeID];
			var dialogueNode = node as DialogueNode;
			bool keepParentBrief = (parentLink.PointsToGhost && dialogueNode != null && dialogueNode.IsQuestionNode);
			FlowChartNode briefNode = (keepParentBrief ? conversation.Nodes[parentLink.FromNodeID] : node);

			Console.WriteLine("{0}", briefNode.GetBrief());

			FileInfo audioFile = ResourceLocator.FindVocalization(conversation.Tag, briefNode.NodeID);
			if (audioFile != null)
				Console.Write("[audio] ");

			StringTable.Entry text = conversation.FindText(briefNode.NodeID);
			if (text != null)
			{
				Console.ForegroundColor = ConsoleColor.White;
				Console.WriteLine("{0}", text.Format());
				Console.ForegroundColor = ConsoleColor.Gray;
			}
			Console.WriteLine("\n\n");

			if (node.Links.Count + node.ChildIDs.Count == 0)
			{
				if (node is TriggerConversationNode)
					Console.WriteLine("(End. Hit BACKSPACE to rewind or ENTER to load target conversation)");
				else if (node.ContainerNodeID != -1)
					Console.WriteLine("(Hit BACKSPACE to rewind into container node)");
				else
					Console.WriteLine("(End. Hit BACKSPACE to rewind)");
			}
			else
			{
				for (int i = 0; i < node.Links.Count + node.ChildIDs.Count; i++)
				{
					FlowChartNode targetNode;
					bool pointsToGhost = false;

					if (i < node.Links.Count)
					{
						FlowChartLink link = node.Links[i];
						pointsToGhost = link.PointsToGhost;
						targetNode = conversation.FindNode(link.ToNodeID);
						Console.Write("({0}) {1} -> {2}", i + 1, link.GetBrief(), targetNode.GetBrief());
					}
					else
					{
						targetNode = conversation.FindNode(node.ChildIDs[i - node.Links.Count]);
						Console.Write("({0}) [ child ] -> {1}", i + 1, targetNode.GetBrief());
					}

					if (targetNode is PlayerResponseNode && targetNode.Links.Count == 1)
						Console.WriteLine(" -> {0}", conversation.FindNode(targetNode.Links[0].ToNodeID).GetBrief());
					else
						Console.WriteLine();

					string condition = targetNode.Conditionals.Format();
					if (condition.Length > 0)
						Console.WriteLine("  if : {0}", condition);
					foreach (var script in targetNode.OnEnterScripts)
						Console.WriteLine("  on enter  : {0}", script.Format());
					foreach (var script in targetNode.OnExitScripts)
						Console.WriteLine("  on exit   : {0}", script.Format());
					foreach (var script in targetNode.OnUpdateScripts)
						Console.WriteLine("  on update : {0}", script.Format());

					if (!pointsToGhost)
						Console.ForegroundColor = ConsoleColor.White;
					if (node.Links.Count + node.ChildIDs.Count == 1)
					{
						Console.WriteLine("[continue]");
					}
					else
					{
						if (ResourceLocator.FindVocalization(conversation.Tag, targetNode.NodeID) != null)
							Console.Write("[audio] ");
						text = conversation.FindText(targetNode.NodeID);
						Console.WriteLine(text.Format());
					}
					if (!pointsToGhost)
						Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine("\n");
				}
			}

			if ((audioFile != null) && playAudio && (briefNode == node))
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
				conversation = LoadConversationByPath(new FileInfo(args[0]), Settings.Instance.Localization);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error: " + e);
				Console.ReadKey(true);
				return;
			}

			bool audioEnabled = Settings.Instance.PlayAudio;

			var linkStack = new Stack<ConversationLink>();
			linkStack.Push(new ConversationLink(conversation, -1, 0));
			PrintNode(linkStack.Peek(), audioEnabled);

			Command command;
			do
			{
				FlowChartNode node = conversation.FindNode(linkStack.Peek().Link.ToNodeID);
				FlowChartLink pickedLink;
				command = ReadInput(node, out pickedLink);

				switch (command)
				{
					case Command.PickLine:
						AudioServer.Stop();
						Console.Clear();

						linkStack.Push(new ConversationLink(conversation, pickedLink));

						FlowChartNode targetNode = conversation.FindNode(pickedLink.ToNodeID);
						if (targetNode is PlayerResponseNode && targetNode.Links.Count == 1)
						{
							// skip player response nodes
							linkStack.Pop();
							linkStack.Push(new ConversationLink(conversation, targetNode.Links[0]));
						}

						PrintNode(linkStack.Peek(), audioEnabled);
						break;

					case Command.Rewind:
						if (linkStack.Count > 1)
						{
							AudioServer.Stop();
							Console.Clear();

							linkStack.Pop();
							conversation = linkStack.Peek().Conversation;
							PrintNode(linkStack.Peek(), false);
						}
						break;

					case Command.Confirm:
						var triggerNode = node as TriggerConversationNode;
						if (triggerNode != null)
						{
							AudioServer.Stop();
							Console.Clear();

							string tag = Path.GetFileNameWithoutExtension(triggerNode.ConversationFilename);
							if (tag != conversation.Tag)
								conversation = LoadConversation(tag, Settings.Instance.Localization);

							linkStack.Push(new ConversationLink(conversation, -1, triggerNode.StartNodeID));
							PrintNode(linkStack.Peek(), audioEnabled);
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
