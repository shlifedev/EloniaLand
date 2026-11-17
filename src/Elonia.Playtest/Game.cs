using System;
using Elonia.Simulation;

namespace Elonia.Playtest;

internal sealed class Game
{
    private readonly SpriteAtlas _atlas;
    private readonly UiFont _font;
    private readonly GameSession _session;
    private readonly GameView _view;
    private readonly MessageLog _log;
    private readonly Debugger _debugger;
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    public bool IsDebuggerOpen => _debugger.IsOpen;

    public Game(SpriteAtlas atlas, UiFont font, int seed, int screenWidth, int screenHeight, bool bDebuggerOpen)
    {
        _atlas = atlas;
        _font = font;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _session = TownMap.Create(new Random(seed));
        _view = new GameView(atlas, font, _session, screenWidth, screenHeight);
        _debugger = new Debugger(_session, _view, seed, screenWidth, screenHeight, bDebuggerOpen);
        _log = new MessageLog(_session.Player);
        _session.World.Events.Register<ActorAttacked, ActorDied, ItemPickedUp>(_log);
        _log.Add("A quiet day in the village. Something stirs in the east meadow.", MessageLog.MUTED);
    }

    public void Execute(EPlayerCommand command)
    {
        if (command == EPlayerCommand.None || _session.Runner.IsGameOver)
        {
            return;
        }

        _debugger.RecordCommand(command);

        ActionResult result;
        switch (command)
        {
            case EPlayerCommand.MoveUp:
                result = MoveOrAttack(MoveAction.UP);
                break;
            case EPlayerCommand.MoveDown:
                result = MoveOrAttack(MoveAction.DOWN);
                break;
            case EPlayerCommand.MoveLeft:
                result = MoveOrAttack(MoveAction.LEFT);
                break;
            case EPlayerCommand.MoveRight:
                result = MoveOrAttack(MoveAction.RIGHT);
                break;
            case EPlayerCommand.PickUp:
                result = _session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT);
                if (!result.IsExecuted)
                {
                    _log.Add("There is nothing here to pick up.", MessageLog.MUTED);
                }
                break;
            case EPlayerCommand.Wait:
                result = _session.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
                break;
            default:
                return;
        }

        if (result.IsExecuted)
        {
            _view.NotifyTurnEnded();
        }
    }

    public void Update(float deltaTime)
    {
        _view.Update(deltaTime);
        _log.Update(deltaTime);
        _debugger.Update();
    }

    public void Draw()
    {
        _view.Draw();
        _log.Draw(_font, 20, _screenHeight - 12);
        Hud.Draw(_atlas, _font, _session, GetPromptOrNull(), _screenWidth, _screenHeight);
        _debugger.Draw();
    }

    private ActionResult MoveOrAttack(MoveAction move)
    {
        Actor player = _session.Player;
        Actor? blockerOrNull = _session.Map.GetLivingActorAtOrNull(player.X + move.DeltaX, player.Y + move.DeltaY);
        if (blockerOrNull != null && GameContent.IsAttackable(blockerOrNull))
        {
            return _session.Runner.SubmitPlayerAction(new AttackAction(blockerOrNull));
        }

        ActionResult result = _session.Runner.SubmitPlayerAction(move);
        if (!result.IsExecuted)
        {
            _view.ShowBump(player, move.DeltaX, move.DeltaY);
        }

        return result;
    }

    private string? GetPromptOrNull()
    {
        Actor player = _session.Player;
        Item? itemOrNull = _session.Map.GetItemAtOrNull(player.X, player.Y);
        if (itemOrNull != null)
        {
            return itemOrNull.Type == EItemType.Potion ? "E  Drink the potion" : "E  Pick up the gold";
        }

        return null;
    }
}
