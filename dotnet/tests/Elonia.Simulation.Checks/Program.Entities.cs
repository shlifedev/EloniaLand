using System;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckEntities()
    {
        TestThingDef rock = new TestThingDef("Rock");
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

        CheckThrows<ArgumentException>(() => map.AddEntity(new Thing(104, TestItems.GOLD, 6, 5)), "한 칸에는 아이템을 하나만 놓을 수 있다.");
        CheckThrows<ArgumentException>(() => map.AddEntity(new Thing(105, TestItems.GOLD, -1, 5)), "아이템을 맵 밖에 놓을 수 없다.");
        CheckThrows<ArgumentOutOfRangeException>(() => new Thing(106, TestItems.GOLD, 0, 0, 0), "물건 수량은 양수다.");

        CheckMovementValidation();
    }
}
