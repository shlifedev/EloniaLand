using System;
using System.Collections.Generic;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Checks;

public static class Program
{
    private const int MAP_WIDTH = 20;
    private const int MAP_HEIGHT = 12;

    private static int _failureCount;
    private static int _checkCount;

    public static int Main()
    {
        Run(CheckCadence);
        Run(CheckRejectedMoveSpendsNoTime);
        Run(CheckSurplusEnergyCarriesOver);
        Run(CheckActionCosts);
        Run(CheckTurnOrder);
        Run(CheckRejectedNpcActionFallsBackToWait);
        Run(CheckNpcControllerTurnsAtEdge);
        Run(CheckSetupValidation);
        Run(CheckBehaviorPriority);
        Run(CheckFleeBehavior);
        Run(CheckWanderBehavior);
        Run(CheckFleeInterruptsWander);
        Run(CheckBehaviorSetupValidation);
        Run(CheckTiles);
        Run(CheckWorldOwnsTimeAcrossMaps);
        Run(CheckAttack);
        Run(CheckActorsBlockEachOther);
        Run(CheckDeadActorsTakeNoTurns);
        Run(CheckMonsterKillsPlayer);
        Run(CheckChaseBehavior);
        Run(CheckItems);
        Run(CheckTypeValidation);
        Run(CheckActionExecutedEvent);
        Run(CheckDebugInfo);
        Run(MeasureActionAllocations);

        Console.WriteLine($"Simulation checks: {_checkCount - _failureCount}/{_checkCount} passed.");
        return _failureCount == 0 ? 0 : 1;
    }

    private static GameRunner CreateDefault()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, 3, 4, 10));
        map.AddActor(TestActors.Create(2, 3, 8, 5, new NpcController()));
        return new GameRunner(new World(map), player);
    }

    private static GameRunner CreateWithNpc(Actor npc)
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, 3, 4, 10));
        map.AddActor(npc);
        return new GameRunner(new World(map), player);
    }

    private static GameRunner CreatePlayerOnly(int x, int y, int speed)
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, x, y, speed));
        return new GameRunner(new World(map), player);
    }

    private static void CheckCadence()
    {
        GameRunner runner = CreateDefault();
        Actor npc = runner.World.CurrentMap.Actors[1];
        CheckEqual(10L, runner.World.Tick, "Everyone starts at 0 energy; the first player turn comes at tick 10.");

        runner.SubmitPlayerAction(MoveAction.RIGHT);
        CheckEqual(4, runner.Player.X, "Player moved right.");
        CheckEqual(20L, runner.World.Tick, "One default action at speed 10 takes 10 ticks.");
        CheckEqual(0L, npc.ActionCount, "An NPC added after the player waits for the player's input.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(30L, runner.World.Tick, "Wait also spends the default cost.");
        CheckEqual(1L, npc.ActionCount, "NPC acts after the player's input on the shared tick.");
        CheckEqual(4, npc.X, "NPC moved right.");

        for (int index = 0; index < 998; index++)
        {
            runner.SubmitPlayerAction(WaitAction.DEFAULT);
        }
        CheckEqual(500L, npc.ActionCount, "2:1 cadence holds over 1000 player actions.");
        CheckEqual(10010L, runner.World.Tick, "World time matches 1000 default actions after the first turn.");
    }

    private static void CheckRejectedMoveSpendsNoTime()
    {
        GameRunner runner = CreatePlayerOnly(0, 0, 10);

        ActionResult result = runner.SubmitPlayerAction(MoveAction.LEFT);
        CheckEqual(EActionStatus.Rejected, result.Status, "Moving off the map is rejected.");
        CheckEqual(10L, runner.World.Tick, "A rejected action does not advance time.");
        CheckEqual(0L, runner.Player.ActionCount, "A rejected action is not counted.");
        CheckEqual(100L, runner.Player.Energy, "A rejected action spends no energy.");
    }

    private static void CheckSurplusEnergyCarriesOver()
    {
        GameRunner uneven = CreatePlayerOnly(3, 4, 7);
        CheckEqual(15L, uneven.World.Tick, "Speed 7 needs 15 ticks to reach 100.");
        CheckEqual(105L, uneven.Player.Energy, "Overflow energy is kept.");
        uneven.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(29L, uneven.World.Tick, "The kept 5 energy shortens the next wait to 14 ticks.");

        GameRunner fast = CreatePlayerOnly(3, 4, 200);
        CheckEqual(1L, fast.World.Tick, "Speed 200 is ready after one tick.");
        fast.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(1L, fast.World.Tick, "Surplus energy allows an action without advancing time.");
        fast.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(2L, fast.World.Tick, "Time advances once the surplus is spent.");
    }

    private static void CheckActionCosts()
    {
        GameRunner quick = CreatePlayerOnly(3, 4, 10);
        quick.SubmitPlayerAction(new CostAction(50));
        CheckEqual(15L, quick.World.Tick, "A cost of 50 takes half the default time.");

        GameRunner slow = CreatePlayerOnly(3, 4, 10);
        slow.SubmitPlayerAction(new CostAction(200));
        CheckEqual(30L, slow.World.Tick, "A cost of 200 takes twice the default time.");

        GameRunner slowNpcRunner = CreateWithNpc(TestActors.Create(2, 3, 8, 10, new FixedController(new CostAction(200))));
        Actor slowNpc = slowNpcRunner.World.CurrentMap.Actors[1];
        slowNpcRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(1L, slowNpc.ActionCount, "NPC acts once.");
        CheckEqual(0L, slowNpc.Energy, "Cost 200 left -100; ten ticks only recover it to 0.");
        slowNpcRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(1L, slowNpc.ActionCount, "NPC skips a turn while repaying the cost.");
        slowNpcRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(2L, slowNpc.ActionCount, "NPC acts again once energy recovers.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        Actor player = map.AddActor(TestActors.Create(1, 3, 4, 100));
        Actor cheapNpc = map.AddActor(TestActors.Create(2, 3, 8, 200, new FixedController(new CostAction(50))));
        GameRunner cheapNpcRunner = new GameRunner(world, player);
        cheapNpcRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(3L, cheapNpc.ActionCount, "Cheap actions let the NPC act repeatedly while ready.");
        CheckEqual(2L, world.Tick, "Speed 100 player waits one tick per action.");
    }

    private static void CheckTurnOrder()
    {
        List<int> executionOrder = new List<int>();
        CostAction recordingAction = new CostAction(100, executionOrder);
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        map.AddActor(TestActors.Create(9, 0, 0, 10, new FixedController(recordingAction)));
        Actor player = map.AddActor(TestActors.Create(1, 3, 4, 10));
        map.AddActor(TestActors.Create(2, 1, 0, 20, new FixedController(recordingAction)));
        map.AddActor(TestActors.Create(5, 2, 0, 10, new FixedController(recordingAction)));

        GameRunner runner = new GameRunner(new World(map), player);
        CheckEqual("2,9", string.Join(",", executionOrder), "Actors before the player act before input.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual("2,9,2,5,2,9", string.Join(",", executionOrder),
            "Actors after the player act after input, in add order.");
    }

    private static void CheckRejectedNpcActionFallsBackToWait()
    {
        FixedController controller = new FixedController(new RejectAction());
        GameRunner runner = CreateWithNpc(TestActors.Create(2, 3, 8, 10, controller));

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(1, controller.ChooseCount, "Controller is asked once.");
        CheckEqual(1L, runner.World.CurrentMap.Actors[1].ActionCount, "The rejected choice is replaced by a wait.");
    }

    private static void CheckNpcControllerTurnsAtEdge()
    {
        GameRunner runner = CreateWithNpc(TestActors.Create(2, MAP_WIDTH - 2, 8, 10, new NpcController()));
        Actor npc = runner.World.CurrentMap.Actors[1];

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(MAP_WIDTH - 1, npc.X, "NPC walks right to the edge.");
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(MAP_WIDTH - 2, npc.X, "NPC turns around at the edge.");
    }

    private static void CheckSetupValidation()
    {
        CheckThrows<ArgumentException>(() => new MoveAction(1, 1), "Diagonal moves are rejected at construction.");
        CheckThrows<ArgumentException>(() => new MoveAction(0, 0), "Zero moves are rejected at construction.");
        CheckThrows<ArgumentException>(() => new MoveAction(2, 0), "Multi-tile moves are rejected at construction.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        CheckThrows<ArgumentException>(() => map.AddActor(TestActors.Create(1, MAP_WIDTH, 0, 10)),
            "Actors outside the map are rejected.");
        Actor player = map.AddActor(TestActors.Create(1, 0, 0, 10));
        CheckThrows<ArgumentException>(() => map.AddActor(player), "The same actor cannot be added twice.");

        CheckThrows<ArgumentException>(() => new GameRunner(new World(map), TestActors.Create(9, 0, 0, 10)),
            "The player must be in the current map.");

        Map controlledMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor controlledPlayer = controlledMap.AddActor(TestActors.Create(1, 0, 0, 10, new NpcController()));
        CheckThrows<ArgumentException>(() => new GameRunner(new World(controlledMap), controlledPlayer),
            "The player must not have a controller.");

        GameRunner runner = CreateWithNpc(TestActors.Create(2, 3, 8, 10));
        CheckThrows<InvalidOperationException>(() => runner.SubmitPlayerAction(WaitAction.DEFAULT),
            "An NPC without a controller fails on its turn.");
    }

    private static GameRunner CreateWithBrain(int playerX, int playerY, Actor npc)
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, playerX, playerY, 10));
        map.AddActor(npc);
        return new GameRunner(new World(map), player);
    }

    private static void CheckBehaviorPriority()
    {
        SwitchBehavior high = new SwitchBehavior("High", false);
        SwitchBehavior low = new SwitchBehavior("Low", true);
        Brain brain = new Brain(high, low);
        Actor npc = TestActors.Create(2, 3, 8, 10, brain);
        GameRunner runner = CreateWithBrain(3, 4, npc);
        CheckEqual<IBehavior?>(null, brain.CurrentBehaviorOrNull, "No behavior is current before the first turn.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual<IBehavior?>(low, brain.CurrentBehaviorOrNull, "The first behavior that can run is chosen.");
        CheckEqual(0, high.ChooseCount, "A behavior that cannot run is not asked for an action.");

        high.IsEnabled = true;
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual<IBehavior?>(high, brain.CurrentBehaviorOrNull, "A higher-priority behavior interrupts as soon as it can run.");
        CheckEqual(1, low.ChooseCount, "The lower-priority behavior is skipped while interrupted.");

        high.IsEnabled = false;
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual<IBehavior?>(low, brain.CurrentBehaviorOrNull, "The lower-priority behavior resumes afterwards.");

        low.IsEnabled = false;
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual<IBehavior?>(null, brain.CurrentBehaviorOrNull, "No behavior is current when none can run.");
        CheckEqual(4L, npc.ActionCount, "The NPC waits when no behavior can run, so time keeps moving.");
    }

    private static void CheckFleeBehavior()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, 5, 5, 10));
        FleeBehavior flee = new FleeBehavior(player, 3);
        Brain brain = new Brain(flee);
        Actor npc = map.AddActor(TestActors.Create(2, 7, 5, 10, brain));
        GameRunner runner = new GameRunner(new World(map), player);

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(8, npc.X, "The NPC steps directly away from the player.");
        CheckEqual(5, npc.Y, "Fleeing straight away keeps the row.");
        CheckEqual<IBehavior?>(flee, brain.CurrentBehaviorOrNull, "Flee is the current behavior inside the sense range.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(9, npc.X, "A distance equal to the sense range still triggers fleeing.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(9, npc.X, "The NPC stops fleeing once outside the sense range.");
        CheckEqual<IBehavior?>(null, brain.CurrentBehaviorOrNull, "Flee does not run outside the sense range.");

        runner.SubmitPlayerAction(MoveAction.RIGHT);
        CheckEqual(10, npc.X, "The NPC flees again when the player approaches.");

        Map wallMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor wallPlayer = wallMap.AddActor(TestActors.Create(1, 5, 2, 10));
        Actor wallNpc = wallMap.AddActor(TestActors.Create(2, 5, 0, 10, new Brain(new FleeBehavior(wallPlayer, 3))));
        GameRunner wallRunner = new GameRunner(new World(wallMap), wallPlayer);
        wallRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual("4,0", $"{wallNpc.X},{wallNpc.Y}", "Against a wall, the NPC sidesteps along it.");

        Map cornerMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor cornerPlayer = cornerMap.AddActor(TestActors.Create(1, 2, 2, 10));
        Actor cornerNpc = cornerMap.AddActor(TestActors.Create(2, 0, 0, 10, new Brain(new FleeBehavior(cornerPlayer, 3))));
        GameRunner cornerRunner = new GameRunner(new World(cornerMap), cornerPlayer);
        cornerRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual("0,0", $"{cornerNpc.X},{cornerNpc.Y}", "A cornered NPC stays in place.");
        CheckEqual(1L, cornerNpc.ActionCount, "A cornered NPC still spends its turn.");
    }

    private static void CheckWanderBehavior()
    {
        const int TURN_COUNT = 500;
        const int SEED = 1234;

        Actor first = TestActors.Create(2, 10, 6, 10, new Brain(new WanderBehavior(new Random(SEED))));
        GameRunner firstRunner = CreateWithBrain(3, 4, first);
        bool bAlwaysInside = true;
        bool bMoved = false;
        for (int index = 0; index < TURN_COUNT; index++)
        {
            firstRunner.SubmitPlayerAction(WaitAction.DEFAULT);
            bAlwaysInside &= firstRunner.World.CurrentMap.IsInside(first.X, first.Y);
            bMoved |= first.X != 10 || first.Y != 6;
        }
        Check(bAlwaysInside, "A wandering NPC never leaves the map.");
        Check(bMoved, "A wandering NPC moves around.");
        CheckEqual((long)TURN_COUNT, first.ActionCount, "A wandering NPC acts on every turn.");

        Actor second = TestActors.Create(2, 10, 6, 10, new Brain(new WanderBehavior(new Random(SEED))));
        GameRunner secondRunner = CreateWithBrain(3, 4, second);
        for (int index = 0; index < TURN_COUNT; index++)
        {
            secondRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        }
        CheckEqual($"{first.X},{first.Y}", $"{second.X},{second.Y}", "The same seed produces the same path.");
    }

    private static void CheckFleeInterruptsWander()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, 3, 6, 10));
        FleeBehavior flee = new FleeBehavior(player, 4);
        WanderBehavior wander = new WanderBehavior(new Random(7));
        Brain brain = new Brain(flee, wander);
        Actor npc = map.AddActor(TestActors.Create(2, 15, 6, 10, brain));
        GameRunner runner = new GameRunner(new World(map), player);

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual<IBehavior?>(wander, brain.CurrentBehaviorOrNull, "Far from the player, the NPC wanders.");

        bool bFled = false;
        bool bConsistent = true;
        for (int index = 0; index < 200; index++)
        {
            int npcX = npc.X;
            int npcY = npc.Y;
            runner.SubmitPlayerAction(GetStepToward(player, npc));
            long deltaX = npcX - player.X;
            long deltaY = npcY - player.Y;
            bool bInRange = deltaX * deltaX + deltaY * deltaY <= 16;
            IBehavior expected = bInRange ? flee : wander;
            bConsistent &= brain.CurrentBehaviorOrNull == expected;
            bFled |= bInRange;
        }
        Check(bFled, "A player who keeps approaching gets inside the sense range.");
        Check(bConsistent, "The NPC flees exactly on the turns the player is inside the sense range.");
    }

    private static ActorAction GetStepToward(Actor from, Actor target)
    {
        if (target.X > from.X)
        {
            return MoveAction.RIGHT;
        }

        if (target.X < from.X)
        {
            return MoveAction.LEFT;
        }

        if (target.Y > from.Y)
        {
            return MoveAction.DOWN;
        }

        if (target.Y < from.Y)
        {
            return MoveAction.UP;
        }

        return WaitAction.DEFAULT;
    }

    private static void CheckBehaviorSetupValidation()
    {
        Actor player = TestActors.Create(1, 0, 0, 10);
        CheckThrows<ArgumentException>(() => new Brain(), "A brain needs at least one behavior.");
        CheckThrows<ArgumentNullException>(() => new Brain((IBehavior[])null!), "A null behavior list is rejected.");
        CheckThrows<ArgumentException>(() => new Brain(new IBehavior[] { null! }), "A null behavior is rejected.");
        CheckThrows<ArgumentNullException>(() => new FleeBehavior(null!, 3), "Flee needs a threat.");
        CheckThrows<ArgumentOutOfRangeException>(() => new FleeBehavior(player, 0), "Flee needs a positive sense range.");
        CheckThrows<ArgumentNullException>(() => new WanderBehavior(null!), "Wander needs a random source.");
    }

    private static void CheckWorldOwnsTimeAcrossMaps()
    {
        CheckThrows<ArgumentNullException>(() => new World(null!), "A world needs a current map.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, 3, 4, 10));
        World world = new World(map);
        CheckEqual(map, world.CurrentMap, "The world simulates the map it was created with.");

        Map otherMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor elsewhere = otherMap.AddActor(TestActors.Create(2, 3, 4, 10));
        CheckThrows<ArgumentException>(() => new GameRunner(world, elsewhere), "The player must be in the current map.");

        GameRunner runner = new GameRunner(world, player);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(20L, world.Tick, "The world keeps the time.");
        CheckEqual(0L, elsewhere.Energy, "Actors in other maps gain no energy.");
    }

    private static void CheckTiles()
    {
        TileType wall = new TileType("Wall", false);
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        CheckEqual(TileType.GROUND, map.GetTile(0, 0), "A new map is filled with ground.");
        map.SetTile(5, 4, wall);
        CheckEqual(wall, map.GetTile(5, 4), "A tile can be replaced.");
        Check(map.IsWalkable(4, 4) && !map.IsWalkable(5, 4), "Walkability follows the tile type.");
        Check(!map.IsWalkable(-1, 0) && !map.IsWalkable(0, MAP_HEIGHT), "Outside the map is never walkable.");
        CheckThrows<ArgumentOutOfRangeException>(() => map.GetTile(MAP_WIDTH, 0), "Reading a tile outside the map is rejected.");
        CheckThrows<ArgumentOutOfRangeException>(() => map.SetTile(0, -1, wall), "Writing a tile outside the map is rejected.");
        CheckThrows<ArgumentNullException>(() => map.SetTile(0, 0, null!), "A null tile type is rejected.");
        CheckThrows<ArgumentException>(() => map.AddActor(TestActors.Create(9, 5, 4, 10)), "Actors cannot be placed on a blocked tile.");

        Actor player = map.AddActor(TestActors.Create(1, 4, 4, 10));
        GameRunner runner = new GameRunner(world, player);
        ActionResult result = runner.SubmitPlayerAction(MoveAction.RIGHT);
        CheckEqual(EActionStatus.Rejected, result.Status, "Moving into a blocked tile is rejected.");
        CheckEqual(4, player.X, "A blocked move does not change the position.");
        CheckEqual(10L, world.Tick, "A blocked move spends no time.");

        Map npcMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        npcMap.SetTile(5, 8, wall);
        Actor npcPlayer = npcMap.AddActor(TestActors.Create(1, 3, 4, 10));
        Actor npc = npcMap.AddActor(TestActors.Create(2, 4, 8, 10, new NpcController()));
        GameRunner npcRunner = new GameRunner(new World(npcMap), npcPlayer);
        npcRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(3, npc.X, "An NPC turns around at a blocked tile.");
    }

    private static void CheckAttack()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        Actor player = map.AddActor(TestActors.Create(1, 5, 5, 10));
        Actor target = map.AddActor(TestActors.Create(2, 6, 5, 1, new FixedController(WaitAction.DEFAULT)));
        Actor farTarget = map.AddActor(TestActors.Create(3, 7, 5, 1, new FixedController(WaitAction.DEFAULT)));
        Actor diagonalTarget = map.AddActor(TestActors.Create(4, 6, 6, 1, new FixedController(WaitAction.DEFAULT)));
        EventRecorder recorder = new EventRecorder();
        world.Events.Register<ActorAttacked, ActorDied>(recorder);
        GameRunner runner = new GameRunner(world, player);
        long startTick = world.Tick;

        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new AttackAction(farTarget)).Status,
            "A target two tiles away is out of reach.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new AttackAction(diagonalTarget)).Status,
            "A diagonal target is out of reach.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new AttackAction(player)).Status,
            "An actor cannot attack itself.");
        CheckEqual(startTick, world.Tick, "Rejected attacks spend no time.");
        CheckEqual(0, recorder.Log.Count, "Rejected attacks raise no events.");

        ActionResult result = runner.SubmitPlayerAction(new AttackAction(target));
        CheckEqual(EActionStatus.Executed, result.Status, "An adjacent target can be attacked.");
        CheckEqual(TestActors.MAX_HP - TestActors.ATTACK_POWER, target.Hp, "Damage equals the attacker's attack power.");
        CheckEqual(startTick + 10, world.Tick, "An attack spends the default cost.");

        runner.SubmitPlayerAction(new AttackAction(target));
        runner.SubmitPlayerAction(new AttackAction(target));
        Check(target.IsAlive, "The target survives while HP remains.");
        runner.SubmitPlayerAction(new AttackAction(target));
        CheckEqual(0, target.Hp, "HP does not drop below zero.");
        Check(!target.IsAlive, "The target dies at zero HP.");
        CheckEqual("attack 1>2 3,attack 1>2 3,attack 1>2 3,attack 1>2 3,died 2", string.Join(",", recorder.Log),
            "Each hit raises an attack event, and the kill is followed by a death event.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(new AttackAction(target)).Status,
            "A dead target cannot be attacked.");
    }

    private static void CheckActorsBlockEachOther()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        Actor player = map.AddActor(TestActors.Create(1, 5, 5, 10));
        Actor npc = map.AddActor(TestActors.Create(2, 6, 5, 1, new FixedController(WaitAction.DEFAULT)));
        GameRunner runner = new GameRunner(world, player);
        long startTick = world.Tick;

        CheckThrows<ArgumentException>(() => map.AddActor(TestActors.Create(3, 6, 5, 10)),
            "Two living actors cannot be placed on the same tile.");
        CheckEqual<Actor?>(npc, map.GetLivingActorAtOrNull(6, 5), "A living actor can be found by its tile.");
        Check(map.IsWalkable(6, 5) && !map.CanEnter(6, 5), "A tile with a living actor is walkable but cannot be entered.");

        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(MoveAction.RIGHT).Status,
            "Moving into a living actor is rejected.");
        CheckEqual(5, player.X, "A move blocked by an actor does not change the position.");
        CheckEqual(startTick, world.Tick, "A move blocked by an actor spends no time.");

        for (int index = 0; index < 4; index++)
        {
            runner.SubmitPlayerAction(new AttackAction(npc));
        }
        Check(!npc.IsAlive, "The NPC is dead after four hits.");
        CheckEqual<Actor?>(null, map.GetLivingActorAtOrNull(6, 5), "A dead actor does not occupy its tile.");
        CheckEqual(EActionStatus.Executed, runner.SubmitPlayerAction(MoveAction.RIGHT).Status,
            "The tile of a dead actor can be entered.");
        CheckEqual(6, player.X, "The player moved onto the dead actor's tile.");

        Map chaseMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor chasePlayer = chaseMap.AddActor(TestActors.Create(1, 5, 5, 10));
        chaseMap.AddActor(TestActors.Create(2, 7, 6, 1, new FixedController(WaitAction.DEFAULT)));
        Actor chaser = chaseMap.AddActor(TestActors.Create(3, 8, 6, 10, new Brain(new ChaseBehavior(chasePlayer, 6))));
        GameRunner chaseRunner = new GameRunner(new World(chaseMap), chasePlayer);
        chaseRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual("8,5", $"{chaser.X},{chaser.Y}", "A chaser steps around an actor that blocks the closest tile.");
    }

    private static void CheckDeadActorsTakeNoTurns()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, 5, 5, 10));
        FixedController controller = new FixedController(WaitAction.DEFAULT);
        Actor npc = map.AddActor(TestActors.Create(2, 6, 5, 10, controller));
        GameRunner runner = new GameRunner(new World(map), player);

        for (int index = 0; index < 4; index++)
        {
            runner.SubmitPlayerAction(new AttackAction(npc));
        }
        Check(!npc.IsAlive, "The NPC is dead after four hits.");
        int chooseCount = controller.ChooseCount;
        long energy = npc.Energy;
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(chooseCount, controller.ChooseCount, "A dead actor's controller is no longer asked.");
        CheckEqual(energy, npc.Energy, "A dead actor gains no energy.");
        Check(map.Contains(npc), "A dead actor stays in the map's actor list.");
    }

    private static void CheckMonsterKillsPlayer()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        Actor player = map.AddActor(TestActors.Create(1, 5, 5, 10));
        AttackBehavior attack = new AttackBehavior(player);
        ChaseBehavior chase = new ChaseBehavior(player, 6);
        Brain brain = new Brain(attack, chase);
        Actor monster = map.AddActor(TestActors.Create(2, 9, 5, 10, brain));
        EventRecorder recorder = new EventRecorder();
        world.Events.Register<ActorAttacked, ActorDied>(recorder);
        GameRunner runner = new GameRunner(world, player);

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(8, monster.X, "The monster steps toward the player.");
        CheckEqual<IBehavior?>(chase, brain.CurrentBehaviorOrNull, "Chase runs while the player is sensed but out of reach.");
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(6, monster.X, "The monster stops next to the player.");
        Check(!runner.IsGameOver, "The game continues while the player lives.");

        int submitCount = 0;
        while (!runner.IsGameOver && submitCount < 10)
        {
            runner.SubmitPlayerAction(WaitAction.DEFAULT);
            submitCount++;
        }
        CheckEqual(4, submitCount, "The player dies after four monster attacks.");
        CheckEqual<IBehavior?>(attack, brain.CurrentBehaviorOrNull, "Attack has priority over chase when in reach.");
        CheckEqual("died 1", recorder.Log[recorder.Log.Count - 1], "The player's death is announced.");

        long tick = world.Tick;
        long monsterActions = monster.ActionCount;
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(WaitAction.DEFAULT).Status,
            "Player actions are rejected after game over.");
        CheckEqual(tick, world.Tick, "Time stops after game over.");
        CheckEqual(monsterActions, monster.ActionCount, "NPCs stop acting after game over.");
    }

    private static void CheckChaseBehavior()
    {
        TileType wall = new TileType("Wall", false);

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, 2, 5, 10));
        Brain brain = new Brain(new ChaseBehavior(player, 3));
        Actor monster = map.AddActor(TestActors.Create(2, 8, 5, 10, brain));
        GameRunner runner = new GameRunner(new World(map), player);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(8, monster.X, "A monster does not chase a player outside its sense range.");
        CheckEqual<IBehavior?>(null, brain.CurrentBehaviorOrNull, "Chase does not run outside the sense range.");

        Map blockedMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        blockedMap.SetTile(6, 5, wall);
        Actor blockedPlayer = blockedMap.AddActor(TestActors.Create(1, 5, 5, 10));
        Actor blockedMonster = blockedMap.AddActor(TestActors.Create(2, 7, 5, 10, new Brain(new ChaseBehavior(blockedPlayer, 5))));
        GameRunner blockedRunner = new GameRunner(new World(blockedMap), blockedPlayer);
        blockedRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual("7,5", $"{blockedMonster.X},{blockedMonster.Y}", "A monster waits when every closer tile is blocked.");
        CheckEqual(1L, blockedMonster.ActionCount, "A blocked monster still spends its turn.");
    }

    private static void CheckItems()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        Actor player = map.AddActor(TestActors.Create(1, 5, 5, 10));
        Actor monster = map.AddActor(TestActors.Create(2, 5, 6, 10, new Brain(new AttackBehavior(player))));
        Item potion = map.AddItem(new Item(EItemType.Potion, 5, 5, 5));
        map.AddItem(new Item(EItemType.Gold, 6, 5, 7));
        EventRecorder recorder = new EventRecorder();
        world.Events.Register<ItemPickedUp>(recorder);
        GameRunner runner = new GameRunner(world, player);

        CheckEqual<Item?>(potion, map.GetItemAtOrNull(5, 5), "An item can be found by its tile.");
        CheckEqual<Item?>(null, map.GetItemAtOrNull(4, 5), "An empty tile has no item.");
        CheckThrows<ArgumentException>(() => map.AddItem(new Item(EItemType.Gold, 5, 5, 1)), "A tile holds only one item.");
        CheckThrows<ArgumentException>(() => map.AddItem(new Item(EItemType.Gold, -1, 5, 1)), "Items must be inside the map.");
        CheckThrows<ArgumentOutOfRangeException>(() => new Item(EItemType.Gold, 0, 0, 0), "An item amount must be positive.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(4, player.Hp, "The player took two hits.");
        ActionResult result = runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        CheckEqual(EActionStatus.Executed, result.Status, "An item on the actor's tile can be picked up.");
        CheckEqual(4 + 5 - 3, player.Hp, "A potion heals by its amount.");
        CheckEqual(1, map.Items.Count, "A picked-up item leaves the map.");
        CheckEqual(EActionStatus.Rejected, runner.SubmitPlayerAction(PickUpAction.DEFAULT).Status,
            "Picking up on an empty tile is rejected.");

        runner.SubmitPlayerAction(MoveAction.RIGHT);
        runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        CheckEqual(7, player.Gold, "Gold is added by its amount.");
        CheckEqual("pickup 1 Potion 5,pickup 1 Gold 7", string.Join(",", recorder.Log), "Each pickup raises an event.");
        Check(monster.IsAlive, "The monster is untouched.");

        Map fullMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor fullPlayer = fullMap.AddActor(TestActors.Create(1, 5, 5, 10));
        fullMap.AddItem(new Item(EItemType.Potion, 5, 5, 5));
        GameRunner fullRunner = new GameRunner(new World(fullMap), fullPlayer);
        fullRunner.SubmitPlayerAction(PickUpAction.DEFAULT);
        CheckEqual(TestActors.MAX_HP, fullPlayer.Hp, "Healing does not exceed max HP.");
    }

    private static void CheckTypeValidation()
    {
        CheckThrows<ArgumentException>(() => new TileType("", true), "A tile type needs a name.");
        CheckThrows<ArgumentException>(() => new ActorType("", 1, 1, 1), "An actor type needs a name.");
        CheckThrows<ArgumentOutOfRangeException>(() => new ActorType("A", 0, 1, 1), "Max HP must be positive.");
        CheckThrows<ArgumentOutOfRangeException>(() => new ActorType("A", 1, -1, 1), "Attack power must not be negative.");
        CheckThrows<ArgumentOutOfRangeException>(() => new ActorType("A", 1, 1, 0), "Speed must be positive.");
        CheckThrows<ArgumentNullException>(() => new Actor(1, null!, 0, 0), "An actor needs a type.");
        CheckThrows<ArgumentNullException>(() => new AttackAction(null!), "An attack needs a target.");
        CheckThrows<ArgumentNullException>(() => new AttackBehavior(null!), "Attack behavior needs a target.");
        CheckThrows<ArgumentNullException>(() => new ChaseBehavior(null!, 3), "Chase needs a target.");
        Actor actor = TestActors.Create(1, 0, 0, 10);
        CheckThrows<ArgumentOutOfRangeException>(() => new ChaseBehavior(actor, 0), "Chase needs a positive sense range.");
    }

    private static void CheckActionExecutedEvent()
    {
        GameRunner runner = CreateDefault();
        ActionRecorder recorder = new ActionRecorder();
        runner.World.Events.Register<ActionExecuted>(recorder);

        runner.SubmitPlayerAction(PickUpAction.DEFAULT);
        CheckEqual(0, recorder.Log.Count, "A rejected action is not reported.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        string wait = WaitAction.DEFAULT.Name;
        string move = MoveAction.RIGHT.Name;
        CheckEqual($"1 {wait} 100 @10|1 {wait} 100 @20|2 {move} 100 @20", string.Join("|", recorder.Log), "Every executed action is reported with its actor, cost and tick.");

        CountingActionListener counter = new CountingActionListener();
        runner.World.Events.UnRegister<ActionExecuted>(recorder);
        runner.World.Events.Register<ActionExecuted>(counter);
        for (int index = 0; index < 1000; index++)
        {
            runner.SubmitPlayerAction(WaitAction.DEFAULT);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 10000; index++)
        {
            runner.SubmitPlayerAction(WaitAction.DEFAULT);
        }

        CheckEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before, "Reporting executed actions to a listener does not allocate.");
        Check(counter.Count > 11000, "The listener received every executed action.");
    }

    private static void CheckDebugInfo()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddActor(TestActors.Create(1, 3, 4, 10));
        FleeBehavior flee = new FleeBehavior(player, 4);
        ChaseBehavior chase = new ChaseBehavior(player, 6);
        AttackBehavior attack = new AttackBehavior(player);
        Brain brain = new Brain(attack, flee, chase);
        map.AddActor(TestActors.Create(2, 3, 6, 10, brain));
        GameRunner runner = new GameRunner(new World(map), player);

        CheckEqual(3, brain.Behaviors.Count, "A brain exposes all of its behaviors.");
        CheckEqual<IBehavior>(attack, brain.Behaviors[0], "Behaviors are listed in priority order.");
        CheckEqual<IBehavior>(chase, brain.Behaviors[2], "The last behavior has the lowest priority.");
        CheckEqual<ActorAction?>(null, brain.LastActionOrNull, "No action is recorded before the first turn.");
        CheckEqual(player, flee.Threat, "Flee exposes its threat.");
        CheckEqual(4, flee.SenseRange, "Flee exposes its sense range.");
        CheckEqual(player, chase.Target, "Chase exposes its target.");
        CheckEqual(6, chase.SenseRange, "Chase exposes its sense range.");
        CheckEqual(player, attack.Target, "Attack exposes its target.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual<IBehavior?>(flee, brain.CurrentBehaviorOrNull, "The player is within flee range.");
        CheckEqual<ActorAction?>(MoveAction.DOWN, brain.LastActionOrNull, "The brain records the action it chose.");
    }

    private static void MeasureActionAllocations()
    {
        const int WARMUP_COUNT = 10000;
        const int ACTION_COUNT = 100000;
        GameRunner runner = CreateDefault();
        ActorAction[] actions = new ActorAction[] { MoveAction.LEFT, WaitAction.DEFAULT, MoveAction.RIGHT };
        for (int index = 0; index < WARMUP_COUNT; index++)
        {
            runner.SubmitPlayerAction(actions[index % actions.Length]);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < ACTION_COUNT; index++)
        {
            runner.SubmitPlayerAction(actions[(WARMUP_COUNT + index) % actions.Length]);
        }
        long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;

        CheckEqual(0L, allocatedBytes, "Warmed game actions do not allocate.");
        Console.WriteLine($"Warmed simulation: {allocatedBytes} bytes / {ACTION_COUNT} mixed player actions.");
    }

    private static void Run(Action test)
    {
        try
        {
            test();
        }
        catch (Exception exception)
        {
            Check(false, $"{test.Method.Name} raised an unexpected exception: {exception}");
        }
    }

    private static void CheckThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Check(true, message);
            return;
        }
        catch (Exception exception)
        {
            Check(false, $"{message} Expected {typeof(TException).Name}, got {exception.GetType().Name}.");
            return;
        }

        Check(false, $"{message} No exception was thrown.");
    }

    private static void CheckEqual<T>(T expected, T actual, string message)
    {
        bool bEqual = EqualityComparer<T>.Default.Equals(expected, actual);
        Check(bEqual, $"{message} Expected {expected}, got {actual}.");
    }

    private static void Check(bool bCondition, string message)
    {
        _checkCount++;
        if (!bCondition)
        {
            _failureCount++;
            Console.Error.WriteLine($"FAIL: {message}");
        }
    }
}
