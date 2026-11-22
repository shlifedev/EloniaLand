using System;
using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Simulation;
using static Elonia.Checks.Checker;

namespace Elonia.Simulation.Checks;

internal static partial class SimulationChecks
{
    private const int MAP_WIDTH = 20;
    private const int MAP_HEIGHT = 12;

    public static void RunAll()
    {
        Run(CheckRingBuffer);
        Run(CheckCadence);
        Run(CheckTicksUntilReady);
        Run(CheckRejectedMoveSpendsNoTime);
        Run(CheckSurplusEnergyCarriesOver);
        Run(CheckActionCosts);
        Run(CheckNpcSpeedCap);
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
        Run(CheckWorldRandom);
        Run(CheckRestore);
        Run(CheckActorsBlockEachOther);
        Run(CheckDeadActorsTakeNoTurns);
        Run(CheckPathFinder);
        Run(CheckBlockedGoalPaths);
        Run(CheckGridReach);
        Run(CheckInventory);
        Run(CheckInteractions);
        Run(CheckDefinitionValidation);
        Run(CheckActorStats);
        Run(CheckActionExecutedEvent);
        Run(MeasureActionAllocations);
    }

    private static void CheckWorldRandom()
    {
        World first = new World(new Map(1, 1), seed: 42);
        World second = new World(new Map(1, 1), seed: 42);
        int[] expected = new int[] { 559678136, 2068313097, 974992177, 64156308, 1067743308, 1273847919 };
        foreach (int value in expected)
        {
            CheckEqual(value, first.Random.NextInt(int.MaxValue), "월드 난수의 수열이 PCG32 기준값과 일치한다.");
        }

        CheckThrows<ArgumentOutOfRangeException>(() => second.Random.NextInt(0), "난수 상한이 0이면 거부한다.");
        CheckThrows<ArgumentOutOfRangeException>(() => second.Random.NextInt(-1), "난수 상한이 음수면 거부한다.");
        CheckEqual(expected[0], second.Random.NextInt(int.MaxValue), "다른 월드의 추첨과 잘못된 호출은 이 월드의 첫 값을 바꾸지 않는다.");
        CheckEqual(0, second.Random.NextInt(1), "상한이 1이면 0을 뽑는다.");
        CheckEqual(expected[2], second.Random.NextInt(int.MaxValue), "상한이 1이어도 한 번 추첨한다.");

        WorldRandom zeroSeed = new WorldRandom(0);
        CheckEqual(249872251, zeroSeed.NextInt(1431655766), "상한의 배수가 되지 않는 구간은 건너뛰고 다시 뽑는다.");
        WorldRandom negativeSeed = new WorldRandom(-1);
        CheckEqual(406254218, negativeSeed.NextInt(int.MaxValue), "음수 시드의 32비트 패턴을 보존한다.");
    }

    private static void CheckRestore()
    {
        Map map = new Map(3, 1);
        map.SetProp(1, 0, new TestPropDef("Wall", false));
        Actor player = TestActors.Create(2, 1, 0, 10);
        Actor dead = TestActors.Create(3, 1, 0, 5, new FixedController(WaitAction.DEFAULT));
        Actor first = TestActors.Create(1, 0, 0, 5, new FixedController(WaitAction.DEFAULT));
        first.RestoreState(4, 0, 1, -15, false);
        player.RestoreState(7, 5, 3, 115, true);
        dead.RestoreState(0, 0, 2, 50, false);
        map.RestoreEntity(first);
        map.RestoreEntity(player);
        map.RestoreEntity(dead);
        Thing item = map.RestoreEntity(new Thing(4, TestItems.GOLD, 1, 0));
        map.RestoreNextEntityID(9);

        CheckEqual<Actor?>(player, map.GetLivingActorAtOrNull(1, 0), "시체를 나중에 복원해도 그 칸의 살아 있는 액터가 남는다.");
        CheckEqual<Thing?>(item, map.GetItemAtOrNull(1, 0), "이전에 놓인 아이템은 막힌 지형에서도 복원한다.");
        CheckEqual(9, map.CreateEntityID(), "주워서 사라진 물건의 ID를 재사용하지 않는다.");
        CheckThrows<ArgumentException>(() => map.RestoreEntity(new Thing(2, TestItems.GOLD, 2, 0)), "다른 종류가 이미 쓰는 ID를 거부한다.");
        CheckThrows<ArgumentException>(() => map.RestoreEntity(TestActors.Create(11, 1, 0, 5)), "살아 있는 액터가 겹치는 복원을 거부한다.");
        CheckThrows<ArgumentException>(() => map.RestoreEntity(new Thing(11, TestItems.GOLD, 1, 0)), "같은 칸에 아이템 두 개를 복원하지 않는다.");
        CheckThrows<ArgumentException>(() => map.RestoreEntity(new Thing(11, TestItems.GOLD, 4, 0)), "맵 바깥에 있는 개체를 거부한다.");
        CheckThrows<ArgumentOutOfRangeException>(() => map.RestoreNextEntityID(2), "이미 쓰인 ID로 발급 위치를 되돌리지 않는다.");
        CheckThrows<InvalidOperationException>(() => dead.RestoreState(5, 0, 0, 0, false), "칸 점유 검사를 우회해 이미 배치한 시체를 되살릴 수 없다.");
        CheckEqual(10, map.NextEntityID, "거부된 개체들은 다음 ID를 소비하지 않는다.");

        WorldRandom random = new WorldRandom(42);
        random.NextInt(10);
        World world = World.Restore(map, null, 30, random.State, 42);
        CheckEqual(42, world.Seed, "난수의 현재 상태와 함께 처음 시드도 보존한다.");
        GameRunner runner = GameRunner.Restore(world, player, 1);
        CheckEqual(30L, world.Tick, "복원 과정에서 새 틱을 실행하지 않는다.");
        CheckEqual(1, runner.TurnIndex, "플레이어가 목록 첫 번째가 아니어도 차례를 복원한다.");
        CheckEqual(115L, player.Energy, "입력 대기 중인 플레이어의 남는 에너지를 복원한다.");
        CheckEqual(-15L, first.Energy, "비싼 행동으로 음수가 된 NPC 에너지를 복원한다.");
        CheckEqual(random.NextInt(100), world.Random.NextInt(100), "다음 무작위 선택을 저장 시점에서 이어간다.");
        CheckThrows<ArgumentException>(() => GameRunner.Restore(world, player, 0), "입력 대기와 다른 액터 차례는 거부한다.");
        CheckEqual(7, player.Hp, "치트 무적과 별도로 현재 HP를 복원한다.");
        Check(player.IsInvincible && player.Gold == 5 && player.ActionCount == 3, "무적, 골드, 행동 횟수를 복원한다.");
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
        CheckEqual(0L, runner.GetTicksUntilReady(runner.Player), "The player is ready on its turn.");
        CheckEqual(10L, runner.GetTicksUntilReady(npc), "The NPC needs the missing energy divided by its speed.");
        CheckThrows<ArgumentNullException>(() => runner.GetTicksUntilReady(null!), "Ticks until ready needs an actor.");
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

    private static void CheckNpcSpeedCap()
    {
        GameRunner runner = CreateWithNpc(TestActors.Create(2, 3, 8, 1000, new FixedController(WaitAction.DEFAULT)));
        Actor fastNpc = runner.World.CurrentMap.Actors[1];
        CheckEqual(10L, runner.GetEnergyPerTick(runner.Player), "The player gains its own speed.");
        CheckEqual(100L, runner.GetEnergyPerTick(fastNpc), "An NPC gains at most ten times the player's speed.");
        CheckThrows<ArgumentNullException>(() => runner.GetEnergyPerTick(null!), "Energy per tick needs an actor.");

        long before = fastNpc.ActionCount;
        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(10L, fastNpc.ActionCount - before, "The capped NPC acts ten times per player action.");

        GameRunner slowRunner = CreateDefault();
        CheckEqual(5L, slowRunner.GetEnergyPerTick(slowRunner.World.CurrentMap.Actors[1]), "A slower NPC keeps its speed.");
        GameRunner edgeRunner = CreateWithNpc(TestActors.Create(2, 3, 8, 100, new FixedController(WaitAction.DEFAULT)));
        CheckEqual(100L, edgeRunner.GetEnergyPerTick(edgeRunner.World.CurrentMap.Actors[1]), "Exactly ten times is not cut.");

        TestStats playerStats = (TestStats)runner.Player.Stats;
        playerStats.Speed = 5;
        CheckEqual(5L, runner.GetEnergyPerTick(runner.Player), "A changed speed is used for energy.");
        CheckEqual(50L, runner.GetEnergyPerTick(fastNpc), "The cap follows the player's current speed.");
        playerStats.Speed = 10;

        GameRunner forecastRunner = CreateWithNpc(TestActors.Create(2, 3, 8, 1000, new FixedController(new CostAction(300))));
        Actor forecastNpc = forecastRunner.World.CurrentMap.Actors[1];
        forecastRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(20L, forecastRunner.World.Tick, "The player waits ten ticks.");
        CheckEqual(-100L, forecastNpc.Energy, "The NPC is still repaying its last action.");
        CheckEqual(2L, forecastRunner.GetTicksUntilReady(forecastNpc), "Ticks until ready uses the capped energy.");
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
        CheckEqual(8, path.Count, "The path around the wall is the shortest detour.");
        Check(IsConnectedPath(wallMap, 2, 5, path), "Every step moves to a neighbouring enterable tile without cutting a wall corner.");

        Map actorMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        actorMap.AddEntity(TestActors.Create(1, 4, 5, 10));
        actorMap.AddEntity(TestActors.Create(2, 8, 5, 10));
        Check(pathFinder.TryFindPath(actorMap, 2, 5, 6, 5, path), "A path is found around an actor.");
        CheckEqual(4, path.Count, "Stepping diagonally around an actor costs no extra steps.");
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

    private static void CheckBlockedGoalPaths()
    {
        TestPropDef wall = new TestPropDef("Wall", false);
        Map map = new Map(5, 5);
        map.SetProp(3, 3, wall);
        WalkabilityProbe farFloor = new WalkabilityProbe();
        map.SetFloor(4, 4, farFloor);
        PathFinder pathFinder = new PathFinder();
        List<(int X, int Y)> path = new List<(int X, int Y)> { (0, 0) };

        long before = GC.GetAllocatedBytesForCurrentThread();
        bool bFound = pathFinder.TryFindPath(map, 2, 3, 3, 3, path);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(!bFound && path.Count == 0, "이미 벽 옆이면 이전 경로를 비우고 제자리에 선다.");
        CheckEqual(0L, allocated, "이미 벽 옆이면 첫 호출에서도 탐색 버퍼를 할당하지 않는다.");

        Check(pathFinder.TryFindPath(map, 1, 1, 3, 3, path) && path.Count == 1 && path[0] == (2, 2),
            "막힌 목표에는 대각선 옆 칸으로도 다가간다.");
        Check(pathFinder.TryFindPath(map, 1, 3, 3, 3, path) && path.Count == 1 && path[0] == (2, 3),
            "막힌 목표 옆에 같은 길이의 경로가 여럿이면 상하좌우를 대각선보다 먼저 고른다.");
        CheckEqual(0, farFloor.ReadCount, "벽 옆에 도달할 수 있으면 더 먼 칸의 통행 여부를 조회하지 않는다.");

        foreach ((int deltaX, int deltaY) in GridMath.DIRECTIONS)
        {
            map.SetProp(3 + deltaX, 3 + deltaY, wall);
        }
        Check(pathFinder.TryFindPath(map, 0, 0, 3, 3, path) && path.Count == 1 && path[0] == (1, 1),
            "목표의 여덟 이웃을 모두 막으면 한 칸 더 떨어진 곳에서 가장 짧은 경로를 찾는다.");
    }

    private sealed class WalkabilityProbe : IFloorDef
    {
        public string Id => "Probe";
        public int ReadCount { get; private set; }
        public bool IsWalkable
        {
            get
            {
                ReadCount++;
                return true;
            }
        }
    }

    private static bool IsConnectedPath(Map map, int fromX, int fromY, List<(int X, int Y)> path)
    {
        int x = fromX;
        int y = fromY;
        for (int index = 0; index < path.Count; index++)
        {
            (int nextX, int nextY) = path[index];
            if (GridMath.GetChebyshevDistance(x, y, nextX, nextY) != 1 || !map.CanStep(x, y, nextX - x, nextY - y))
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
        CheckThrows<ArgumentNullException>(() => new Actor(1, null!, new TestStats(10, 10), 0, 0), "액터에는 정의가 필요하다.");
        CheckThrows<ArgumentNullException>(() => new Actor(1, TestActorDef.DEFAULT, null!, 0, 0), "액터에는 능력치가 필요하다.");
        CheckThrows<ArgumentOutOfRangeException>(() => new Actor(1, TestActorDef.DEFAULT, new TestStats(0, 10), 0, 0),
            "최대 체력이 0인 액터는 만들 수 없다.");
        CheckThrows<ArgumentOutOfRangeException>(() => new Actor(1, TestActorDef.DEFAULT, new TestStats(10, 0), 0, 0),
            "속도가 0인 액터는 만들 수 없다.");
        CheckThrows<ArgumentNullException>(() => new Thing(1, null!, 0, 0), "물건에는 정의가 필요하다.");
    }

    private static void CheckActorStats()
    {
        TestStats stats = new TestStats(10, 10);
        Actor actor = new Actor(1, TestActorDef.DEFAULT, stats, 0, 0);
        CheckEqual(10, actor.Hp, "An actor starts at its max HP.");
        CheckEqual(10, actor.Speed, "Speed is read from the stats.");

        stats.MaxHp = 20;
        CheckEqual(20, actor.MaxHp, "Max HP is read from the stats.");
        CheckEqual(10, actor.Hp, "A larger max HP does not heal.");
        actor.Heal(100);
        CheckEqual(20, actor.Hp, "Healing stops at the current max HP.");

        stats.MaxHp = 5;
        actor.Heal(1);
        CheckEqual(20, actor.Hp, "Healing above a lowered max HP does not lower HP.");
        actor.ClampHpToMax();
        CheckEqual(5, actor.Hp, "Clamping cuts HP down to the lowered max HP.");
        actor.ClampHpToMax();
        CheckEqual(5, actor.Hp, "Clamping again changes nothing.");
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
        string wait = WaitAction.DEFAULT.Id;
        string move = TestMoveAction.RIGHT.Id;
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
}
