using System;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckEntities()
    {
        ThingType rock = new ThingType("Rock");
        CheckEqual<ItemPart?>(null, rock.GetPartOrNull<ItemPart>(), "A thing type without an item part cannot be picked up.");

        ItemPart heal = new ItemPart(EItemEffect.Heal, 1);
        CheckEqual<ItemPart?>(heal, new ThingType("Herb", heal).GetPartOrNull<ItemPart>(), "A thing type gives back its part.");
        CheckThrows<ArgumentException>(() => new ThingType("Twice", heal, new ItemPart(EItemEffect.Gold, 1)),
            "A thing type cannot have two parts of the same kind.");
        CheckThrows<ArgumentException>(() => new ThingType("Broken", heal, null!), "A thing type rejects a null part.");
        CheckThrows<ArgumentNullException>(() => new ThingType("Broken", (IThingPart[])null!), "A thing type rejects a null part list.");
        IThingPart[] parts = new IThingPart[] { heal };
        ThingType copied = new ThingType("Copied", parts);
        parts[0] = new ItemPart(EItemEffect.Gold, 1);
        CheckEqual<ItemPart?>(heal, copied.GetPartOrNull<ItemPart>(), "Changing the given array later does not change the type.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddEntity(TestActors.Create(1, 5, 5, 10));
        Thing potion = map.AddEntity(new Thing(2, TestItems.POTION, 6, 5));
        Check(map.Contains(player) && map.Contains(potion), "Actors and things are both entities in the map.");
        CheckEqual(1, potion.Count, "A thing count defaults to one.");

        CheckThrows<ArgumentException>(() => map.AddEntity(new Thing(1, rock, 7, 5)), "A thing cannot reuse an actor's ID.");
        CheckThrows<ArgumentException>(() => map.AddEntity(TestActors.Create(2, 7, 5, 10)), "An actor cannot reuse a thing's ID.");
        CheckThrows<ArgumentException>(() => map.AddEntity(potion), "The same entity cannot be added twice.");
        Check(!map.Contains(new Thing(2, TestItems.POTION, 6, 5)), "Another entity with a used ID is not in the map.");
        CheckThrows<ArgumentNullException>(() => map.AddEntity<Thing>(null!), "A null entity is rejected.");

        map.AddEntity(new Thing(10, rock, 8, 5));
        int createdID = map.CreateEntityID();
        Check(createdID > 10, "A created ID is larger than every added ID.");
        CheckEqual(createdID + 1, map.CreateEntityID(), "Created IDs are not handed out twice.");

        map.AddEntity(new Thing(map.CreateEntityID(), rock, 6, 5));
        map.AddEntity(new Thing(map.CreateEntityID(), rock, 6, 5));
        CheckEqual<Thing?>(potion, map.GetItemAtOrNull(6, 5), "Things without an item part share a tile with an item.");
        map.AddEntity(new Thing(map.CreateEntityID(), rock, 4, 5));
        CheckEqual<Thing?>(null, map.GetItemAtOrNull(4, 5), "A thing without an item part is not an item.");
        CheckThrows<ArgumentException>(() => map.AddEntity(new Thing(map.CreateEntityID(), rock, -1, 5)),
            "Things must be inside the map.");

        GameRunner runner = new GameRunner(new World(map), player);
        runner.SubmitPlayerAction(MoveAction.LEFT);
        CheckEqual(false, runner.SubmitPlayerAction(PickUpAction.DEFAULT).IsExecuted,
            "A thing without an item part cannot be picked up.");
    }
}
