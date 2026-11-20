using System.Collections.Generic;
using Elonia.Events;
using Elonia.Simulation;

namespace Elonia.Checks;

internal static class TestActors
{
    public static Actor Create(int id, int x, int y, int speed, IActorController? controllerOrNull = null)
    {
        return new Actor(id, new TestActorDef(speed), x, y, controllerOrNull);
    }
}

internal sealed class TestActorDef : IActorDef
{
    public string Id => "Test";
    public int MaxHp => 10;
    public int Speed { get; }

    public TestActorDef(int speed)
    {
        Speed = speed;
    }
}

internal sealed class TestFloorDef : IFloorDef
{
    public string Id { get; }
    public bool IsWalkable { get; }

    public TestFloorDef(string id, bool bWalkable)
    {
        Id = id;
        IsWalkable = bWalkable;
    }
}

internal sealed class TestPropDef : IPropDef
{
    public string Id { get; }
    public bool IsWalkable { get; }

    public TestPropDef(string id, bool bWalkable)
    {
        Id = id;
        IsWalkable = bWalkable;
    }
}

internal sealed class TestThingDef : IThingDef
{
    public string Id { get; }
    public bool IsItem { get; }

    public TestThingDef(string id, bool bItem = false)
    {
        Id = id;
        IsItem = bItem;
    }
}

internal static class TestItems
{
    public static readonly TestThingDef POTION = new TestThingDef("Potion", true);
    public static readonly TestThingDef GOLD = new TestThingDef("Gold", true);
}

internal sealed class TestMoveAction : ActorAction
{
    public static readonly TestMoveAction RIGHT = new TestMoveAction(1, 0);
    public static readonly TestMoveAction LEFT = new TestMoveAction(-1, 0);
    public static readonly TestMoveAction UP = new TestMoveAction(0, -1);
    public static readonly TestMoveAction DOWN = new TestMoveAction(0, 1);

    public override string Name => "Test move";
    public int DeltaX { get; }
    private readonly int _deltaY;

    private TestMoveAction(int deltaX, int deltaY)
    {
        DeltaX = deltaX;
        _deltaY = deltaY;
    }

    public override ActionResult Execute(ActionContext context)
    {
        int x = context.Actor.X + DeltaX;
        int y = context.Actor.Y + _deltaY;
        if (!context.Map.CanEnter(x, y))
        {
            return ActionResult.REJECTED;
        }

        context.Map.MoveActor(context.Actor, x, y);
        return ActionResult.Executed(DEFAULT_COST);
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

internal sealed class PacingController : IActorController
{
    private TestMoveAction _direction = TestMoveAction.RIGHT;

    public ActorAction ChooseAction(ActionContext context)
    {
        Actor actor = context.Actor;
        if (!context.Map.CanEnter(actor.X + _direction.DeltaX, actor.Y))
        {
            _direction = _direction == TestMoveAction.RIGHT ? TestMoveAction.LEFT : TestMoveAction.RIGHT;
        }

        return _direction;
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
