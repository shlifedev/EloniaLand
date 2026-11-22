using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

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

        public int GetCount(IThingDef def)
        {
            int index = FindStackIndex(def);
            return index >= 0 ? _stacks[index].Count : 0;
        }

        public void Add(IThingDef def, int amount)
        {
            if (def == null)
            {
                throw new ArgumentNullException(nameof(def));
            }

            if (!def.IsItem)
            {
                throw new ArgumentException("Only an item can be added to an inventory.", nameof(def));
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            int index = FindStackIndex(def);
            if (index >= 0)
            {
                _stacks[index] = new ItemStack(def, checked(_stacks[index].Count + amount));
                return;
            }

            _stacks.Add(new ItemStack(def, amount));
        }

        private int FindStackIndex(IThingDef def)
        {
            for (int index = 0; index < _stacks.Count; index++)
            {
                if (ReferenceEquals(_stacks[index].Def, def))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
