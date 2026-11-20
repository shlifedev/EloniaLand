using System;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckCells()
    {
        TestPropDef tree = new TestPropDef("Tree", false);
        TestFloorDef dirt = new TestFloorDef("Dirt", true);
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        map.SetFloor(2, 2, dirt);
        map.SetProp(2, 2, tree);

        Cell treeCell = map.GetCell(2, 2);
        CheckEqual(2, treeCell.X, "A cell knows its x.");
        CheckEqual(2, treeCell.Y, "A cell knows its y.");
        CheckEqual(dirt, treeCell.Floor, "A cell holds the floor set on the map.");
        CheckEqual<IPropDef?>(tree, treeCell.PropOrNull, "A cell holds the prop set on the map.");
        Check(!treeCell.IsWalkable, "A cell with a blocking prop is not walkable.");
        CheckThrows<ArgumentOutOfRangeException>(() => map.GetCell(MAP_WIDTH, 0), "A cell outside the map cannot be read.");

        Actor player = map.AddEntity(TestActors.Create(1, 5, 5, 10));
        FixedController controller = new FixedController(WaitAction.DEFAULT);
        Actor npc = map.AddEntity(TestActors.Create(2, 6, 5, 10, controller));
        Thing gold = map.AddEntity(new Thing(101, TestItems.GOLD, 5, 5));
        TestThingDef mark = new TestThingDef("Mark");
        Thing markThing = map.AddEntity(new Thing(102, mark, 5, 5));
        GameRunner runner = new GameRunner(new World(map), player);

        Cell startCell = map.GetCell(5, 5);
        CheckEqual<Actor?>(player, startCell.LivingActorOrNull, "An added actor is recorded in its cell.");
        CheckEqual(2, startCell.Things.Count, "Every added thing is recorded in its cell.");
        CheckEqual<Thing?>(gold, startCell.GetItemOrNull(), "A cell finds its item among its things.");
        CheckEqual(0, map.GetCell(0, 0).Things.Count, "An empty cell has no things.");

        runner.SubmitPlayerAction(TestMoveAction.DOWN);
        CheckEqual<Actor?>(null, startCell.LivingActorOrNull, "The cell an actor left is empty.");
        CheckEqual<Actor?>(player, map.GetCell(5, 6).LivingActorOrNull, "The cell an actor entered holds it.");
        CheckEqual(2, startCell.Things.Count, "Things stay in their cell when an actor leaves.");

        runner.SubmitPlayerAction(TestMoveAction.UP);
        map.RemoveThing(gold);
        CheckEqual<Thing?>(null, startCell.GetItemOrNull(), "A picked-up item leaves its cell.");
        CheckEqual(1, startCell.Things.Count, "Other things stay in the cell after a pick-up.");
        CheckEqual<Thing?>(markThing, startCell.Things[0], "The remaining thing is the one not picked up.");

        Cell npcCell = map.GetCell(6, 5);
        npc.TakeDamage(npc.Hp);
        CheckEqual<Actor?>(null, npcCell.LivingActorOrNull, "A dead actor no longer occupies its cell.");
        Check(npcCell.CanEnter, "A dead actor's cell can be entered.");

        runner.SubmitPlayerAction(TestMoveAction.RIGHT);
        CheckEqual<Actor?>(player, npcCell.LivingActorOrNull, "A living actor can stand where a dead one lies.");
        runner.SubmitPlayerAction(TestMoveAction.RIGHT);
        CheckEqual<Actor?>(null, npcCell.LivingActorOrNull, "Leaving a dead actor's cell leaves it empty.");
        CheckEqual<Actor?>(player, map.GetCell(7, 5).LivingActorOrNull, "The player is recorded in its new cell.");
    }
}
