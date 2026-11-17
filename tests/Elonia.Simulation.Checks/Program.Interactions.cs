using System;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckInteractions()
    {
        TileType tree = new TileType("Tree", false, new TileHarvest(TileType.GROUND, EItemType.Branch, 1));
        TileType wall = new TileType("Wall", false);
        ActorType monsterType = new ActorType("Monster", 10, 1, 1, true);
        Check(monsterType.IsHostile && !new ActorType("Villager", 10, 1, 1).IsHostile, "An actor type is hostile only when asked.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        map.SetTile(8, 5, tree);
        map.SetTile(8, 6, wall);
        Actor player = map.AddActor(TestActors.Create(1, 5, 5, 10));
        Actor monster = map.AddActor(new Actor(2, monsterType, 6, 5, new FixedController(WaitAction.DEFAULT)));
        map.AddActor(TestActors.Create(3, 5, 6, 1, new FixedController(WaitAction.DEFAULT)));
        map.AddItem(new Item(EItemType.Branch, 3, 5, 2));
        map.AddItem(new Item(EItemType.Gold, 6, 5, 1));
        GameRunner runner = new GameRunner(world, player);
        InteractionPlan plan = new InteractionPlan();

        CheckThrows<ArgumentNullException>(() => plan.Collect(null!, player, 0, 0), "A plan needs a map.");
        CheckThrows<ArgumentNullException>(() => plan.Collect(map, null!, 0, 0), "A plan needs an actor.");

        plan.Collect(map, player, 4, 4);
        CheckEqual(0, plan.Items.Count, "An empty tile offers nothing.");
        plan.Collect(map, player, 8, 6);
        CheckEqual(0, plan.Items.Count, "A tile that cannot be harvested offers nothing.");
        plan.Collect(map, player, 5, 6);
        CheckEqual(0, plan.Items.Count, "A non-hostile actor offers nothing.");
        plan.Collect(map, player, -1, 0);
        CheckEqual(0, plan.Items.Count, "A tile outside the map offers nothing.");
        plan.Collect(map, player, 5, 5);
        CheckEqual(0, plan.Items.Count, "An actor offers nothing to itself.");

        plan.Collect(map, player, 8, 5);
        CheckEqual(1, plan.Items.Count, "A harvestable tile offers one interaction.");
        Interaction harvest = plan.Items[0];
        Check(harvest.Action is HarvestAction, "A harvestable tile offers a harvest.");
        CheckEqual("8,5,1", $"{harvest.TargetX},{harvest.TargetY},{harvest.Reach}", "A harvest targets the tile from one step away.");
        Check(!harvest.IsInReach(player), "A tile three steps away is not in reach yet.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(harvest.Action).Status,
            "An interaction out of reach is rejected by its action.");

        plan.Collect(map, player, 3, 5);
        CheckEqual(1, plan.Items.Count, "An item offers one interaction.");
        Check(plan.Items[0].Action is PickUpAction, "An item offers a pick up.");
        CheckEqual("3,5,0", $"{plan.Items[0].TargetX},{plan.Items[0].TargetY},{plan.Items[0].Reach}", "A pick up targets the item's own tile.");

        plan.Collect(map, player, 6, 5);
        CheckEqual(2, plan.Items.Count, "A hostile actor standing on an item offers two interactions.");
        Interaction attack = plan.Items[0];
        Check(attack.Action is AttackAction attackAction && attackAction.Target == monster, "A hostile actor offers an attack on itself.");
        Check(plan.Items[1].Action is PickUpAction, "The item's interaction follows the actor's.");
        CheckEqual<Actor?>(monster, attack.TargetActorOrNull, "An attack interaction names its target actor.");
        CheckEqual(AttackAction.REACH, attack.Reach, "An attack is done from its reach.");
        Check(attack.IsInReach(player), "An adjacent monster is in reach.");
        Check(plan.TryGetFirst(1, out Interaction adjacent) && adjacent.Action == attack.Action, "The first adjacent interaction can be found.");
        Check(plan.TryGetFirst(0, out Interaction onTile) && onTile.Action is PickUpAction, "The first on-tile interaction can be found.");
        Check(!plan.TryGetFirst(2, out _), "A reach nobody registered finds nothing.");

        runner.SubmitPlayerAction(MoveAction.LEFT);
        Check(!attack.IsInReach(player), "Stepping away puts the monster out of reach.");
        runner.SubmitPlayerAction(MoveAction.RIGHT);
        CheckEqual(EActionStatus.Executed, runner.SubmitPlayerAction(attack.Action).Status, "An interaction in reach can be submitted.");
        CheckEqual(10 - TestActors.ATTACK_POWER, monster.Hp, "The submitted interaction attacked the monster.");

        while (monster.IsAlive)
        {
            runner.SubmitPlayerAction(attack.Action);
        }
        plan.Collect(map, player, 6, 5);
        CheckEqual(1, plan.Items.Count, "A dead actor offers nothing.");
        Check(plan.Items[0].Action is PickUpAction, "The item under a dead actor is still offered.");
    }
}
