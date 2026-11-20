namespace Elonia.Simulation
{
    public sealed class HarvestAction : ActorAction
    {
        public const int COST = 300;

        public const int REACH = 1;

        public override string Name => "HARVEST";

        public int TargetX { get; }
        public int TargetY { get; }

        public HarvestAction(int targetX, int targetY)
        {
            TargetX = targetX;
            TargetY = targetY;
        }

        public override ActionResult Execute(ActionContext context)
        {
            Actor actor = context.Actor;
            Map map = context.Map;
            bool bInReach = GridMath.GetManhattanDistance(actor.X, actor.Y, TargetX, TargetY) == REACH;
            if (!bInReach || !map.IsInside(TargetX, TargetY))
            {
                return ActionResult.REJECTED;
            }

            PropType? harvestedPropOrNull = map.GetPropOrNull(TargetX, TargetY);
            PropHarvest? harvestOrNull = harvestedPropOrNull?.HarvestOrNull;
            if (harvestedPropOrNull == null || harvestOrNull == null)
            {
                return ActionResult.REJECTED;
            }

            if (map.GetItemAtOrNull(TargetX, TargetY) != null || !map.GetFloor(TargetX, TargetY).IsWalkable)
            {
                return ActionResult.REJECTED;
            }

            map.RemoveProp(TargetX, TargetY);
            Thing dropped = map.AddEntity(new Thing(map.CreateEntityID(), harvestOrNull.DropType, TargetX, TargetY, harvestOrNull.DropCount));
            context.World.Events.Broadcast(new PropHarvested(actor, TargetX, TargetY, harvestedPropOrNull, dropped));
            return ActionResult.Executed(COST);
        }
    }
}
