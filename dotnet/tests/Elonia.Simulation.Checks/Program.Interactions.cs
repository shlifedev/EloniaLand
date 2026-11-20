using System;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private static void CheckInteractions()
    {
        PropType tree = new PropType("Tree", false, new PropHarvest(TestItems.BRANCH, 1));
        PropType wall = new PropType("Wall", false);
        ActorType monsterType = new ActorType("Monster", 10, 1, 1, true);
        Check(monsterType.IsHostile && !new ActorType("Villager", 10, 1, 1).IsHostile, "An actor type is hostile only when asked.");
        Check(monsterType.IsThreat, "A hostile actor type that can attack is a threat.");
        Check(!new ActorType("Rat", 10, 0, 1, true).IsThreat, "A hostile actor type without attack power is not a threat.");
        Check(!new ActorType("Villager", 10, 1, 1).IsThreat, "A friendly actor type is not a threat.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        map.SetProp(8, 5, tree);
        map.SetProp(8, 6, wall);
        Actor player = map.AddEntity(TestActors.Create(1, 5, 5, 10));
        Actor monster = map.AddEntity(new Actor(2, monsterType, 6, 5, new FixedController(WaitAction.DEFAULT)));
        map.AddEntity(TestActors.Create(3, 5, 6, 1, new FixedController(WaitAction.DEFAULT)));
        map.AddEntity(new Thing(115, TestItems.BRANCH, 3, 5, 2));
        map.AddEntity(new Thing(116, TestItems.GOLD, 6, 5, 1));
        GameRunner runner = new GameRunner(world, player);
        InteractionPlan plan = new InteractionPlan();

        CheckThrows<ArgumentNullException>(() => plan.Collect(null!, player, 0, 0), "A plan needs a map.");
        CheckThrows<ArgumentNullException>(() => plan.Collect(map, null!, 0, 0), "A plan needs an actor.");

        plan.Collect(map, player, 4, 4);
        CheckEqual(0, plan.Items.Count, "An empty tile offers nothing.");
        plan.Collect(map, player, 8, 6);
        CheckEqual(0, plan.Items.Count, "A prop that cannot be harvested offers nothing.");
        plan.Collect(map, player, 5, 6);
        CheckEqual(0, plan.Items.Count, "A non-hostile actor offers nothing.");
        plan.Collect(map, player, -1, 0);
        CheckEqual(0, plan.Items.Count, "A tile outside the map offers nothing.");
        plan.Collect(map, player, 5, 5);
        CheckEqual(0, plan.Items.Count, "An actor offers nothing to itself.");

        plan.Collect(map, player, 8, 5);
        CheckEqual(1, plan.Items.Count, "A harvestable prop offers one interaction.");
        Interaction harvest = plan.Items[0];
        Check(harvest.Action is HarvestAction, "A harvestable prop offers a harvest.");
        CheckEqual("8,5,1", $"{harvest.TargetX},{harvest.TargetY},{harvest.Reach}", "A harvest targets the tile from one step away.");
        Check(!harvest.IsInReach(player), "A tile three steps away is not in reach yet.");
        CheckEqual(false, runner.SubmitPlayerAction(harvest.Action).IsExecuted,
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

        Check(plan.TryCollectBump(map, player, 6, 5, out Interaction bump) && bump.Action is AttackAction, "Bumping into a monster attacks it.");
        Check(plan.TryCollectBump(map, player, 8, 5, out bump) && bump.Action is HarvestAction, "Bumping into a harvestable prop harvests it.");
        Check(!plan.TryCollectBump(map, player, 3, 5, out _), "Bumping into an item does not pick it up.");
        Check(!plan.TryCollectBump(map, player, 5, 6, out _), "Bumping into a friendly actor does nothing.");
        Check(!plan.TryCollectBump(map, player, 4, 4, out _), "Bumping into an empty tile does nothing.");

        runner.SubmitPlayerAction(MoveAction.LEFT);
        Check(!attack.IsInReach(player), "Stepping away puts the monster out of reach.");
        runner.SubmitPlayerAction(MoveAction.RIGHT);
        CheckEqual(true, runner.SubmitPlayerAction(attack.Action).IsExecuted, "An interaction in reach can be submitted.");
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
