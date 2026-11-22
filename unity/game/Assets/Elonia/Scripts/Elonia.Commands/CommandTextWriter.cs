using System;

namespace Elonia.Commands
{
    public sealed class CommandTextWriter
    {
        private const int MAX_INT_LENGTH = 11;

        private char[] _buffer;
        private int _length;

        public int Length => _length;

        public CommandTextWriter(int capacity = 256)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _buffer = new char[capacity];
        }

        public void Write(char value)
        {
            EnsureCapacity(1);
            _buffer[_length] = value;
            _length++;
        }

        public void Write(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            EnsureCapacity(value.Length);
            value.CopyTo(0, _buffer, _length, value.Length);
            _length += value.Length;
        }

        public void Write(int value)
        {
            EnsureCapacity(MAX_INT_LENGTH);
            long remaining = value;
            if (remaining < 0)
            {
                _buffer[_length] = '-';
                _length++;
                remaining = -remaining;
            }

            int digitCount = 1;
            for (long rest = remaining / 10; rest > 0; rest /= 10)
            {
                digitCount++;
            }

            for (int index = _length + digitCount - 1; index >= _length; index--)
            {
                _buffer[index] = (char)('0' + remaining % 10);
                remaining /= 10;
            }

            _length += digitCount;
        }

        public void Clear()
        {
            _length = 0;
        }

        public override string ToString()
        {
            return new string(_buffer, 0, _length);
        }

        private void EnsureCapacity(int additionalLength)
        {
            int required = _length + additionalLength;
            if (required <= _buffer.Length)
            {
                return;
            }

            int newCapacity = _buffer.Length * 2;
            while (newCapacity < required)
            {
                newCapacity *= 2;
            }

            Array.Resize(ref _buffer, newCapacity);
        }
    }
}
