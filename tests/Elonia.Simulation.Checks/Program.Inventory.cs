using System;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckInventory()
    {
        const int PLAYER_SPEED = 10;

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        Actor player = map.AddEntity(TestActors.Create(1, 5, 5, PLAYER_SPEED));
        Thing branch = map.AddEntity(new Thing(111, TestItems.BRANCH, 5, 5, 3));
        map.AddEntity(new Thing(112, TestItems.BRANCH, 6, 5, 2));
        map.AddEntity(new Thing(113, TestItems.POTION, 7, 5, 1));
        map.AddEntity(new Thing(114, TestItems.GOLD, 8, 5, 7));
        EventRecorder recorder = new EventRecorder();
        world.Events.Register<ItemPickedUp>(recorder);
        GameRunner runner = new GameRunner(world, player);

        CheckEqual(0, player.Inventory.Stacks.Count, "A new actor's inventory is empty.");
        CheckEqual(0, player.Inventory.GetCount(TestItems.BRANCH), "An item type the actor does not have counts as 0.");

        long tickBeforePickUp = world.Tick;
        ActionResult result = runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        CheckEqual(true, result.IsExecuted, "A material on the actor's tile can be picked up.");
        CheckEqual(branch.Count, player.Inventory.GetCount(TestItems.BRANCH), "A picked-up material is added by its count.");
        CheckEqual(1, player.Inventory.Stacks.Count, "The first material of a type makes a new stack.");
        CheckEqual<Thing?>(null, map.GetItemAtOrNull(5, 5), "A picked-up material leaves the map.");
        CheckEqual(3, map.Things.Count, "The other items stay on the map.");
        CheckEqual("pickup 1 Branch 3", string.Join(",", recorder.Log), "Picking up a material raises an event.");
        CheckEqual(tickBeforePickUp + ActorAction.DEFAULT_COST / PLAYER_SPEED, world.Tick, "Picking up spends the action's cost.");
        CheckEqual(0, player.Gold, "A material gives no gold.");

        runner.SubmitPlayerAction(MoveAction.RIGHT);
        runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        CheckEqual(1, player.Inventory.Stacks.Count, "The same item type shares one stack.");
        CheckEqual(TestItems.BRANCH, player.Inventory.Stacks[0].Type, "The stack keeps its item type.");
        CheckEqual(3 + 2, player.Inventory.Stacks[0].Count, "The stack holds the sum of both pickups.");
        CheckEqual(3 + 2, player.Inventory.GetCount(TestItems.BRANCH), "GetCount matches the stack.");

        runner.SubmitPlayerAction(MoveAction.RIGHT);
        runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        CheckEqual(0, player.Inventory.GetCount(TestItems.POTION), "A potion does not enter the inventory.");
        CheckEqual<Thing?>(null, map.GetItemAtOrNull(7, 5), "A potion is still used up on pickup.");

        runner.SubmitPlayerAction(MoveAction.RIGHT);
        runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        CheckEqual(0, player.Inventory.GetCount(TestItems.GOLD), "Gold does not enter the inventory.");
        CheckEqual(7, player.Gold, "Gold is still added to the actor's gold on pickup.");
        CheckEqual(1, player.Inventory.Stacks.Count, "Only the material is in the inventory.");
        CheckEqual(0, map.Things.Count, "Every picked-up item left the map.");
        CheckEqual("pickup 1 Branch 3,pickup 1 Branch 2,pickup 1 Potion 5,pickup 1 Gold 7", string.Join(",", recorder.Log),
            "Each pickup raises an event.");
    }
}
