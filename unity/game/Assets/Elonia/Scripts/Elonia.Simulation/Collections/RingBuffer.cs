using System;
using System.Collections;
using System.Collections.Generic;

namespace Elonia.Simulation
{
    public sealed class RingBuffer<T> : IReadOnlyList<T>
    {
        private readonly T[] _items;
        private int _oldestIndex;
        private int _version;

        public int Capacity => _items.Length;
        public int Count { get; private set; }

        public T this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                int slotsUntilWrap = Capacity - _oldestIndex;
                return index < slotsUntilWrap ? _items[_oldestIndex + index] : _items[index - slotsUntilWrap];
            }
        }

        public RingBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _items = new T[capacity];
        }

        public void Add(T item)
        {
            if (Count < Capacity)
            {
                _items[Count] = item;
                Count++;
            }
            else
            {
                _items[_oldestIndex] = item;
                _oldestIndex++;
                if (_oldestIndex == Capacity)
                {
                    _oldestIndex = 0;
                }
            }

            _version++;
        }

        public void Clear()
        {
            Array.Clear(_items, 0, Count);
            Count = 0;
            _oldestIndex = 0;
            _version++;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private sealed class Enumerator : IEnumerator<T>
        {
            private readonly RingBuffer<T> _buffer;
            private readonly int _version;
            private int _nextIndex;

            public T Current { get; private set; } = default!;

            object? IEnumerator.Current => _nextIndex == 0 || _nextIndex > _buffer.Count
                ? throw new InvalidOperationException()
                : Current;

            public Enumerator(RingBuffer<T> buffer)
            {
                _buffer = buffer;
                _version = buffer._version;
            }

            public bool MoveNext()
            {
                CheckVersion();
                if (_nextIndex < _buffer.Count)
                {
                    Current = _buffer[_nextIndex++];
                    return true;
                }

                _nextIndex = _buffer.Count + 1;
                Current = default!;
                return false;
            }

            public void Reset()
            {
                CheckVersion();
                _nextIndex = 0;
                Current = default!;
            }

            public void Dispose()
            {
            }

            private void CheckVersion()
            {
                if (_version != _buffer._version)
                {
                    throw new InvalidOperationException("순회 중에 원형 버퍼가 바뀌었습니다.");
                }
            }
        }
    }
}
