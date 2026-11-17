using System;
using System.Collections.Generic;
using System.Numerics;
using Cysharp.Text;
using Elonia.Simulation;
using ImGuiNET;

namespace Elonia.Playtest;

internal sealed partial class Debugger
{
    private const float ENERGY_BAR_WIDTH = 36.0f;
    private const float ENERGY_BAR_HEIGHT = 4.0f;
    private const float LABEL_PADDING = 2.0f;
    private const int RANGE_ALPHA = 80;

    private static readonly uint ATTACK_COLOR = MakeColor(255, 96, 96, 255);
    private static readonly uint CHASE_COLOR = MakeColor(255, 160, 64, 255);
    private static readonly uint FLEE_COLOR = MakeColor(255, 224, 96, 255);
    private static readonly uint WANDER_COLOR = MakeColor(200, 204, 214, 255);
    private static readonly uint LABEL_BACK_COLOR = MakeColor(24, 20, 37, 200);
    private static readonly uint ENERGY_BACK_COLOR = MakeColor(24, 20, 37, 220);
    private static readonly uint ENERGY_FILL_COLOR = MakeColor(120, 140, 220, 255);
    private static readonly uint ENERGY_READY_COLOR = MakeColor(110, 230, 255, 255);
    private static readonly uint SELECTION_COLOR = MakeColor(255, 255, 255, 255);
    private static readonly uint TARGET_LINE_COLOR = MakeColor(255, 255, 255, 180);
    private static readonly uint HOVER_COLOR = MakeColor(255, 255, 255, 90);

    private void DrawOverlay()
    {
        ImDrawListPtr drawList = ImGui.GetBackgroundDrawList();
        IReadOnlyList<Actor> actors = _session.Map.Actors;

        for (int index = 0; index < actors.Count; index++)
        {
            Actor actor = actors[index];
            bool bShowsRange = _isEverySenseRangeVisible || (_isSenseRangeVisible && actor == _selectedActorOrNull);
            if (bShowsRange && actor.IsAlive && actor.ControllerOrNull is Brain brain)
            {
                DrawSenseRanges(drawList, actor, brain);
            }
        }

        if (_selectedActorOrNull != null)
        {
            DrawSelection(drawList, _selectedActorOrNull);
        }

        for (int index = 0; index < actors.Count; index++)
        {
            Actor actor = actors[index];
            if (!actor.IsAlive)
            {
                continue;
            }

            if (_isEnergyBarVisible)
            {
                DrawEnergyBar(drawList, actor);
            }

            if (_isBehaviorLabelVisible && actor.ControllerOrNull is Brain brain && brain.CurrentBehaviorOrNull != null)
            {
                DrawBehaviorLabel(drawList, actor, brain.CurrentBehaviorOrNull);
            }
        }

        if (_isTileInfoVisible && !ImGui.GetIO().WantCaptureMouse)
        {
            DrawHoveredTileInfo(drawList);
        }
    }

    private void DrawSenseRanges(ImDrawListPtr drawList, Actor actor, Brain brain)
    {
        for (int index = 0; index < brain.Behaviors.Count; index++)
        {
            IBehavior behavior = brain.Behaviors[index];
            if (behavior is FleeBehavior flee)
            {
                DrawRange(drawList, actor, flee.SenseRange, WithAlpha(FLEE_COLOR, RANGE_ALPHA));
            }
            else if (behavior is ChaseBehavior chase)
            {
                DrawRange(drawList, actor, chase.SenseRange, WithAlpha(CHASE_COLOR, RANGE_ALPHA));
            }
        }
    }

    private void DrawRange(ImDrawListPtr drawList, Actor actor, int range, uint color)
    {
        long squaredRange = (long)range * range;
        Map map = _session.Map;
        for (int y = actor.Y - range; y <= actor.Y + range; y++)
        {
            for (int x = actor.X - range; x <= actor.X + range; x++)
            {
                long deltaX = x - actor.X;
                long deltaY = y - actor.Y;
                if (map.IsInside(x, y) && deltaX * deltaX + deltaY * deltaY <= squaredRange)
                {
                    Vector2 topLeft = _view.GetTileScreenPosition(x, y);
                    drawList.AddRectFilled(topLeft, topLeft + new Vector2(GameView.TILE_SIZE, GameView.TILE_SIZE), color);
                }
            }
        }
    }

    private void DrawSelection(ImDrawListPtr drawList, Actor actor)
    {
        Vector2 topLeft = _view.GetActorScreenPosition(actor);
        Vector2 size = new Vector2(GameView.TILE_SIZE, GameView.TILE_SIZE);
        drawList.AddRect(topLeft, topLeft + size, SELECTION_COLOR, 0.0f, ImDrawFlags.None, 2.0f);

        if (!_isTargetLineVisible || !actor.IsAlive || !(actor.ControllerOrNull is Brain brain))
        {
            return;
        }

        Actor? lastTargetOrNull = null;
        for (int index = 0; index < brain.Behaviors.Count; index++)
        {
            Actor? targetOrNull = GetBehaviorTargetOrNull(brain.Behaviors[index]);
            if (targetOrNull == null || targetOrNull == lastTargetOrNull || !targetOrNull.IsAlive)
            {
                continue;
            }

            Vector2 center = size * 0.5f;
            drawList.AddLine(topLeft + center, _view.GetActorScreenPosition(targetOrNull) + center, TARGET_LINE_COLOR, 2.0f);
            lastTargetOrNull = targetOrNull;
        }
    }

    private void DrawEnergyBar(ImDrawListPtr drawList, Actor actor)
    {
        Vector2 topLeft = _view.GetActorScreenPosition(actor);
        Vector2 barTopLeft = topLeft + new Vector2((GameView.TILE_SIZE - ENERGY_BAR_WIDTH) * 0.5f, GameView.TILE_SIZE - 2.0f);
        float fraction = Math.Min(1.0f, (float)actor.Energy / GameRunner.READY_ENERGY);
        uint fillColor = actor.Energy >= GameRunner.READY_ENERGY ? ENERGY_READY_COLOR : ENERGY_FILL_COLOR;
        drawList.AddRectFilled(barTopLeft, barTopLeft + new Vector2(ENERGY_BAR_WIDTH, ENERGY_BAR_HEIGHT), ENERGY_BACK_COLOR);
        drawList.AddRectFilled(barTopLeft, barTopLeft + new Vector2(ENERGY_BAR_WIDTH * fraction, ENERGY_BAR_HEIGHT), fillColor);
    }

    private void DrawBehaviorLabel(ImDrawListPtr drawList, Actor actor, IBehavior behavior)
    {
        string text = DebugNames.GetBehaviorName(behavior);
        Vector2 textSize = ImGui.CalcTextSize(text);
        Vector2 topLeft = _view.GetActorScreenPosition(actor);
        Vector2 textTopLeft = topLeft + new Vector2((GameView.TILE_SIZE - textSize.X) * 0.5f, -textSize.Y - 10.0f);
        Vector2 padding = new Vector2(LABEL_PADDING, LABEL_PADDING);
        drawList.AddRectFilled(textTopLeft - padding, textTopLeft + textSize + padding, LABEL_BACK_COLOR);
        drawList.AddText(textTopLeft, GetBehaviorColor(behavior), text);
    }

    private void DrawHoveredTileInfo(ImDrawListPtr drawList)
    {
        if (!ImGui.IsMousePosValid() || !_view.TryGetTileAtScreen(ImGui.GetIO().MousePos, out int tileX, out int tileY))
        {
            return;
        }

        Vector2 topLeft = _view.GetTileScreenPosition(tileX, tileY);
        drawList.AddRect(topLeft, topLeft + new Vector2(GameView.TILE_SIZE, GameView.TILE_SIZE), HOVER_COLOR);

        Map map = _session.Map;
        TileType tile = map.GetTile(tileX, tileY);
        ImGui.BeginTooltip();
        ImGui.TextUnformatted(ZString.Format("칸 ({0}, {1})", tileX, tileY));
        ImGui.TextUnformatted(ZString.Concat(DebugNames.GetTileName(tile), tile.IsWalkable ? " (걸을 수 있음)" : " (막힘)"));

        IReadOnlyList<Actor> actors = map.Actors;
        for (int index = 0; index < actors.Count; index++)
        {
            Actor actor = actors[index];
            if (actor.X == tileX && actor.Y == tileY)
            {
                ImGui.TextUnformatted(ZString.Concat(DebugNames.GetActorName(actor), actor.IsAlive ? "" : " (사망)"));
            }
        }

        Item? itemOrNull = map.GetItemAtOrNull(tileX, tileY);
        if (itemOrNull != null)
        {
            ImGui.TextUnformatted(ZString.Format("아이템: {0} {1}", DebugNames.GetItemName(itemOrNull), itemOrNull.Amount));
        }

        ImGui.EndTooltip();
    }

    private static Actor? GetBehaviorTargetOrNull(IBehavior behavior)
    {
        switch (behavior)
        {
            case FleeBehavior flee:
                return flee.Threat;
            case ChaseBehavior chase:
                return chase.Target;
            case AttackBehavior attack:
                return attack.Target;
            default:
                return null;
        }
    }

    private static uint GetBehaviorColor(IBehavior behavior)
    {
        switch (behavior)
        {
            case AttackBehavior _:
                return ATTACK_COLOR;
            case ChaseBehavior _:
                return CHASE_COLOR;
            case FleeBehavior _:
                return FLEE_COLOR;
            default:
                return WANDER_COLOR;
        }
    }

    private static uint MakeColor(int red, int green, int blue, int alpha)
    {
        return (uint)((alpha << 24) | (blue << 16) | (green << 8) | red);
    }

    private static uint WithAlpha(uint color, int alpha)
    {
        return (color & 0x00FFFFFFu) | ((uint)alpha << 24);
    }

    private static Vector4 ToVector4(uint color)
    {
        return ImGui.ColorConvertU32ToFloat4(color);
    }
}
