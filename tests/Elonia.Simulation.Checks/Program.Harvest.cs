using System;
using System.Collections.Generic;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckHarvest()
    {
        const int DROP_AMOUNT = 2;
        TileType wall = new TileType("Wall", false);
        TileHarvest treeHarvest = new TileHarvest(TileType.GROUND, EItemType.Branch, DROP_AMOUNT);
        TileType tree = new TileType("Tree", false, treeHarvest);
        CheckEqual<TileHarvest?>(treeHarvest, tree.HarvestOrNull, "A tile type keeps its harvest result.");
        CheckEqual<TileHarvest?>(null, wall.HarvestOrNull, "A tile type is not harvestable by default.");

        CheckThrows<ArgumentNullException>(() => new TileHarvest(null!, EItemType.Branch, 1), "A harvest needs a result tile.");
        CheckThrows<ArgumentException>(() => new TileHarvest(wall, EItemType.Branch, 1), "A harvest result tile must be walkable.");
        CheckThrows<ArgumentOutOfRangeException>(() => new TileHarvest(TileType.GROUND, EItemType.Branch, 0),
            "A harvest must drop a positive amount.");
        CheckThrows<ArgumentException>(() => new TileType("Bush", true, treeHarvest), "A harvestable tile must not be walkable.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        map.SetTile(0, 4, wall);
        map.SetTile(1, 5, tree);
        map.SetTile(1, 6, tree);
        map.SetTile(2, 5, tree);
        Actor player = map.AddActor(TestActors.Create(1, 0, 5, 10));
        HarvestRecorder recorder = new HarvestRecorder();
        world.Events.Register<TileHarvested>(recorder);
        GameRunner runner = new GameRunner(world, player);
        long startTick = world.Tick;

        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new HarvestAction(-1, 5)).Status,
            "Harvesting outside the map is rejected.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new HarvestAction(0, 4)).Status,
            "A tile without a harvest result cannot be harvested.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new HarvestAction(0, 6)).Status,
            "Open ground cannot be harvested.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new HarvestAction(1, 6)).Status,
            "A diagonal tile is out of reach.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new HarvestAction(2, 5)).Status,
            "A tile two steps away is out of reach.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new HarvestAction(0, 5)).Status,
            "An actor cannot harvest its own tile.");
        CheckEqual(tree, map.GetTile(2, 5), "An out-of-reach harvest leaves the tree as it is.");
        CheckEqual(startTick, world.Tick, "Rejected harvests spend no time.");
        CheckEqual(wall, map.GetTile(0, 4), "A rejected harvest leaves the wall as it is.");
        CheckEqual(TileType.GROUND, map.GetTile(0, 6), "A rejected harvest leaves the ground as it is.");
        CheckEqual(0, map.Items.Count, "Rejected harvests drop nothing.");
        CheckEqual(0, recorder.Events.Count, "Rejected harvests raise no events.");

        ActionResult result = runner.SubmitPlayerAction(new HarvestAction(1, 5));
        CheckEqual(EActionStatus.Executed, result.Status, "An adjacent harvestable tile can be harvested.");
        CheckEqual(TileType.GROUND, map.GetTile(1, 5), "A harvested tile turns into its result tile.");
        Item? droppedOrNull = map.GetItemAtOrNull(1, 5);
        Check(droppedOrNull != null, "A harvest drops an item on the harvested tile.");
        CheckEqual(EItemType.Branch, droppedOrNull!.Type, "The drop has the harvest's item type.");
        CheckEqual(DROP_AMOUNT, droppedOrNull.Amount, "The drop has the harvest's amount.");
        CheckEqual(startTick + HarvestAction.COST / 10, world.Tick, "A harvest spends its own cost.");
        CheckEqual("0,5", $"{player.X},{player.Y}", "Harvesting does not move the actor.");

        CheckEqual(1, recorder.Events.Count, "A harvest raises one event.");
        TileHarvested harvested = recorder.Events[0];
        CheckEqual(player, harvested.Actor, "The event names the harvesting actor.");
        CheckEqual("1,5", $"{harvested.X},{harvested.Y}", "The event carries the harvested tile's position.");
        CheckEqual(tree, harvested.HarvestedTile, "The event carries the tile from before the harvest.");
        CheckEqual(droppedOrNull, harvested.DroppedItem, "The event carries the dropped item.");

        long harvestedTick = world.Tick;
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new HarvestAction(1, 5)).Status,
            "An already harvested tile cannot be harvested again.");
        CheckEqual(harvestedTick, world.Tick, "A repeated harvest spends no time.");
        CheckEqual(1, map.Items.Count, "A repeated harvest drops nothing more.");
        CheckEqual(1, recorder.Events.Count, "A repeated harvest raises no event.");

        CheckEqual(EActionStatus.Executed, runner.SubmitPlayerAction(MoveAction.RIGHT).Status,
            "A harvested tile can be walked onto.");
        CheckEqual(1, player.X, "The actor stands on the harvested tile.");
    }
}

internal sealed class HarvestRecorder : IEventListener<TileHarvested>
{
    public List<TileHarvested> Events { get; } = new List<TileHarvested>();

    public void OnEvent(TileHarvested message)
    {
        Events.Add(message);
    }
}
