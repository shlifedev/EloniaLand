using System;

namespace Elonia.Simulation
{
    public sealed class ItemPart : IThingPart
    {
        public EItemEffect Effect { get; }

        public int Amount { get; }

        public ItemPart(EItemEffect effect, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "An item amount must be positive.");
            }

            Effect = effect;
            Amount = amount;
        }
    }
}
