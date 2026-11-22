using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class HarvestAction : ActorAction, IWorkAction
    {
        public const int TURNS = 3;

        public const int REACH = 1;

        public override string Id => "harvest";

        public int TargetX { get; }
        public int TargetY { get; }

        public int Progress { get; private set; }

        public bool IsFinished => Progress >= TURNS;

        public HarvestAction(int targetX, int targetY)
        {
            TargetX = targetX;
            TargetY = targetY;
        }

        public override ActionResult Execute(ActionContext context)
        {
            if (IsFinished)
            {
                return ActionResult.REJECTED;
            }

            Actor actor = context.Actor;
            Map map = context.Map;
            bool bInReach = GridMath.IsInReach(actor.X, actor.Y, TargetX, TargetY, REACH);
            if (!bInReach || !map.IsInside(TargetX, TargetY))
            {
                return ActionResult.REJECTED;
            }

            PropRow? harvestedPropOrNull = map.GetPropOrNull(TargetX, TargetY)?.Row();
            if (harvestedPropOrNull == null || !harvestedPropOrNull.DropThing.HasValue)
            {
                return ActionResult.REJECTED;
            }

            if (map.GetItemAtOrNull(TargetX, TargetY) != null || !map.GetFloor(TargetX, TargetY).IsWalkable)
            {
                return ActionResult.REJECTED;
            }

            Progress++;
            if (!IsFinished)
            {
                return ActionResult.Executed(DEFAULT_COST);
            }

            ThingRow drop = context.World.Tables().Thing.Get(harvestedPropOrNull.DropThing);
            map.RemoveProp(TargetX, TargetY);
            Thing dropped = map.AddEntity(new Thing(map.CreateEntityID(), drop, TargetX, TargetY, harvestedPropOrNull.DropAmount));
            context.World.Events.Broadcast(new PropHarvested(actor, TargetX, TargetY, harvestedPropOrNull, dropped));
            return ActionResult.Executed(DEFAULT_COST);
        }
    }
}
