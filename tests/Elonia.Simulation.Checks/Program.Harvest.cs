using System;
using System.Collections.Generic;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckHarvest()
    {
        const int DROP_COUNT = 2;
        PropType wall = new PropType("Wall", false);
        PropHarvest treeHarvest = new PropHarvest(TestItems.BRANCH, DROP_COUNT);
        PropType tree = new PropType("Tree", false, treeHarvest);
        FloorType dirt = new FloorType("Dirt", true);
        FloorType water = new FloorType("Water", false);
        CheckEqual<PropHarvest?>(treeHarvest, tree.HarvestOrNull, "A prop type keeps its harvest result.");
        CheckEqual<PropHarvest?>(null, wall.HarvestOrNull, "A prop type is not harvestable by default.");

        CheckThrows<ArgumentOutOfRangeException>(() => new PropHarvest(TestItems.BRANCH, 0), "A harvest must drop a positive count.");
        CheckThrows<ArgumentNullException>(() => new PropHarvest(null!, 1), "A harvest needs a drop type.");
        CheckThrows<ArgumentException>(() => new PropType("Bush", true, treeHarvest), "A harvestable prop must not be walkable.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        map.SetProp(0, 4, wall);
        map.SetFloor(1, 5, dirt);
        map.SetProp(1, 5, tree);
        map.SetProp(1, 6, tree);
        map.SetProp(2, 5, tree);
        Actor player = map.AddEntity(TestActors.Create(1, 0, 5, 10));
        HarvestRecorder recorder = new HarvestRecorder();
        world.Events.Register<PropHarvested>(recorder);
        GameRunner runner = new GameRunner(world, player);
        long startTick = world.Tick;

        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(-1, 5)).IsExecuted,
            "Harvesting outside the map is rejected.");
        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(0, 4)).IsExecuted,
            "A prop without a harvest result cannot be harvested.");
        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(0, 6)).IsExecuted,
            "Open ground cannot be harvested.");
        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(1, 6)).IsExecuted,
            "A diagonal tile is out of reach.");
        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(2, 5)).IsExecuted,
            "A tile two steps away is out of reach.");
        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(0, 5)).IsExecuted,
            "An actor cannot harvest its own tile.");
        CheckEqual<PropType?>(tree, map.GetPropOrNull(2, 5), "An out-of-reach harvest leaves the tree as it is.");
        CheckEqual(startTick, world.Tick, "Rejected harvests spend no time.");
        CheckEqual<PropType?>(wall, map.GetPropOrNull(0, 4), "A rejected harvest leaves the wall as it is.");
        CheckEqual<PropType?>(null, map.GetPropOrNull(0, 6), "A rejected harvest leaves the open ground as it is.");
        CheckEqual(0, map.Things.Count, "Rejected harvests drop nothing.");
        CheckEqual(0, recorder.Events.Count, "Rejected harvests raise no events.");

        ActionResult result = runner.SubmitPlayerAction(new HarvestAction(1, 5));
        CheckEqual(true, result.IsExecuted, "An adjacent harvestable prop can be harvested.");
        CheckEqual<PropType?>(null, map.GetPropOrNull(1, 5), "A harvested prop is removed.");
        CheckEqual(dirt, map.GetFloor(1, 5), "A harvest leaves the floor under the prop as it is.");
        Thing? droppedOrNull = map.GetItemAtOrNull(1, 5);
        Check(droppedOrNull != null, "A harvest drops an item on the harvested tile.");
        CheckEqual(TestItems.BRANCH, droppedOrNull!.Type, "The drop has the harvest's item type.");
        CheckEqual(DROP_COUNT, droppedOrNull.Count, "The drop has the harvest's count.");
        Check(droppedOrNull.ID != player.ID, "The drop gets an entity ID that is not used in the map.");
        CheckEqual(startTick + HarvestAction.COST / 10, world.Tick, "A harvest spends its own cost.");
        CheckEqual("0,5", $"{player.X},{player.Y}", "Harvesting does not move the actor.");

        CheckEqual(1, recorder.Events.Count, "A harvest raises one event.");
        PropHarvested harvested = recorder.Events[0];
        CheckEqual(player, harvested.Actor, "The event names the harvesting actor.");
        CheckEqual("1,5", $"{harvested.X},{harvested.Y}", "The event carries the harvested tile's position.");
        CheckEqual(tree, harvested.HarvestedProp, "The event carries the harvested prop.");
        CheckEqual(droppedOrNull, harvested.DroppedThing, "The event carries the dropped item.");

        long harvestedTick = world.Tick;
        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(1, 5)).IsExecuted,
            "An already harvested tile cannot be harvested again.");
        CheckEqual(harvestedTick, world.Tick, "A repeated harvest spends no time.");
        CheckEqual(1, map.Things.Count, "A repeated harvest drops nothing more.");
        CheckEqual(1, recorder.Events.Count, "A repeated harvest raises no event.");

        CheckEqual(true, runner.SubmitPlayerAction(MoveAction.RIGHT).IsExecuted,
            "A harvested tile can be walked onto.");
        CheckEqual(1, player.X, "The actor stands on the harvested tile.");

        Thing buried = map.AddEntity(new Thing(110, TestItems.GOLD, 2, 4, 1));
        map.SetProp(2, 4, tree);
        runner.SubmitPlayerAction(MoveAction.UP);
        CheckEqual("1,4", $"{player.X},{player.Y}", "The actor stands next to the covered tile.");
        long coveredTick = world.Tick;
        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(2, 4)).IsExecuted,
            "A harvestable prop over an item cannot be harvested.");
        CheckEqual<PropType?>(tree, map.GetPropOrNull(2, 4), "A rejected harvest over an item leaves the prop as it is.");
        CheckEqual<Thing?>(buried, map.GetItemAtOrNull(2, 4), "A rejected harvest over an item leaves the item as it is.");
        CheckEqual(coveredTick, world.Tick, "A rejected harvest over an item spends no time.");
        CheckEqual(1, recorder.Events.Count, "A rejected harvest over an item raises no event.");

        map.SetFloor(1, 3, water);
        map.SetProp(1, 3, tree);
        long waterTick = world.Tick;
        CheckEqual(false, runner.SubmitPlayerAction(new HarvestAction(1, 3)).IsExecuted,
            "A harvestable prop on a blocking floor cannot be harvested.");
        CheckEqual<PropType?>(tree, map.GetPropOrNull(1, 3), "A rejected harvest on a blocking floor leaves the prop as it is.");
        CheckEqual(waterTick, world.Tick, "A rejected harvest on a blocking floor spends no time.");
        CheckEqual(1, recorder.Events.Count, "A rejected harvest on a blocking floor raises no event.");
    }
}

internal sealed class HarvestRecorder : IEventListener<PropHarvested>
{
    public List<PropHarvested> Events { get; } = new List<PropHarvested>();

    public void OnEvent(PropHarvested message)
    {
        Events.Add(message);
    }
}
