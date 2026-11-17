using System;
using System.Collections.Generic;
using System.Numerics;
using Elonia.Simulation;
using Raylib_cs;

namespace Elonia.Playtest;

internal sealed class ContextMenu
{
    private const int PADDING_X = 14;
    private const int PADDING_Y = 8;
    private const int ROW_HEIGHT = 32;
    private const int MIN_WIDTH = 160;

    private static readonly Color PANEL = new Color(24, 20, 37, 235);
    private static readonly Color PANEL_BORDER = new Color(240, 236, 220, 90);
    private static readonly Color HOVER = new Color(240, 236, 220, 40);

    private readonly List<Entry> _entries = new List<Entry>();
    private Rectangle _bounds;

    public bool IsOpen { get; private set; }

    public int TileX { get; private set; }
    public int TileY { get; private set; }

    public void Clear()
    {
        _entries.Clear();
    }

    public void AddEntry(string label, Interaction? interactionOrNull)
    {
        _entries.Add(new Entry(label, interactionOrNull));
    }

    public void Open(UiFont font, Vector2 screenPosition, int tileX, int tileY, int screenWidth, int screenHeight)
    {
        if (_entries.Count == 0)
        {
            IsOpen = false;
            return;
        }

        int width = MIN_WIDTH;
        for (int index = 0; index < _entries.Count; index++)
        {
            width = Math.Max(width, font.Measure(_entries[index].Label, ETextSize.Normal) + PADDING_X * 2);
        }

        int height = _entries.Count * ROW_HEIGHT + PADDING_Y * 2;
        float left = Math.Min(screenPosition.X, screenWidth - width - 4);
        float top = Math.Min(screenPosition.Y, screenHeight - height - 4);
        _bounds = new Rectangle(left, top, width, height);
        TileX = tileX;
        TileY = tileY;
        IsOpen = true;
    }

    public void Close()
    {
        IsOpen = false;
        _entries.Clear();
    }

    public bool TryGetEntryAt(Vector2 screenPosition, out Interaction? interactionOrNull)
    {
        int index = GetEntryIndexAt(screenPosition);
        if (index < 0)
        {
            interactionOrNull = null;
            return false;
        }

        interactionOrNull = _entries[index].InteractionOrNull;
        return true;
    }

    public void Draw(UiFont font)
    {
        if (!IsOpen)
        {
            return;
        }

        Raylib.DrawRectangleRounded(_bounds, 0.12f, 6, PANEL);
        Raylib.DrawRectangleRoundedLinesEx(_bounds, 0.12f, 6, 2.0f, PANEL_BORDER);

        int hoveredIndex = GetEntryIndexAt(Raylib.GetMousePosition());
        for (int index = 0; index < _entries.Count; index++)
        {
            int rowTop = (int)_bounds.Y + PADDING_Y + index * ROW_HEIGHT;
            if (index == hoveredIndex)
            {
                Raylib.DrawRectangle((int)_bounds.X + 4, rowTop, (int)_bounds.Width - 8, ROW_HEIGHT, HOVER);
            }

            font.Draw(_entries[index].Label, (int)_bounds.X + PADDING_X, rowTop, ETextSize.Normal, MessageLog.NORMAL);
        }
    }

    private int GetEntryIndexAt(Vector2 screenPosition)
    {
        if (!IsOpen || !Raylib.CheckCollisionPointRec(screenPosition, _bounds))
        {
            return -1;
        }

        int index = (int)((screenPosition.Y - _bounds.Y - PADDING_Y) / ROW_HEIGHT);
        if (screenPosition.Y < _bounds.Y + PADDING_Y || index >= _entries.Count)
        {
            return -1;
        }

        return index;
    }

    private readonly struct Entry
    {
        public string Label { get; }
        public Interaction? InteractionOrNull { get; }

        public Entry(string label, Interaction? interactionOrNull)
        {
            Label = label;
            InteractionOrNull = interactionOrNull;
        }
    }
}
