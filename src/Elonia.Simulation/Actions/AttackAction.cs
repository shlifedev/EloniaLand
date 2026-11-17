using System;

namespace Elonia.Simulation
{
    public sealed class AttackAction : ActorAction
    {
        public const int REACH = 1;

        public override string Name => "ATTACK";
        public Actor Target { get; }

        public AttackAction(Actor target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Target = target;
        }

        public static bool IsInReach(Actor attacker, Actor target)
        {
            return Math.Abs(attacker.X - target.X) + Math.Abs(attacker.Y - target.Y) == REACH;
        }

        public override ActionResult Execute(ActionContext context)
        {
            Actor attacker = context.Actor;
            if (Target == attacker || !Target.IsAlive || !IsInReach(attacker, Target))
            {
                return ActionResult.REJECTED;
            }

            int damage = attacker.Type.AttackPower;
            Target.TakeDamage(damage);
            context.World.Events.Broadcast(new ActorAttacked(attacker, Target, damage));
            if (!Target.IsAlive)
            {
                context.World.Events.Broadcast(new ActorDied(Target));
            }

            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
