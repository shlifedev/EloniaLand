using System;

namespace Elonia.Simulation
{
    public readonly struct ActionResult
    {
        public static readonly ActionResult REJECTED = new ActionResult(0);

        public int Cost { get; }
        public bool IsExecuted => Cost > 0;

        private ActionResult(int cost)
        {
            Cost = cost;
        }

        public static ActionResult Executed(int cost)
        {
            if (cost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), "An executed action must consume a positive cost.");
            }

            return new ActionResult(cost);
        }
    }
}
