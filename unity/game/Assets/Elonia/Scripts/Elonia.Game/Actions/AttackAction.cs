using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class AttackAction : ActorTargetAction
    {
        public const int REACH = 1;

        public override string Id => "attack";

        public AttackAction(Actor target)
            : base(target, REACH)
        {
        }

        public static bool IsInReach(Actor attacker, Actor target)
        {
            return GridMath.IsInReach(attacker.X, attacker.Y, target.X, target.Y, REACH);
        }

        public override ActionResult Execute(ActionContext context)
        {
            Actor attacker = context.Actor;
            if (!CanReachTarget(attacker))
            {
                return ActionResult.REJECTED;
            }

            context.World.Events.Broadcast(new ActorAttacked(attacker, Target));
            int damage = attacker.Stat(EStat.Attack);
            if (damage > 0)
            {
                DamageEffect.Apply(context, Target, damage, attacker);
            }

            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
