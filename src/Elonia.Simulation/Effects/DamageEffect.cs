using System;

namespace Elonia.Simulation
{
    public sealed class DamageEffect : IEffect
    {
        public int Amount { get; }

        public DamageEffect(int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "A damage amount must be positive.");
            }

            Amount = amount;
        }

        public void Apply(ActionContext context, Actor target)
        {
            if (!target.IsAlive)
            {
                return;
            }

            target.TakeDamage(Amount);
            context.World.Events.Broadcast(new ActorDamaged(target, Amount));
            if (!target.IsAlive)
            {
                context.World.Events.Broadcast(new ActorDied(target));
            }
        }
    }
}
