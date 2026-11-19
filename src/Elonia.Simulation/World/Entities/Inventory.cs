using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Elonia.Simulation
{
    public sealed class Inventory
    {
        private readonly List<ItemStack> _stacks = new List<ItemStack>();

        public IReadOnlyList<ItemStack> Stacks { get; }

        public Inventory()
        {
            Stacks = new ReadOnlyCollection<ItemStack>(_stacks);
        }

        public int GetCount(ThingType type)
        {
            int index = FindStackIndex(type);
            return index >= 0 ? _stacks[index].Count : 0;
        }

        internal void Add(ThingType type, int amount)
        {
            Debug.Assert(type != null);
            Debug.Assert(amount > 0);

            int index = FindStackIndex(type);
            if (index >= 0)
            {
                _stacks[index] = new ItemStack(type, _stacks[index].Count + amount);
                return;
            }

            _stacks.Add(new ItemStack(type, amount));
        }

        private int FindStackIndex(ThingType type)
        {
            for (int index = 0; index < _stacks.Count; index++)
            {
                if (_stacks[index].Type == type)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
