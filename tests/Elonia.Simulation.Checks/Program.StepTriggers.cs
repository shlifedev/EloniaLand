using System;
using System.Collections.Generic;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Checks;

public static partial class Program
{
    private const int SPIKE_DAMAGE = 4;

    private static void CheckStepTriggers()
    {
        CheckThrows<ArgumentOutOfRangeException>(() => new DamageEffect(0), "A damage effect must be positive.");
        CheckThrows<ArgumentNullException>(() => new StepTriggerPart(null!), "A step trigger needs an effect.");

        ThingType spikes = new ThingType("Spikes", new StepTriggerPart(new DamageEffect(SPIKE_DAMAGE)));

        Map map = new Map(MAP_WIDTH, MAP_HEIGHT);
        World world = new World(map);
        Actor player = map.AddEntity(TestActors.Create(1, 0, 5, 10));
        Thing trap = map.AddEntity(new Thing(101, spikes, 1, 5));
        StepRecorder recorder = new StepRecorder();
        world.Events.Register<StepTriggered, ActorDamaged, ActorDied>(recorder);
        GameRunner runner = new GameRunner(world, player);

        CheckEqual(true, runner.SubmitPlayerAction(MoveAction.RIGHT).IsExecuted, "A step trigger does not block movement.");
        CheckEqual(1, player.X, "The actor stands on the step trigger.");
        CheckEqual(TestActors.MAX_HP - SPIKE_DAMAGE, player.Hp, "Stepping on spikes deals their damage.");
        CheckEqual("step 1 Spikes,damage 1 4", string.Join(",", recorder.Log), "Stepping raises the trigger event, then the effect's event.");
        Check(map.Contains(trap), "A step trigger stays in the map after it fires.");
        CheckEqual(false, runner.SubmitPlayerAction(PickUpAction.DEFAULT).IsExecuted, "A step trigger without an item part cannot be picked up.");

        runner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(2, recorder.Log.Count, "Standing on a step trigger does not fire it again.");
        runner.SubmitPlayerAction(MoveAction.LEFT);
        CheckEqual(2, recorder.Log.Count, "Leaving a step trigger does not fire it.");
        runner.SubmitPlayerAction(MoveAction.RIGHT);
        CheckEqual(TestActors.MAX_HP - SPIKE_DAMAGE * 2, player.Hp, "Stepping on spikes again deals damage again.");
        CheckEqual(4, recorder.Log.Count, "Stepping again raises the events again.");

        Map monsterMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        World monsterWorld = new World(monsterMap);
        Actor watcher = monsterMap.AddEntity(TestActors.Create(1, 0, 0, 10));
        Actor walker = monsterMap.AddEntity(TestActors.Create(2, 3, 5, 10, new FixedController(MoveAction.RIGHT)));
        monsterMap.AddEntity(new Thing(101, spikes, 4, 5));
        GameRunner monsterRunner = new GameRunner(monsterWorld, watcher);
        monsterRunner.SubmitPlayerAction(WaitAction.DEFAULT);
        CheckEqual(4, walker.X, "The NPC walked onto the spikes.");
        CheckEqual(TestActors.MAX_HP - SPIKE_DAMAGE, walker.Hp, "An NPC is hurt by spikes too.");

        ThingType deadly = new ThingType("Deadly", new StepTriggerPart(new DamageEffect(TestActors.MAX_HP)));
        Map deathMap = new Map(MAP_WIDTH, MAP_HEIGHT);
        World deathWorld = new World(deathMap);
        Actor victim = deathMap.AddEntity(TestActors.Create(1, 0, 5, 10));
        deathMap.AddEntity(new Thing(101, spikes, 1, 5));
        deathMap.AddEntity(new Thing(102, deadly, 1, 5));
        deathMap.AddEntity(new Thing(103, spikes, 1, 5));
        StepRecorder deathRecorder = new StepRecorder();
        deathWorld.Events.Register<StepTriggered, ActorDamaged, ActorDied>(deathRecorder);
        GameRunner deathRunner = new GameRunner(deathWorld, victim);
        deathRunner.SubmitPlayerAction(MoveAction.RIGHT);
        CheckEqual(false, victim.IsAlive, "A lethal step trigger kills.");
        CheckEqual("step 1 Spikes,damage 1 4,step 1 Deadly,damage 1 10,died 1", string.Join(",", deathRecorder.Log),
            "Triggers fire in order, death is reported once, and later triggers do not fire on the dead.");
        Check(deathRunner.IsGameOver, "Dying on a step trigger ends the game.");
    }
}

internal sealed class StepRecorder : IEventListener<StepTriggered>, IEventListener<ActorDamaged>, IEventListener<ActorDied>
{
    public List<string> Log { get; } = new List<string>();

    public void OnEvent(StepTriggered message)
    {
        Log.Add($"step {message.Actor.ID} {message.Thing.Type.Name}");
    }

    public void OnEvent(ActorDamaged message)
    {
        Log.Add($"damage {message.Actor.ID} {message.Damage}");
    }

    public void OnEvent(ActorDied message)
    {
        Log.Add($"died {message.Actor.ID}");
    }
}
