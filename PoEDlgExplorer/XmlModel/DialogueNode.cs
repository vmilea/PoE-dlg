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
using System.IO;
using System.Text;

namespace PoEDlgExplorer.XmlModel
{
	public enum QuestionNodeDisplayType
	{
		ShowOnce,
		ShowAlways,
		ShowNever
	}

	public enum PlayType
	{
		Normal,
		Random,
		CycleLoop,
		CycleStop
	}

	public enum PersistenceType
	{
		None,
		MarkAsRead,
		OncePerConversation,
		OnceEver
	}

	public enum DisplayType
	{
		Hidden,
		Conversation,
		Bark
	}

	public abstract class DialogueNode : FlowChartNode
	{
		public bool NotSkippable;
		public bool IsQuestionNode;
		public bool IsTempText;
		public bool PlayVOAs3DSound;
		public PlayType PlayType;
		public PersistenceType Persistence;
		public int NoPlayRandomWeight;
		public DisplayType DisplayType;
		public string VOFilename;
		public string VoiceType;
		public List<Guid> ExcludedSpeakerClasses;
		public List<Guid> ExcludedListenerClasses;
		public List<Guid> IncludedSpeakerClasses;
		public List<Guid> IncludedListenerClasses;

		protected override void ExtendBrief(StringBuilder sb)
		{
			if (IsQuestionNode)
				sb.Append("QuestionNode ");

			if (!(this is TalkNode))
				sb.Append(GetType().Name).Append(" ");

			if (DisplayType != DisplayType.Conversation)
				sb.Append(DisplayType).Append(" ");

			if (PlayType != PlayType.Normal)
				sb.Append(PlayType).Append(" ");

			if (Persistence != PersistenceType.None)
				sb.Append(Persistence).Append(" ");

			if (NotSkippable)
				sb.Append("NotSkippable ");
		}
	}

	[Serializable]
	public class PlayerResponseNode : DialogueNode
	{

	}

	[Serializable]
	public class ScriptNode : DialogueNode
	{

	}

	[Serializable]
	public class TalkNode : DialogueNode
	{
		public string ActorDirection;
		public Guid SpeakerGuid;
		public Guid ListenerGuid;
	}

	[Serializable]
	public class TriggerConversationNode : DialogueNode
	{
		public string ConversationFilename;
		public int StartNodeID;

		protected override void ExtendBrief(StringBuilder sb)
		{
			base.ExtendBrief(sb);

			string tag = Path.GetFileNameWithoutExtension(ConversationFilename);
			sb.Append("node-").Append(StartNodeID)
				.Append(" ").Append(tag).Append(" ");
		}
	}

	[Serializable]
	public class DialogueLink : FlowChartLink
	{
		public QuestionNodeDisplayType QuestionNodeTextDisplay;
		public bool PlayQuestionNodeVO;
		public int RandomWeight;

		protected override void ExtendBrief(StringBuilder sb)
		{
			if (!PlayQuestionNodeVO)
				sb.Append("!PlayQuestionNodeVO ");

			if (RandomWeight != 1)
				sb.Append("RandomWeight-").Append(RandomWeight).Append(" ");

			if (QuestionNodeTextDisplay != QuestionNodeDisplayType.ShowOnce)
				sb.Append(QuestionNodeTextDisplay).Append(" ");
		}
	}
}
