using System;
using System.Collections.Generic;

namespace Elonia.Game
{
    public sealed class StoryFlags
    {
        private readonly Dictionary<string, int> _values = new Dictionary<string, int>(StringComparer.Ordinal);

        internal Dictionary<string, int> CaptureValues()
        {
            return new Dictionary<string, int>(_values, StringComparer.Ordinal);
        }

        public int Get(string name)
        {
            return _values.TryGetValue(name, out int value) ? value : 0;
        }

        public void Set(string name, int value)
        {
            _values[name] = value;
        }
    }
}
