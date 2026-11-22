using System;
using System.Collections.Generic;

namespace Elonia.Game
{
    public sealed class DialogueLine
    {
        public const int MAX_CHOICE_COUNT = 3;

        public string Id { get; }
        public string Text { get; }
        public IReadOnlyList<DialogueChoice> Choices { get; }

        public DialogueLine(string id, string text, params DialogueChoice[] choices)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("A dialogue line needs an id.", nameof(id));
            }

            if (choices == null || choices.Length == 0 || choices.Length > MAX_CHOICE_COUNT)
            {
                throw new ArgumentException($"A dialogue line needs 1 to {MAX_CHOICE_COUNT} choices.", nameof(choices));
            }

            Id = id;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Choices = choices;
        }
    }

    public readonly struct DialogueChoice
    {
        public string Label { get; }

        public DialogueLine? NextLineOrNull { get; }

        public string? FlagOrNull { get; }

        public DialogueChoice(string label, DialogueLine? nextLineOrNull, string? flagOrNull)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            NextLineOrNull = nextLineOrNull;
            FlagOrNull = flagOrNull;
        }
    }
}
