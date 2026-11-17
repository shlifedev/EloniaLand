using System;

namespace Elonia.Simulation
{
    public readonly struct ActionResult
    {
        public static readonly ActionResult REJECTED = new ActionResult(EActionStatus.Rejected, 0);

        public EActionStatus Status { get; }
        public int Cost { get; }
        public bool IsExecuted => Status == EActionStatus.Executed;

        private ActionResult(EActionStatus status, int cost)
        {
            Status = status;
            Cost = cost;
        }

        public static ActionResult Executed(int cost)
        {
            if (cost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost), "An executed action must consume a positive cost.");
            }

            return new ActionResult(EActionStatus.Executed, cost);
        }
    }
}
