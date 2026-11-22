using System;
using System.Collections.Generic;
using Elonia.Data;

namespace Elonia.Game
{
    public static class Dialogues
    {
        public const string PROMISED_HELP_FLAG = "villager_promised_help";

        private const string VILLAGER_ID = "villager";

        private static readonly Dictionary<string, DialogueLine> LINES = new Dictionary<string, DialogueLine>(StringComparer.Ordinal);

        private static readonly DialogueLine VILLAGER_GRATEFUL = AddLine("villager.grateful", "정말요? 고마워요! 동쪽 들판을 조심하세요.",
            new DialogueChoice("그럼 이만.", null, null));

        private static readonly DialogueLine VILLAGER_GREETING = AddLine("villager.greeting", "요즘 동쪽 들판에 슬라임이 늘어서 걱정이에요.",
            new DialogueChoice("제가 도와드릴게요.", VILLAGER_GRATEFUL, PROMISED_HELP_FLAG),
            new DialogueChoice("바빠서요.", null, null));

        private static readonly DialogueLine VILLAGER_REMINDER = AddLine("villager.reminder", "도와주신다니 든든해요. 슬라임은 동쪽 들판에 있어요.",
            new DialogueChoice("다녀올게요.", null, null));

        internal static DialogueLine GetLine(string id)
        {
            return LINES[id];
        }

        private static DialogueLine AddLine(string id, string text, params DialogueChoice[] choices)
        {
            DialogueLine line = new DialogueLine(id, text, choices);
            LINES.Add(id, line);
            return line;
        }

        public static bool HasDialogue(ActorRow row)
        {
            return row.Id == VILLAGER_ID;
        }

        public static DialogueLine? GetStartLineOrNull(ActorRow row, StoryFlags flags)
        {
            if (row.Id != VILLAGER_ID)
            {
                return null;
            }

            return flags.Get(PROMISED_HELP_FLAG) > 0 ? VILLAGER_REMINDER : VILLAGER_GREETING;
        }
    }
}
