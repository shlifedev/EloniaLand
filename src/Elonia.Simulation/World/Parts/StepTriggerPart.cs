using System;

namespace Elonia.Simulation
{
    public sealed class StepTriggerPart : IThingPart
    {
        public IEffect Effect { get; }

        public StepTriggerPart(IEffect effect)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            Effect = effect;
        }
    }
}
