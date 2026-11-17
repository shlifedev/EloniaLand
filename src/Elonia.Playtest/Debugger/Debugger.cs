using System.Collections.Generic;
using System.Numerics;
using Elonia.Simulation;
using ImGuiNET;
using Raylib_cs;

namespace Elonia.Playtest;

internal sealed partial class Debugger
{
    private const float WINDOW_WIDTH = 420.0f;
    private const float WINDOW_MARGIN = 8.0f;
    private const int MAX_HISTORY_ROW_COUNT = 120;
    private const string REPRO_FILE_NAME = "repro.txt";

    private static readonly Vector4 MUTED_TEXT = new Vector4(0.62f, 0.64f, 0.70f, 1.0f);
    private static readonly Vector4 GOOD_TEXT = new Vector4(0.55f, 0.90f, 0.60f, 1.0f);
    private static readonly Vector4 PLAYER_TEXT = new Vector4(0.55f, 0.80f, 1.00f, 1.0f);

    private readonly GameSession _session;
    private readonly GameView _view;
    private readonly int _seed;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private readonly TurnHistory _history;
    private readonly List<char> _commandSymbols = new List<char>();
    private readonly List<ForecastRow> _forecastRows = new List<ForecastRow>();

    private Actor? _selectedActorOrNull;
    private string? _commandTextOrNull;
    private string? _reproStatusOrNull;

    private bool _isOpen;
    private bool _isFollowingNearest = true;
    private bool _isBehaviorLabelVisible = true;
    private bool _isEnergyBarVisible = true;
    private bool _isSenseRangeVisible = true;
    private bool _isEverySenseRangeVisible;
    private bool _isTargetLineVisible = true;
    private bool _isTileInfoVisible = true;
    private bool _isHistoryFilteredBySelection;

    public bool IsOpen => _isOpen;

    public Debugger(GameSession session, GameView view, int seed, int screenWidth, int screenHeight, bool bOpen)
    {
        _session = session;
        _view = view;
        _seed = seed;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        _isOpen = bOpen;
        _history = new TurnHistory(session.Player);
        session.World.Events.Register<ActionExecuted>(_history);
    }

    public void RecordCommand(EPlayerCommand command)
    {
        char? symbolOrNull = DemoScript.GetSymbolOrNull(command);
        if (symbolOrNull != null)
        {
            _commandSymbols.Add(symbolOrNull.Value);
            _commandTextOrNull = null;
        }
    }

    public void Update()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.F1))
        {
            _isOpen = !_isOpen;
        }

        if (!_isOpen)
        {
            return;
        }

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && !ImGui.GetIO().WantCaptureMouse)
        {
            SelectActorAtMouse();
        }

        if (_isFollowingNearest)
        {
            _selectedActorOrNull = FindNearestNpcOrNull();
        }
    }

    public void Draw()
    {
        if (!_isOpen)
        {
            return;
        }

        DrawOverlay();

        ImGui.SetNextWindowPos(new Vector2(_screenWidth - WINDOW_WIDTH - WINDOW_MARGIN, WINDOW_MARGIN), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(WINDOW_WIDTH, _screenHeight - WINDOW_MARGIN * 2.0f), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("디버거 (F1)", ref _isOpen))
        {
            DrawTabs();
        }

        ImGui.End();
    }

    private void DrawTabs()
    {
        if (!ImGui.BeginTabBar("tabs"))
        {
            return;
        }

        if (ImGui.BeginTabItem("시간"))
        {
            DrawTimeTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("액터"))
        {
            DrawActorTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("기록"))
        {
            DrawHistoryTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("재현"))
        {
            DrawReproTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("표시"))
        {
            DrawDisplayTab();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void SelectActorAtMouse()
    {
        if (!_view.TryGetTileAtScreen(Raylib.GetMousePosition(), out int tileX, out int tileY))
        {
            return;
        }

        Actor? firstOrNull = null;
        Actor? nextOrNull = null;
        bool bPassedSelected = false;
        IReadOnlyList<Actor> actors = _session.Map.Actors;
        for (int index = 0; index < actors.Count; index++)
        {
            Actor actor = actors[index];
            if (!actor.IsAlive || actor.X != tileX || actor.Y != tileY)
            {
                continue;
            }

            if (firstOrNull == null)
            {
                firstOrNull = actor;
            }

            if (bPassedSelected && nextOrNull == null)
            {
                nextOrNull = actor;
            }

            if (actor == _selectedActorOrNull)
            {
                bPassedSelected = true;
            }
        }

        Actor? pickedOrNull = nextOrNull ?? firstOrNull;
        if (pickedOrNull != null)
        {
            _selectedActorOrNull = pickedOrNull;
            _isFollowingNearest = false;
        }
    }

    private Actor? FindNearestNpcOrNull()
    {
        Actor player = _session.Player;
        Actor? nearestOrNull = null;
        long nearestDistance = long.MaxValue;
        IReadOnlyList<Actor> actors = _session.Map.Actors;
        for (int index = 0; index < actors.Count; index++)
        {
            Actor actor = actors[index];
            if (actor == player || !actor.IsAlive)
            {
                continue;
            }

            long deltaX = actor.X - player.X;
            long deltaY = actor.Y - player.Y;
            long distance = deltaX * deltaX + deltaY * deltaY;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestOrNull = actor;
            }
        }

        return nearestOrNull;
    }

    private void DrawSelectableActor(Actor actor)
    {
        bool bColored = actor == _session.Player || !actor.IsAlive;
        if (bColored)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, actor == _session.Player ? PLAYER_TEXT : MUTED_TEXT);
        }

        ImGui.PushID(actor.ID);
        if (ImGui.Selectable(DebugNames.GetActorName(actor), actor == _selectedActorOrNull, ImGuiSelectableFlags.SpanAllColumns))
        {
            _selectedActorOrNull = actor;
            _isFollowingNearest = false;
        }

        ImGui.PopID();
        if (bColored)
        {
            ImGui.PopStyleColor();
        }
    }

    private static string GetCurrentBehaviorName(Actor actor)
    {
        if (!(actor.ControllerOrNull is Brain brain))
        {
            return "-";
        }

        return brain.CurrentBehaviorOrNull == null ? "없음" : DebugNames.GetBehaviorName(brain.CurrentBehaviorOrNull);
    }

    private static int IndexOf(IReadOnlyList<IBehavior> behaviors, IBehavior behavior)
    {
        for (int index = 0; index < behaviors.Count; index++)
        {
            if (behaviors[index] == behavior)
            {
                return index;
            }
        }

        return -1;
    }

    private static void DrawLabelValue(string label, string value)
    {
        DrawMuted(label);
        ImGui.SameLine(110.0f);
        ImGui.TextUnformatted(value);
    }

    private static void DrawRow(string label, string value)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        DrawMuted(label);
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(value);
    }

    private static void DrawBarRow(string label, float fraction, string overlay)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        DrawMuted(label);
        ImGui.TableNextColumn();
        ImGui.ProgressBar(fraction, new Vector2(-1.0f, 0.0f), overlay);
    }

    private static void DrawMuted(string text)
    {
        DrawColored(text, MUTED_TEXT);
    }

    private static void DrawColored(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
        ImGui.PopStyleColor();
    }

    private static void DrawWrapped(string text)
    {
        ImGui.PushTextWrapPos(0.0f);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }

    private static void DrawWrappedMuted(string text)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, MUTED_TEXT);
        DrawWrapped(text);
        ImGui.PopStyleColor();
    }

    private readonly struct ForecastRow
    {
        public Actor Actor { get; }

        public int Order { get; }
        public long TicksUntilReady { get; }

        public ForecastRow(Actor actor, int order, long ticksUntilReady)
        {
            Actor = actor;
            Order = order;
            TicksUntilReady = ticksUntilReady;
        }
    }
}
