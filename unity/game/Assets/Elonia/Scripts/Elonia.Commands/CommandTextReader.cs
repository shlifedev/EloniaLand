using System;

namespace Elonia.Commands
{
    public sealed class CommandTextReader
    {
        private readonly string _text;

        public int Position { get; internal set; }

        public bool IsEnd => Position >= _text.Length;

        public CommandTextReader(string text)
        {
            _text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public bool TryRead(char expected)
        {
            if (IsEnd || _text[Position] != expected)
            {
                return false;
            }

            Position++;
            return true;
        }

        public bool TryReadOneOf(string symbols, out int index)
        {
            index = IsEnd ? -1 : symbols.IndexOf(_text[Position]);
            if (index < 0)
            {
                return false;
            }

            Position++;
            return true;
        }

        public bool TryReadInt(out int value)
        {
            value = 0;
            int position = Position;
            bool bNegative = position < _text.Length && _text[position] == '-';
            if (bNegative)
            {
                position++;
            }

            long number = 0;
            int start = position;
            while (position < _text.Length && _text[position] >= '0' && _text[position] <= '9')
            {
                number = number * 10 + (_text[position] - '0');
                if (number > (long)int.MaxValue + 1)
                {
                    return false;
                }

                position++;
            }

            if (position == start)
            {
                return false;
            }

            number = bNegative ? -number : number;
            if (number < int.MinValue || number > int.MaxValue)
            {
                return false;
            }

            value = (int)number;
            Position = position;
            return true;
        }

        public bool TryReadName(out string name)
        {
            int position = Position;
            while (position < _text.Length && IsNameChar(_text[position]))
            {
                position++;
            }

            if (position == Position)
            {
                name = "";
                return false;
            }

            name = _text.Substring(Position, position - Position);
            Position = position;
            return true;
        }

        public static bool IsNameChar(char value)
        {
            return (value >= 'a' && value <= 'z') || (value >= 'A' && value <= 'Z') || (value >= '0' && value <= '9') || value == '_';
        }

        internal void SkipWhitespace()
        {
            while (!IsEnd && char.IsWhiteSpace(_text[Position]))
            {
                Position++;
            }
        }
    }
}
