using System;
using System.Numerics;
using Cysharp.Text;
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
    private readonly PlayerTravel _travel = new PlayerTravel();
    private readonly ContextMenu _menu = new ContextMenu();

    private readonly InteractionPlan _plan = new InteractionPlan();
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
        _session.World.Events.Register<ActorAttacked, ActorDied, ItemPickedUp, TileHarvested>(_log);
        _log.Add("마을의 조용한 하루. 동쪽 들판에서 무언가 꿈틀거린다.", MessageLog.MUTED);
    }

    public void Execute(EPlayerCommand command)
    {
        if (command == EPlayerCommand.None)
        {
            return;
        }

        _travel.Cancel();
        _menu.Close();
        ExecuteCommand(command);
    }

    public void ClickTile(Vector2 screenPosition)
    {
        if (_menu.IsOpen)
        {
            if (_menu.TryGetEntryAt(screenPosition, out Interaction? interactionOrNull))
            {
                StartMenuEntry(interactionOrNull);
            }

            _menu.Close();
            return;
        }

        if (_session.Runner.IsGameOver || !_view.TryGetTileAtScreen(screenPosition, out int tileX, out int tileY))
        {
            return;
        }

        if (_travel.IsPreviewGoal(tileX, tileY))
        {
            _travel.Confirm(_session);
            return;
        }

        _travel.Cancel();
        _travel.Preview(_session, tileX, tileY, GetAdjacentInteractionOrNull(tileX, tileY));
    }

    public void OpenContextMenu(Vector2 screenPosition)
    {
        _menu.Close();
        if (_session.Runner.IsGameOver || !_view.TryGetTileAtScreen(screenPosition, out int tileX, out int tileY))
        {
            return;
        }

        _travel.Cancel();
        AddMenuEntries(tileX, tileY);
        _menu.Open(_font, screenPosition, tileX, tileY, _screenWidth, _screenHeight);
    }

    private ActionResult ExecuteCommand(EPlayerCommand command)
    {
        if (command == EPlayerCommand.None || _session.Runner.IsGameOver)
        {
            return ActionResult.REJECTED;
        }

        _debugger.RecordCommand(command);

        ActionResult result;
        switch (command)
        {
            case EPlayerCommand.MoveUp:
                result = MoveOrInteract(MoveAction.UP);
                break;
            case EPlayerCommand.MoveDown:
                result = MoveOrInteract(MoveAction.DOWN);
                break;
            case EPlayerCommand.MoveLeft:
                result = MoveOrInteract(MoveAction.LEFT);
                break;
            case EPlayerCommand.MoveRight:
                result = MoveOrInteract(MoveAction.RIGHT);
                break;
            case EPlayerCommand.PickUp:
                result = _session.Runner.SubmitPlayerAction(PickUpAction.DEFAULT);
                if (!result.IsExecuted)
                {
                    _log.Add("여기에는 주울 것이 없다.", MessageLog.MUTED);
                }
                break;
            case EPlayerCommand.Wait:
                result = _session.Runner.SubmitPlayerAction(WaitAction.DEFAULT);
                break;
            default:
                return ActionResult.REJECTED;
        }

        if (result.IsExecuted)
        {
            _view.NotifyTurnEnded();
        }

        return result;
    }

    public void Update(float deltaTime)
    {
        UpdateTravel(deltaTime);
        _view.Update(deltaTime);
        _log.Update(deltaTime);
        _debugger.Update();
    }

    public void Draw()
    {
        _view.TravelPathOrNull = _travel.IsActive || _travel.IsPreviewing ? _travel.Path : null;
        _view.MarkedTileOrNull = _menu.IsOpen ? (_menu.TileX, _menu.TileY) : null;
        _view.Draw();
        _log.Draw(_font, 20, _screenHeight - 12);
        Hud.Draw(_atlas, _font, _session, GetPromptOrNull(), _screenWidth, _screenHeight);
        _menu.Draw(_font);
        _debugger.Draw();
    }

    private void AddMenuEntries(int tileX, int tileY)
    {
        Map map = _session.Map;
        Actor player = _session.Player;
        _plan.Collect(map, player, tileX, tileY);
        for (int index = 0; index < _plan.Items.Count; index++)
        {
            Interaction interaction = _plan.Items[index];
            _menu.AddEntry(GameText.GetInteractionLabel(interaction, map), interaction);
        }

        bool bPlayerTile = tileX == player.X && tileY == player.Y;
        if (!bPlayerTile)
        {
            _menu.AddEntry("여기로 걸어가기", null);
        }
    }

    private void StartMenuEntry(Interaction? interactionOrNull)
    {
        if (!_travel.Start(_session, _menu.TileX, _menu.TileY, interactionOrNull))
        {
            _log.Add("그곳에는 갈 수 없다.", MessageLog.MUTED);
        }
    }

    private Interaction? GetAdjacentInteractionOrNull(int tileX, int tileY)
    {
        _plan.Collect(_session.Map, _session.Player, tileX, tileY);
        if (_plan.TryGetFirst(1, out Interaction interaction))
        {
            return interaction;
        }

        return null;
    }

    private void UpdateTravel(float deltaTime)
    {
        ETravelStep step = _travel.NextStep(_session, deltaTime, out EPlayerCommand moveCommand);
        if (step == ETravelStep.None)
        {
            return;
        }

        if (step == ETravelStep.Interact)
        {
            Interaction interaction = _travel.Interaction;
            _travel.Cancel();
            PerformInteraction(interaction);
            return;
        }

        ActionResult result = ExecuteCommand(moveCommand);
        if (!result.IsExecuted)
        {
            _travel.Cancel();
            return;
        }

        string? reasonOrNull = _travel.CheckInterruptOrNull(_session);
        if (reasonOrNull != null)
        {
            _log.Add(reasonOrNull, MessageLog.DANGER);
        }
    }

    private void PerformInteraction(Interaction interaction)
    {
        EPlayerCommand command = GetKeyboardCommand(interaction);
        if (command != EPlayerCommand.None)
        {
            ExecuteCommand(command);
            return;
        }

        ActionResult result = _session.Runner.SubmitPlayerAction(interaction.Action);
        if (result.IsExecuted)
        {
            _view.NotifyTurnEnded();
        }
    }

    private EPlayerCommand GetKeyboardCommand(Interaction interaction)
    {
        Actor player = _session.Player;
        if (interaction.Action is PickUpAction)
        {
            return EPlayerCommand.PickUp;
        }

        if (interaction.Reach != 1)
        {
            return EPlayerCommand.None;
        }

        Interaction? bumpOrNull = GetAdjacentInteractionOrNull(interaction.TargetX, interaction.TargetY);
        if (bumpOrNull == null || bumpOrNull.Value.Action.GetType() != interaction.Action.GetType())
        {
            return EPlayerCommand.None;
        }

        int deltaX = interaction.TargetX - player.X;
        int deltaY = interaction.TargetY - player.Y;
        if (deltaX != 0)
        {
            return deltaX < 0 ? EPlayerCommand.MoveLeft : EPlayerCommand.MoveRight;
        }

        return deltaY < 0 ? EPlayerCommand.MoveUp : EPlayerCommand.MoveDown;
    }

    private ActionResult MoveOrInteract(MoveAction move)
    {
        Actor player = _session.Player;
        Interaction? interactionOrNull = GetAdjacentInteractionOrNull(player.X + move.DeltaX, player.Y + move.DeltaY);
        if (interactionOrNull != null)
        {
            return _session.Runner.SubmitPlayerAction(interactionOrNull.Value.Action);
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
            return ZString.Concat("E  ", GameText.GetPickUpLabel(itemOrNull.Type));
        }

        return null;
    }
}
