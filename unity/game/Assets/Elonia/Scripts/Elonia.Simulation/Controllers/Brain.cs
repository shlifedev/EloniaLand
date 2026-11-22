using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Elonia.Simulation
{
    public sealed class Brain : IActorController
    {
        private readonly IBehavior[] _behaviors;

        public IReadOnlyList<IBehavior> Behaviors { get; }

        public IBehavior? CurrentBehaviorOrNull { get; private set; }

        public ActorAction? LastActionOrNull { get; private set; }

        public Brain(params IBehavior[] behaviors)
        {
            if (behaviors == null)
            {
                throw new ArgumentNullException(nameof(behaviors));
            }

            if (behaviors.Length == 0)
            {
                throw new ArgumentException("A brain needs at least one behavior.", nameof(behaviors));
            }

            for (int index = 0; index < behaviors.Length; index++)
            {
                if (behaviors[index] == null)
                {
                    throw new ArgumentException("A behavior must not be null.", nameof(behaviors));
                }
            }

            _behaviors = (IBehavior[])behaviors.Clone();
            Behaviors = new ReadOnlyCollection<IBehavior>(_behaviors);
        }

        public void RestoreCurrentBehavior(IBehavior? behaviorOrNull)
        {
            if (behaviorOrNull != null && Array.IndexOf(_behaviors, behaviorOrNull) < 0)
            {
                throw new ArgumentException("이 두뇌에 등록되지 않은 행동 방식입니다.", nameof(behaviorOrNull));
            }

            CurrentBehaviorOrNull = behaviorOrNull;
            LastActionOrNull = null;
        }

        public ActorAction ChooseAction(ActionContext context)
        {
            for (int index = 0; index < _behaviors.Length; index++)
            {
                IBehavior behavior = _behaviors[index];
                if (behavior.CanRun(context))
                {
                    CurrentBehaviorOrNull = behavior;
                    LastActionOrNull = behavior.ChooseAction(context);
                    return LastActionOrNull;
                }
            }

            CurrentBehaviorOrNull = null;
            LastActionOrNull = WaitAction.DEFAULT;
            return WaitAction.DEFAULT;
        }
    }
}
