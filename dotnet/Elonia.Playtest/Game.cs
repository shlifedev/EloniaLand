using System.Collections.Generic;
using System.Numerics;
using Elonia.Game;
using Elonia.Simulation;

namespace Elonia.Playtest;

internal sealed class Game
{
    private const float TRAVEL_STEP_INTERVAL = 0.12f;

    private readonly SpriteAtlas _atlas;
    private readonly UiFont _font;
    private readonly PlaySession _play;
    private readonly GameView _view;
    private readonly MessageLog _log = new MessageLog();
    private readonly Debugger _debugger;
    private readonly ContextMenu _menu = new ContextMenu();

    private readonly List<MenuChoice> _menuChoices = new List<MenuChoice>();
    private readonly int _screenWidth;
    private readonly int _screenHeight;

    private float _travelTimer;

    public bool IsDebuggerOpen => _debugger.IsOpen;

    public Game(GameContent content, SpriteAtlas atlas, UiFont font, int seed, int screenWidth, int screenHeight, bool bDebuggerOpen)
    {
        _atlas = atlas;
        _font = font;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        _play = PlaySession.CreateTown(content, seed);
        GameSession session = _play.Session;
        session.World.Events.Register<MessageLogged>(_log);
        _view = new GameView(atlas, font, session, HouseSprites.Create(session, content), screenWidth, screenHeight);
        _debugger = new Debugger(_play, _view, seed, screenWidth, screenHeight, bDebuggerOpen);

        _play.AnnounceIntro();
    }

    public void Execute(EPlayerCommand command)
    {
        if (command == EPlayerCommand.None)
        {
            return;
        }

        _menu.Close();
        _play.Execute(command);
    }

    public void ClickTile(Vector2 screenPosition)
    {
        if (_menu.IsOpen)
        {
            if (_menu.TryGetEntryAt(screenPosition, out Interaction? interactionOrNull))
            {
                _play.StartMenuChoice(_menu.TileX, _menu.TileY, interactionOrNull);
            }

            _menu.Close();
            return;
        }

        if (_view.TryGetTileAtScreen(screenPosition, out int tileX, out int tileY))
        {
            _play.SelectTile(tileX, tileY);
        }
    }

    public void OpenContextMenu(Vector2 screenPosition)
    {
        _menu.Close();
        if (_play.IsGameOver || !_view.TryGetTileAtScreen(screenPosition, out int tileX, out int tileY))
        {
            return;
        }

        _play.CancelTravel();
        _play.CollectMenuChoices(tileX, tileY, _menuChoices);
        for (int index = 0; index < _menuChoices.Count; index++)
        {
            _menu.AddEntry(_menuChoices[index].Label, _menuChoices[index].InteractionOrNull);
        }

        _menu.Open(_font, screenPosition, tileX, tileY, _screenWidth, _screenHeight);
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
        _view.TravelPathOrNull = _play.TravelPathOrNull;
        _view.MarkedTileOrNull = _menu.IsOpen ? (_menu.TileX, _menu.TileY) : null;
        _view.Draw();
        _log.Draw(_font, 20, _screenHeight - 12);
        Hud.Draw(_atlas, _font, _play.Session, _play.GetPromptOrNull(), _screenWidth, _screenHeight);
        _menu.Draw(_font);
        _debugger.Draw();
    }

    private void UpdateTravel(float deltaTime)
    {
        if (!_play.IsTraveling)
        {
            _travelTimer = 0.0f;
            return;
        }

        _travelTimer -= deltaTime;
        if (_travelTimer > 0.0f)
        {
            return;
        }

        _travelTimer = TRAVEL_STEP_INTERVAL;
        _play.StepTravel();
    }
}
