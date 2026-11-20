using System;
using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private const int MAP_WIDTH = 20;
    private const int MAP_HEIGHT = 12;

    private static int _failureCount;
    private static int _checkCount;

    public static int Main()
    {
        Run(CheckCadence);
        Run(CheckTicksUntilReady);
        Run(CheckRejectedMoveSpendsNoTime);
        Run(CheckSurplusEnergyCarriesOver);
        Run(CheckActionCosts);
        Run(CheckTurnOrder);
        Run(CheckRejectedNpcActionFallsBackToWait);
        Run(CheckPacingControllerTurnsAtEdge);
        Run(CheckSetupValidation);
        Run(CheckBehaviorPriority);
        Run(CheckBehaviorSetupValidation);
        Run(CheckMapLayers);
        Run(CheckCells);
        Run(CheckEntities);
        Run(CheckWorldOwnsTimeAcrossMaps);
        Run(CheckActorsBlockEachOther);
        Run(CheckDeadActorsTakeNoTurns);
        Run(CheckPathFinder);
        Run(CheckInventory);
        Run(CheckInteractions);
        Run(CheckDefinitionValidation);
        Run(CheckActionExecutedEvent);
        Run(MeasureActionAllocations);

        Console.WriteLine($"Simulation checks: {_checkCount - _failureCount}/{_checkCount} passed.");
        return _failureCount == 0 ? 0 : 1;
    }

    private static GameRunner CreateDefault()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddEntity(TestActors.Create(1, 3, 4, 10));
        map.AddEntity(TestActors.Create(2, 3, 8, 5, new PacingController()));
        return new GameRunner(new World(map), player);
    }

    private static GameRunner CreateWithNpc(Actor npc)
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddEntity(TestActors.Create(1, 3, 4, 10));
        map.AddEntity(npc);
        return new GameRunner(new World(map), player);
    }

    private static GameRunner CreatePlayerOnly(int x, int y, int speed)
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddEntity(TestActors.Create(1, x, y, speed));
        return new GameRunner(new World(map), player);
    }

    private static void CheckTicksUntilReady()
    {
        GameRunner runner = CreateDefault();
        Actor npc = runner.World.CurrentMap.Actors[1];
        CheckEqual(0L, GameRunner.GetTicksUntilReady(runner.Player), "The player is ready on its turn.");
        CheckEqual(10L, GameRunner.GetTicksUntilReady(npc), "The NPC needs the missing energy divided by its speed.");
        CheckThrows<ArgumentNullException>(() => GameRunner.GetTicksUntilReady(null!), "Ticks until ready needs an actor.");
    }

    private static void CheckCadence()
    {
        GameRunner runner = CreateDefault();
        Actor npc = runner.World.CurrentMap.Actors[1];
        CheckEqual(10L, runner.World.Tick, "Everyone starts at 0 energy; the first player turn comes at tick 10.");

        runner.SubmitPlayerAction(TestMoveAction.RIGHT);
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

        ActionResult result = runner.SubmitPlayerAction(TestMoveAction.LEFT);
        CheckEqual(false, result.IsExecuted, "Moving off the map is rejected.");
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
        Actor player = map.AddEntity(TestActors.Create(1, 3, 4, 100));
        Actor cheapNpc = map.AddEntity(TestActors.Create(2, 3, 8, 200, new FixedController(new CostAction(50))));
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
        map.AddEntity(TestActors.Create(9, 0, 0, 10, new FixedController(recordingAction)));
        Actor player = map.AddEntity(TestActors.Create(1, 3, 4, 10));
        map.AddEntity(TestActors.Create(2, 1, 0, 20, new FixedController(recordingAction)));
        map.AddEntity(TestActors.Create(5, 2, 0, 10, new FixedController(recordingAction)));

        GameRunner runner = new GameRunner(new World(map), player);
        CheckEqual("2,9", ZString.Join(",", executionOrder), "Actors before the player act before input.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual("2,9,2,5,2,9", ZString.Join(",", executionOrder),
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

    private static void CheckPacingControllerTurnsAtEdge()
    {
        GameRunner runner = CreateWithNpc(TestActors.Create(2, MAP_WIDTH - 2, 8, 10, new PacingController()));
        Actor npc = runner.World.CurrentMap.Actors[1];

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(MAP_WIDTH - 1, npc.X, "NPC walks right to the edge.");
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(MAP_WIDTH - 2, npc.X, "NPC turns around at the edge.");
    }

    private static void CheckSetupValidation()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        CheckThrows<ArgumentException>(() => map.AddEntity(TestActors.Create(1, MAP_WIDTH, 0, 10)),
            "Actors outside the map are rejected.");
        Actor player = map.AddEntity(TestActors.Create(1, 0, 0, 10));
        CheckThrows<ArgumentException>(() => map.AddEntity(player), "The same actor cannot be added twice.");

        CheckThrows<ArgumentException>(() => new GameRunner(new World(map), TestActors.Create(9, 0, 0, 10)),
            "The player must be in the current map.");

        Map controlledMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor controlledPlayer = controlledMap.AddEntity(TestActors.Create(1, 0, 0, 10, new PacingController()));
        CheckThrows<ArgumentException>(() => new GameRunner(new World(controlledMap), controlledPlayer),
            "The player must not have a controller.");

        GameRunner runner = CreateWithNpc(TestActors.Create(2, 3, 8, 10));
        CheckThrows<InvalidOperationException>(() => runner.SubmitPlayerAction(WaitAction.DEFAULT),
            "An NPC without a controller fails on its turn.");
    }

    private static GameRunner CreateWithBrain(int playerX, int playerY, Actor npc)
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddEntity(TestActors.Create(1, playerX, playerY, 10));
        map.AddEntity(npc);
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
        CheckEqual(2, brain.Behaviors.Count, "모든 행동 방식을 조회할 수 있다.");
        Check(brain.Behaviors[0] == high && brain.Behaviors[1] == low, "행동 방식은 우선순위 순서대로 조회된다.");
        CheckEqual<ActorAction?>(null, brain.LastActionOrNull, "첫 차례 전에는 선택한 행동이 없다.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual<IBehavior?>(low, brain.CurrentBehaviorOrNull, "The first behavior that can run is chosen.");
        CheckEqual(0, high.ChooseCount, "A behavior that cannot run is not asked for an action.");

        CheckEqual<ActorAction?>(WaitAction.DEFAULT, brain.LastActionOrNull, "마지막으로 고른 행동을 기록한다.");
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

    private static void CheckBehaviorSetupValidation()
    {
        CheckThrows<ArgumentException>(() => new Brain(), "A brain needs at least one behavior.");
        CheckThrows<ArgumentNullException>(() => new Brain((IBehavior[])null!), "A null behavior list is rejected.");
        CheckThrows<ArgumentException>(() => new Brain(new IBehavior[] { null! }), "A null behavior is rejected.");
    }

    private static void CheckWorldOwnsTimeAcrossMaps()
    {
        CheckThrows<ArgumentNullException>(() => new World(null!), "A world needs a current map.");

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddEntity(TestActors.Create(1, 3, 4, 10));
        World world = new World(map);
        CheckEqual(map, world.CurrentMap, "The world simulates the map it was created with.");

        Map otherMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor elsewhere = otherMap.AddEntity(TestActors.Create(2, 3, 4, 10));
        CheckThrows<ArgumentException>(() => new GameRunner(world, elsewhere), "The player must be in the current map.");

        GameRunner runner = new GameRunner(world, player);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(20L, world.Tick, "The world keeps the time.");
        CheckEqual(0L, elsewhere.Energy, "Actors in other maps gain no energy.");
    }

    private static void CheckMapLayers()
    {
        TestPropDef wall = new TestPropDef("Wall", false);
        TestPropDef gate = new TestPropDef("Gate", true);
        TestFloorDef dirt = new TestFloorDef("Dirt", true);
        TestFloorDef water = new TestFloorDef("Water", false);
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        IFloorDef ground = map.GetFloor(0, 0);
        Check(ground.IsWalkable, "새 맵의 기본 바닥은 걸을 수 있다.");
        CheckEqual<IPropDef?>(null, map.GetPropOrNull(0, 0), "A new map has no props.");

        map.SetFloor(5, 4, dirt);
        map.SetProp(5, 4, wall);
        CheckEqual(dirt, map.GetFloor(5, 4), "A floor can be replaced.");
        CheckEqual<IPropDef?>(wall, map.GetPropOrNull(5, 4), "A prop can be placed.");
        Check(map.IsWalkable(4, 4) && !map.IsWalkable(5, 4), "A blocking prop blocks its tile.");

        map.SetProp(6, 4, gate);
        map.SetFloor(7, 4, water);
        Check(map.IsWalkable(6, 4), "A walkable prop does not block its tile.");
        Check(!map.IsWalkable(7, 4), "A floor that cannot be walked on blocks its tile without a prop.");
        map.SetProp(7, 4, gate);
        Check(!map.IsWalkable(7, 4), "A walkable prop does not make a blocking floor walkable.");

        map.SetProp(6, 4, wall);
        CheckEqual<IPropDef?>(wall, map.GetPropOrNull(6, 4), "Setting a prop replaces the old one.");
        CheckEqual(ground, map.GetFloor(6, 4), "Replacing a prop leaves the floor as it is.");
        Check(!map.IsWalkable(6, 4), "A replaced prop decides whether the tile blocks.");

        Check(!map.IsWalkable(-1, 0) && !map.IsWalkable(0, MAP_HEIGHT), "Outside the map is never walkable.");
        CheckThrows<ArgumentOutOfRangeException>(() => map.GetFloor(MAP_WIDTH, 0), "Reading a floor outside the map is rejected.");
        CheckThrows<ArgumentOutOfRangeException>(() => map.SetFloor(0, -1, dirt), "Writing a floor outside the map is rejected.");
        CheckThrows<ArgumentNullException>(() => map.SetFloor(0, 0, null!), "A null floor type is rejected.");
        CheckThrows<ArgumentOutOfRangeException>(() => map.GetPropOrNull(MAP_WIDTH, 0), "Reading a prop outside the map is rejected.");
        CheckThrows<ArgumentOutOfRangeException>(() => map.SetProp(0, -1, wall), "Writing a prop outside the map is rejected.");
        CheckThrows<ArgumentNullException>(() => map.SetProp(0, 0, null!), "A null prop type is rejected.");
        CheckThrows<ArgumentException>(() => map.AddEntity(TestActors.Create(9, 5, 4, 10)), "Actors cannot be placed on a blocked tile.");
        CheckThrows<ArgumentException>(() => map.AddEntity(TestActors.Create(9, 7, 4, 10)), "Actors cannot be placed on a blocking floor.");
        CheckThrows<ArgumentException>(() => map.AddEntity(new Thing(101, TestItems.GOLD, 5, 4, 1)), "Items cannot be placed on a blocked tile.");

        Actor player = map.AddEntity(TestActors.Create(1, 4, 4, 10));
        GameRunner runner = new GameRunner(world, player);
        ActionResult result = runner.SubmitPlayerAction(TestMoveAction.RIGHT);
        CheckEqual(false, result.IsExecuted, "Moving into a blocked tile is rejected.");
        CheckEqual(4, player.X, "A blocked move does not change the position.");
        CheckEqual(10L, world.Tick, "A blocked move spends no time.");

        Map npcMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        npcMap.SetProp(5, 8, wall);
        Actor npcPlayer = npcMap.AddEntity(TestActors.Create(1, 3, 4, 10));
        Actor npc = npcMap.AddEntity(TestActors.Create(2, 4, 8, 10, new PacingController()));
        GameRunner npcRunner = new GameRunner(new World(npcMap), npcPlayer);
        npcRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(3, npc.X, "An NPC turns around at a blocked tile.");
    }

    private static void CheckActorsBlockEachOther()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        Actor player = map.AddEntity(TestActors.Create(1, 5, 5, 10));
        Actor npc = map.AddEntity(TestActors.Create(2, 6, 5, 1, new FixedController(WaitAction.DEFAULT)));
        GameRunner runner = new GameRunner(world, player);
        long startTick = world.Tick;

        CheckThrows<ArgumentException>(() => map.AddEntity(TestActors.Create(3, 6, 5, 10)),
            "Two living actors cannot be placed on the same tile.");
        CheckEqual<Actor?>(npc, map.GetLivingActorAtOrNull(6, 5), "A living actor can be found by its tile.");
        Check(map.IsWalkable(6, 5) && !map.CanEnter(6, 5), "A tile with a living actor is walkable but cannot be entered.");

        CheckEqual(false, runner.SubmitPlayerAction(TestMoveAction.RIGHT).IsExecuted,
            "Moving into a living actor is rejected.");
        CheckEqual(5, player.X, "A move blocked by an actor does not change the position.");
        CheckEqual(startTick, world.Tick, "A move blocked by an actor spends no time.");

        npc.TakeDamage(npc.Hp);
        Check(!npc.IsAlive, "체력을 모두 잃은 NPC는 죽는다.");
        CheckEqual<Actor?>(null, map.GetLivingActorAtOrNull(6, 5), "A dead actor does not occupy its tile.");
        CheckEqual(true, runner.SubmitPlayerAction(TestMoveAction.RIGHT).IsExecuted,
            "The tile of a dead actor can be entered.");
        CheckEqual(6, player.X, "The player moved onto the dead actor's tile.");
    }

    private static void CheckPathFinder()
    {
        TestPropDef wall = new TestPropDef("Wall", false);
        PathFinder pathFinder = new PathFinder();
        List<(int X, int Y)> path = new List<(int X, int Y)>();

        Map openMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        Check(pathFinder.TryFindPath(openMap, 2, 5, 6, 5, path), "A path is found on an open map.");
        CheckEqual(4, path.Count, "The straight path is as long as the distance.");
        CheckEqual((3, 5), path[0], "The path starts with the first step.");
        CheckEqual((6, 5), path[path.Count - 1], "The path ends at the goal.");
        Check(!pathFinder.TryFindPath(openMap, 2, 5, 2, 5, path) && path.Count == 0, "No path is needed to stand still.");
        CheckThrows<ArgumentOutOfRangeException>(() => pathFinder.TryFindPath(openMap, 2, 5, MAP_WIDTH, 5, path),
            "A goal outside the map is rejected.");

        Map wallMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        for (int y = 3; y <= 7; y++)
        {
            wallMap.SetProp(4, y, wall);
        }
        Check(pathFinder.TryFindPath(wallMap, 2, 5, 6, 5, path), "A path is found around a wall.");
        CheckEqual(10, path.Count, "The path around the wall is the shortest detour.");
        Check(IsConnectedPath(wallMap, 2, 5, path), "Every step is one orthogonal move onto a walkable tile.");

        Map actorMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        actorMap.AddEntity(TestActors.Create(1, 4, 5, 10));
        actorMap.AddEntity(TestActors.Create(2, 8, 5, 10));
        Check(pathFinder.TryFindPath(actorMap, 2, 5, 6, 5, path), "A path is found around an actor.");
        CheckEqual(6, path.Count, "Stepping around an actor costs two extra steps.");
        Check(!path.Contains((4, 5)), "The path does not pass through a living actor.");
        Check(pathFinder.TryFindPath(actorMap, 5, 5, 8, 5, path), "A path is found to an actor's tile.");
        CheckEqual((8, 5), path[path.Count - 1], "An actor on the goal tile is allowed as the last step.");

        Map treeMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        treeMap.SetProp(10, 5, wall);
        Check(pathFinder.TryFindPath(treeMap, 2, 5, 10, 5, path), "A path is found toward a blocked goal.");
        CheckEqual((9, 5), path[path.Count - 1], "The path stops at the reachable tile closest to a blocked goal.");
        CheckEqual(7, path.Count, "Among the closest tiles, the one with the shortest path is chosen.");
        Check(!pathFinder.TryFindPath(treeMap, 9, 5, 10, 5, path) && path.Count == 0,
            "No path is returned when the actor already stands on the closest tile.");

        pathFinder.TryFindPath(wallMap, 0, 0, MAP_WIDTH - 1, MAP_HEIGHT - 1, path);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int index = 0; index < 1000; index++)
        {
            pathFinder.TryFindPath(wallMap, 0, 0, MAP_WIDTH - 1, MAP_HEIGHT - 1, path);
        }
        CheckEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before, "A warmed path finder does not allocate.");
    }

    private static bool IsConnectedPath(Map map, int fromX, int fromY, List<(int X, int Y)> path)
    {
        int x = fromX;
        int y = fromY;
        for (int index = 0; index < path.Count; index++)
        {
            (int nextX, int nextY) = path[index];
            if (Math.Abs(nextX - x) + Math.Abs(nextY - y) != 1 || !map.CanEnter(nextX, nextY))
            {
                return false;
            }

            x = nextX;
            y = nextY;
        }

        return true;
    }

    private static void CheckDeadActorsTakeNoTurns()
    {
        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        Actor player = map.AddEntity(TestActors.Create(1, 5, 5, 10));
        FixedController controller = new FixedController(WaitAction.DEFAULT);
        Actor npc = map.AddEntity(TestActors.Create(2, 6, 5, 10, controller));
        GameRunner runner = new GameRunner(new World(map), player);

        npc.TakeDamage(npc.Hp);
        Check(!npc.IsAlive, "체력을 모두 잃은 NPC는 죽는다.");
        int chooseCount = controller.ChooseCount;
        long energy = npc.Energy;
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(chooseCount, controller.ChooseCount, "A dead actor's controller is no longer asked.");
        CheckEqual(energy, npc.Energy, "A dead actor gains no energy.");
        Check(map.Contains(npc), "A dead actor stays in the map's actor list.");
    }

    private static void CheckDefinitionValidation()
    {
        CheckThrows<ArgumentNullException>(() => new Actor(1, null!, 0, 0), "액터에는 정의가 필요하다.");
        CheckThrows<ArgumentNullException>(() => new Thing(1, null!, 0, 0), "물건에는 정의가 필요하다.");
    }

    private static void CheckActionExecutedEvent()
    {
        GameRunner runner = CreateDefault();
        ActionRecorder recorder = new ActionRecorder();
        runner.World.Events.Register<ActionExecuted>(recorder);

        runner.SubmitPlayerAction(new RejectAction());
        CheckEqual(0, recorder.Log.Count, "A rejected action is not reported.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        string wait = WaitAction.DEFAULT.Name;
        string move = TestMoveAction.RIGHT.Name;
        CheckEqual($"1 {wait} 100 @10|1 {wait} 100 @20|2 {move} 100 @20", ZString.Join("|", recorder.Log), "Every executed action is reported with its actor, cost and tick.");

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

    private static void MeasureActionAllocations()
    {
        const int WARMUP_COUNT = 10000;
        const int ACTION_COUNT = 100000;
        GameRunner runner = CreateDefault();
        ActorAction[] actions = new ActorAction[] { TestMoveAction.LEFT, WaitAction.DEFAULT, TestMoveAction.RIGHT };
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
