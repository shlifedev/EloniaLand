using System.Collections.Generic;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Checks;

internal static class TestActors
{
    public const int MAX_HP = 10;
    public const int ATTACK_POWER = 3;

    public static Actor Create(int id, int x, int y, int speed, IActorController? controllerOrNull = null)
    {
        ActorType type = new ActorType("Test", MAX_HP, ATTACK_POWER, speed);
        return new Actor(id, type, x, y, controllerOrNull);
    }
}

internal sealed class EventRecorder : IEventListener<ActorAttacked>, IEventListener<ActorDied>, IEventListener<ItemPickedUp>
{
    public List<string> Log { get; } = new List<string>();

    public void OnEvent(ActorAttacked message)
    {
        Log.Add($"attack {message.Attacker.ID}>{message.Target.ID} {message.Damage}");
    }

    public void OnEvent(ActorDied message)
    {
        Log.Add($"died {message.Actor.ID}");
    }

    public void OnEvent(ItemPickedUp message)
    {
        Log.Add($"pickup {message.Actor.ID} {message.Item.Type} {message.Item.Amount}");
    }
}

internal sealed class ActionRecorder : IEventListener<ActionExecuted>
{
    public List<string> Log { get; } = new List<string>();

    public void OnEvent(ActionExecuted message)
    {
        Log.Add($"{message.Actor.ID} {message.Action.Name} {message.Cost} @{message.Tick}");
    }
}

internal sealed class CountingActionListener : IEventListener<ActionExecuted>
{
    public int Count { get; private set; }

    public void OnEvent(ActionExecuted message)
    {
        Count++;
    }
}

internal sealed class CostAction : ActorAction
{
    private readonly int _cost;
    private readonly List<int>? _executionOrderOrNull;

    public override string Name => "Test cost";

    public CostAction(int cost, List<int>? executionOrderOrNull = null)
    {
        _cost = cost;
        _executionOrderOrNull = executionOrderOrNull;
    }

    public override ActionResult Execute(ActionContext context)
    {
        _executionOrderOrNull?.Add(context.Actor.ID);
        return ActionResult.Executed(_cost);
    }
}

internal sealed class RejectAction : ActorAction
{
    public override string Name => "Test rejection";

    public override ActionResult Execute(ActionContext context)
    {
        return ActionResult.REJECTED;
    }
}

internal sealed class SwitchBehavior : IBehavior
{
    public string Name { get; }
    public bool IsEnabled { get; set; }
    public int ChooseCount { get; private set; }

    public SwitchBehavior(string name, bool bEnabled)
    {
        Name = name;
        IsEnabled = bEnabled;
    }

    public bool CanRun(ActionContext context)
    {
        return IsEnabled;
    }

    public ActorAction ChooseAction(ActionContext context)
    {
        ChooseCount++;
        return WaitAction.DEFAULT;
    }
}

internal sealed class FixedController : IActorController
{
    private readonly ActorAction _action;

    public int ChooseCount { get; private set; }

    public FixedController(ActorAction action)
    {
        _action = action;
    }

    public ActorAction ChooseAction(ActionContext context)
    {
        ChooseCount++;
        return _action;
    }
}
