using System;
using System.Globalization;

namespace Elonia.DataManager
{
    public sealed class EnumExpression
    {
        public delegate bool TryResolveMember(string name, out long value);

        private readonly string _text;
        private readonly TryResolveMember _tryResolve;
        private int _position;
        private string _reason = "";

        private EnumExpression(string text, TryResolveMember tryResolve)
        {
            _text = text;
            _tryResolve = tryResolve;
        }

        public static bool TryEvaluate(string text, TryResolveMember tryResolve, out long value, out string reason)
        {
            EnumExpression expression = new EnumExpression(text, tryResolve);
            bool bParsed = expression.TryParseOr(out value);
            expression.SkipSpaces();
            if (bParsed && expression._position < text.Length)
            {
                bParsed = false;
                expression._reason = "'" + text.Substring(expression._position) + "' 부분을 이해하지 못했습니다.";
            }

            reason = expression._reason;
            return bParsed;
        }

        private bool TryParseOr(out long value)
        {
            if (!TryParseXor(out value))
            {
                return false;
            }

            while (TryConsume('|'))
            {
                if (!TryParseXor(out long right))
                {
                    return false;
                }

                value |= right;
            }

            return true;
        }

        private bool TryParseXor(out long value)
        {
            if (!TryParseAnd(out value))
            {
                return false;
            }

            while (TryConsume('^'))
            {
                if (!TryParseAnd(out long right))
                {
                    return false;
                }

                value ^= right;
            }

            return true;
        }

        private bool TryParseAnd(out long value)
        {
            if (!TryParseShift(out value))
            {
                return false;
            }

            while (TryConsume('&'))
            {
                if (!TryParseShift(out long right))
                {
                    return false;
                }

                value &= right;
            }

            return true;
        }

        private bool TryParseShift(out long value)
        {
            if (!TryParseAdd(out value))
            {
                return false;
            }

            while (true)
            {
                bool bLeft = TryConsume("<<");
                bool bRight = !bLeft && TryConsume(">>");
                if (!bLeft && !bRight)
                {
                    return true;
                }

                if (!TryParseAdd(out long count))
                {
                    return false;
                }

                if (count < 0 || count > 63)
                {
                    _reason = "자리 옮기기(<<, >>)는 0에서 63까지만 됩니다.";
                    return false;
                }

                value = bLeft ? value << (int)count : value >> (int)count;
            }
        }

        private bool TryParseAdd(out long value)
        {
            if (!TryParseMultiply(out value))
            {
                return false;
            }

            while (true)
            {
                bool bPlus = TryConsume('+');
                bool bMinus = !bPlus && TryConsume('-');
                if (!bPlus && !bMinus)
                {
                    return true;
                }

                if (!TryParseMultiply(out long right))
                {
                    return false;
                }

                value = bPlus ? value + right : value - right;
            }
        }

        private bool TryParseMultiply(out long value)
        {
            if (!TryParseUnary(out value))
            {
                return false;
            }

            while (TryConsume('*'))
            {
                if (!TryParseUnary(out long right))
                {
                    return false;
                }

                value *= right;
            }

            return true;
        }

        private bool TryParseUnary(out long value)
        {
            if (TryConsume('~'))
            {
                bool bParsed = TryParseUnary(out long operand);
                value = ~operand;
                return bParsed;
            }

            if (TryConsume('-'))
            {
                bool bParsed = TryParseUnary(out long operand);
                value = -operand;
                return bParsed;
            }

            if (TryConsume('+'))
            {
                return TryParseUnary(out value);
            }

            return TryParsePrimary(out value);
        }

        private bool TryParsePrimary(out long value)
        {
            value = 0;
            SkipSpaces();
            if (_position >= _text.Length)
            {
                _reason = "식이 중간에 끝났습니다.";
                return false;
            }

            if (TryConsume('('))
            {
                if (!TryParseOr(out value))
                {
                    return false;
                }

                if (!TryConsume(')'))
                {
                    _reason = "닫는 괄호가 없습니다.";
                    return false;
                }

                return true;
            }

            int start = _position;
            while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || _text[_position] == '_'))
            {
                _position++;
            }

            string token = _text.Substring(start, _position - start);
            if (token.Length == 0)
            {
                _reason = "'" + _text[start] + "'을(를) 이해하지 못했습니다.";
                return false;
            }

            if (char.IsDigit(token[0]))
            {
                return TryParseNumber(token, out value);
            }

            if (_tryResolve(token, out value))
            {
                return true;
            }

            _reason = "'" + token + "'은(는) 이 enum에서 앞에 정의한 멤버가 아닙니다.";
            return false;
        }

        private bool TryParseNumber(string token, out long value)
        {
            string digits = token.Replace("_", "");
            bool bParsed;
            if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                bParsed = long.TryParse(digits.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
            }
            else if (digits.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            {
                bParsed = TryParseBinary(digits.Substring(2), out value);
            }
            else
            {
                bParsed = long.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value);
            }

            if (!bParsed)
            {
                _reason = "'" + token + "'은(는) 정수가 아닙니다.";
            }

            return bParsed;
        }

        private static bool TryParseBinary(string digits, out long value)
        {
            value = 0;
            if (digits.Length == 0 || digits.Length > 63)
            {
                return false;
            }

            for (int index = 0; index < digits.Length; index++)
            {
                if (digits[index] != '0' && digits[index] != '1')
                {
                    return false;
                }

                value = (value << 1) | (long)(digits[index] - '0');
            }

            return true;
        }

        private void SkipSpaces()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        private bool TryConsume(char character)
        {
            SkipSpaces();
            if (_position >= _text.Length || _text[_position] != character)
            {
                return false;
            }

            _position++;
            return true;
        }

        private bool TryConsume(string token)
        {
            SkipSpaces();
            if (string.CompareOrdinal(_text, _position, token, 0, token.Length) != 0)
            {
                return false;
            }

            _position += token.Length;
            return true;
        }
    }
}
