using System;

namespace Elonia.Simulation
{
    public sealed class Item
    {
        public EItemType Type { get; }
        public int X { get; }
        public int Y { get; }
        public int Amount { get; }

        public Item(EItemType type, int x, int y, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "An item amount must be positive.");
            }

            Type = type;
            X = x;
            Y = y;
            Amount = amount;
        }

        internal void AddInteractions(InteractionPlan plan)
        {
            plan.Add(PickUpAction.DEFAULT, X, Y, PickUpAction.REACH);
        }
    }
}
