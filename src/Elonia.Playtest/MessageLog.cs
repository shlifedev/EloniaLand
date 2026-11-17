using System;
using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Events;
using Elonia.Simulation;
using Raylib_cs;

namespace Elonia.Playtest;

internal sealed class MessageLog : IEventListener<ActorAttacked>, IEventListener<ActorDied>, IEventListener<ItemPickedUp>, IEventListener<TileHarvested>
{
    public static readonly Color NORMAL = new Color(240, 236, 220, 255);
    public static readonly Color DANGER = new Color(255, 120, 110, 255);
    public static readonly Color GOOD = new Color(140, 230, 150, 255);
    public static readonly Color GOLD = new Color(255, 214, 90, 255);
    public static readonly Color MUTED = new Color(190, 196, 210, 255);

    private static readonly Color PANEL = new Color(24, 20, 37, 170);

    private const int MAX_LINE_COUNT = 5;

    private const float VISIBLE_SECONDS = 6.0f;
    private const float FADE_SECONDS = 1.5f;
    private const int LINE_HEIGHT = 26;

    private readonly Actor _player;
    private readonly List<Line> _lines = new List<Line>();

    public MessageLog(Actor player)
    {
        _player = player;
    }

    public void Add(string text, Color color)
    {
        if (_lines.Count > 0 && _lines[_lines.Count - 1].Text == text)
        {
            _lines[_lines.Count - 1].Age = 0.0f;
            return;
        }

        _lines.Add(new Line(text, color));
        if (_lines.Count > MAX_LINE_COUNT)
        {
            _lines.RemoveAt(0);
        }
    }

    public void OnEvent(ActorAttacked message)
    {
        if (message.Attacker == _player)
        {
            Add(ZString.Concat(GameText.GetActorTypeName(message.Target.Type), "에게 ", message.Damage, "의 피해를 주었다."), NORMAL);
        }
        else if (message.Target == _player)
        {
            Add(ZString.Concat(GameText.GetActorTypeName(message.Attacker.Type), "의 공격! ", message.Damage, "의 피해를 입었다."), DANGER);
        }
    }

    public void OnEvent(ActorDied message)
    {
        if (message.Actor == _player)
        {
            Add("당신은 쓰러졌다...", DANGER);
        }
        else
        {
            Add(ZString.Concat(GameText.WithObjectParticle(GameText.GetActorTypeName(message.Actor.Type)), " 쓰러뜨렸다."), GOOD);
        }
    }

    public void OnEvent(ItemPickedUp message)
    {
        if (message.Actor != _player)
        {
            return;
        }

        switch (message.Item.Type)
        {
            case EItemType.Potion:
                Add(ZString.Concat("물약을 마셨다. (체력 +", message.Item.Amount, ")"), GOOD);
                break;
            case EItemType.Gold:
                Add(ZString.Concat(message.Item.Amount, " 골드를 주웠다."), GOLD);
                break;
            default:
                Add(ZString.Concat(GameText.WithObjectParticle(GameText.GetItemName(message.Item.Type)), " 주웠다. (x", message.Item.Amount, ")"), NORMAL);
                break;
        }
    }

    public void OnEvent(TileHarvested message)
    {
        if (message.Actor != _player)
        {
            return;
        }

        Add(ZString.Concat(GameText.WithObjectParticle(GameText.GetTileName(message.HarvestedTile)), " 베었다. ",
            GameText.WithSubjectParticle(GameText.GetItemName(message.DroppedItem.Type)), " 떨어졌다."), NORMAL);
    }

    public void Update(float deltaTime)
    {
        for (int index = _lines.Count - 1; index >= 0; index--)
        {
            _lines[index].Age += deltaTime;
            if (_lines[index].Age >= VISIBLE_SECONDS + FADE_SECONDS)
            {
                _lines.RemoveAt(index);
            }
        }
    }

    public void Draw(UiFont font, int left, int bottom)
    {
        if (_lines.Count == 0)
        {
            return;
        }

        int panelWidth = 0;
        float panelAlpha = 0.0f;
        for (int index = 0; index < _lines.Count; index++)
        {
            panelWidth = Math.Max(panelWidth, font.Measure(_lines[index].Text, ETextSize.Normal));
            panelAlpha = Math.Max(panelAlpha, GetAlpha(_lines[index]));
        }

        int panelHeight = _lines.Count * LINE_HEIGHT + 12;
        Rectangle panel = new Rectangle(left - 10, bottom - panelHeight + 4, panelWidth + 24, panelHeight);
        Raylib.DrawRectangleRounded(panel, 0.2f, 6, Raylib.Fade(PANEL, panelAlpha * PANEL.A / 255.0f));

        for (int index = 0; index < _lines.Count; index++)
        {
            Line line = _lines[index];
            int y = bottom - (_lines.Count - index) * LINE_HEIGHT;
            font.Draw(line.Text, left, y, ETextSize.Normal, line.Color, GetAlpha(line));
        }
    }

    private static float GetAlpha(Line line)
    {
        if (line.Age <= VISIBLE_SECONDS)
        {
            return 1.0f;
        }

        return Math.Max(0.0f, 1.0f - (line.Age - VISIBLE_SECONDS) / FADE_SECONDS);
    }

    private sealed class Line
    {
        public string Text { get; }
        public Color Color { get; }
        public float Age { get; set; }

        public Line(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }
}
