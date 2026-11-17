using System;

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
            bool bInReach = Math.Abs(actor.X - TargetX) + Math.Abs(actor.Y - TargetY) == REACH;
            if (!bInReach || !map.IsInside(TargetX, TargetY))
            {
                return ActionResult.REJECTED;
            }

            TileType harvestedTile = map.GetTile(TargetX, TargetY);
            TileHarvest? harvestOrNull = harvestedTile.HarvestOrNull;
            if (harvestOrNull == null)
            {
                return ActionResult.REJECTED;
            }

            map.SetTile(TargetX, TargetY, harvestOrNull.ResultTile);
            Item dropped = map.AddItem(new Item(harvestOrNull.DropType, TargetX, TargetY, harvestOrNull.DropAmount));
            context.World.Events.Broadcast(new TileHarvested(actor, TargetX, TargetY, harvestedTile, dropped));
            return ActionResult.Executed(COST);
        }
    }
}
