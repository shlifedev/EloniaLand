using System;

namespace Elonia.Simulation
{
    public sealed class AttackBehavior : IBehavior
    {
        private readonly AttackAction _attack;

        public Actor Target => _attack.Target;

        public string Name => "ATTACK";

        public AttackBehavior(Actor target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            _attack = new AttackAction(target);
        }

        public bool CanRun(ActionContext context)
        {
            return _attack.Target.IsAlive && AttackAction.IsInReach(context.Actor, _attack.Target);
        }

        public ActorAction ChooseAction(ActionContext context)
        {
            return _attack;
        }
    }
}
