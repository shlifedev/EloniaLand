using System;
using Elonia.Simulation;

namespace Elonia.Game
{
    public static class DamageEffect
    {
        public static void Apply(ActionContext context, Actor target, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "A damage amount must be positive.");
            }

            if (!target.IsAlive)
            {
                return;
            }

            target.TakeDamage(amount);
            context.World.Events.Broadcast(new ActorDamaged(target, amount));
            if (!target.IsAlive)
            {
                context.World.Events.Broadcast(new ActorDied(target));
            }
        }
    }
}
