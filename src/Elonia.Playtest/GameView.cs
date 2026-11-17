using System;
using System.Collections.Generic;
using System.Numerics;
using Cysharp.Text;
using Elonia.Events;
using Elonia.Simulation;
using Raylib_cs;

namespace Elonia.Playtest;

internal sealed class GameView : IEventListener<ActorAttacked>, IEventListener<ActorDied>, IEventListener<ItemPickedUp>, IEventListener<TileHarvested>
{
    public const int TILE_SIZE = SpriteAtlas.SOURCE_TILE_SIZE * 3;

    private const float MOVE_SPEED = 11.0f;
    private const float HOP_HEIGHT = 5.0f;
    private const float FLASH_SECONDS = 0.18f;
    private const float LUNGE_SECONDS = 0.16f;
    private const float FADE_OUT_SECONDS = 0.4f;
    private const float FLOATING_TEXT_SECONDS = 0.8f;
    private const float FLOATING_TEXT_RISE = 34.0f;
    private const float SHAKE_SECONDS = 0.22f;
    private const float SHAKE_STRENGTH = 6.0f;
    private const float CAMERA_FOLLOW_SPEED = 9.0f;

    private static readonly Color WHITE = new Color(255, 255, 255, 255);
    private static readonly Color HIT_TINT = new Color(255, 96, 96, 255);
    private static readonly Color SHADOW = new Color(24, 20, 37, 70);
    private static readonly Color HP_BAR_BACK = new Color(24, 20, 37, 220);
    private static readonly Color HP_BAR_FILL = new Color(232, 72, 72, 255);
    private static readonly Color ALERT_HOSTILE = new Color(255, 96, 96, 255);
    private static readonly Color ALERT_SCARED = new Color(255, 224, 96, 255);
    private static readonly Color TRAVEL_MARK = new Color(255, 255, 255, 150);
    private static readonly Color MARKED_TILE = new Color(255, 224, 96, 220);

    private readonly SpriteAtlas _atlas;
    private readonly UiFont _font;
    private readonly GameSession _session;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private readonly Dictionary<Actor, ActorView> _actorViews = new Dictionary<Actor, ActorView>();
    private readonly List<Actor> _drawOrder = new List<Actor>();
    private readonly List<FloatingText> _floatingTexts = new List<FloatingText>();

    private float _cameraX;
    private float _cameraY;
    private float _shakeTime;

    private int _drawnCameraX;
    private int _drawnCameraY;
    private float _time;

    public IReadOnlyList<(int X, int Y)>? TravelPathOrNull { get; set; }

    public (int X, int Y)? MarkedTileOrNull { get; set; }

    public GameView(SpriteAtlas atlas, UiFont font, GameSession session, int screenWidth, int screenHeight)
    {
        _atlas = atlas;
        _font = font;
        _session = session;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        Map map = session.Map;
        for (int index = 0; index < map.Actors.Count; index++)
        {
            Actor actor = map.Actors[index];
            _actorViews.Add(actor, new ActorView(actor));
        }

        session.World.Events.Register<ActorAttacked, ActorDied, ItemPickedUp, TileHarvested>(this);
        GetCameraTarget(out _cameraX, out _cameraY);
    }

    public void OnEvent(ActorAttacked message)
    {
        ActorView attacker = _actorViews[message.Attacker];
        ActorView target = _actorViews[message.Target];
        float directionX = Math.Sign(message.Target.X - message.Attacker.X);
        float directionY = Math.Sign(message.Target.Y - message.Attacker.Y);
        if (directionX != 0.0f)
        {
            attacker.IsFacingLeft = directionX < 0.0f;
        }

        StartLunge(attacker, directionX, directionY, 0.35f);
        StartLunge(target, directionX, directionY, 0.15f);
        target.FlashTime = FLASH_SECONDS;
        AddFloatingText($"-{message.Damage}", message.Target, HIT_TINT);
        if (message.Target == _session.Player)
        {
            _shakeTime = SHAKE_SECONDS;
        }
    }

    public void OnEvent(ActorDied message)
    {
    }

    public void OnEvent(ItemPickedUp message)
    {
        switch (message.Item.Type)
        {
            case EItemType.Potion:
                AddFloatingText(ZString.Concat("체력 +", message.Item.Amount), message.Actor, MessageLog.GOOD);
                break;
            case EItemType.Gold:
                AddFloatingText(ZString.Concat("+", message.Item.Amount, " 골드"), message.Actor, MessageLog.GOLD);
                break;
            default:
                AddFloatingText(ZString.Concat("+", message.Item.Amount, " ", GameText.GetItemName(message.Item.Type)), message.Actor, MessageLog.NORMAL);
                break;
        }
    }

    public void OnEvent(TileHarvested message)
    {
        ActorView view = _actorViews[message.Actor];
        int directionX = message.X - message.Actor.X;
        int directionY = message.Y - message.Actor.Y;
        if (directionX != 0)
        {
            view.IsFacingLeft = directionX < 0;
        }

        StartLunge(view, directionX, directionY, 0.35f);
    }

    public void ShowBump(Actor actor, int directionX, int directionY)
    {
        ActorView view = _actorViews[actor];
        if (directionX != 0)
        {
            view.IsFacingLeft = directionX < 0;
        }

        StartLunge(view, directionX, directionY, 0.18f);
    }

    public void NotifyTurnEnded()
    {
        Map map = _session.Map;
        for (int index = 0; index < map.Actors.Count; index++)
        {
            Actor actor = map.Actors[index];
            if (!actor.IsAlive || !(actor.ControllerOrNull is Brain brain))
            {
                continue;
            }

            ActorView view = _actorViews[actor];
            IBehavior? current = brain.CurrentBehaviorOrNull;
            if (IsAlert(current) && !IsAlert(view.LastBehaviorOrNull))
            {
                Color color = current is FleeBehavior ? ALERT_SCARED : ALERT_HOSTILE;
                AddFloatingText("!", actor, color);
            }

            view.LastBehaviorOrNull = current;
        }
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;
        Map map = _session.Map;
        for (int index = 0; index < map.Actors.Count; index++)
        {
            Actor actor = map.Actors[index];
            UpdateActorView(actor, _actorViews[actor], deltaTime);
        }

        for (int index = _floatingTexts.Count - 1; index >= 0; index--)
        {
            _floatingTexts[index].Age += deltaTime;
            if (_floatingTexts[index].Age >= FLOATING_TEXT_SECONDS)
            {
                _floatingTexts.RemoveAt(index);
            }
        }

        _shakeTime = Math.Max(0.0f, _shakeTime - deltaTime);

        GetCameraTarget(out float targetX, out float targetY);
        float follow = Math.Min(1.0f, CAMERA_FOLLOW_SPEED * deltaTime);
        _cameraX += (targetX - _cameraX) * follow;
        _cameraY += (targetY - _cameraY) * follow;
    }

    public void Draw()
    {
        Map map = _session.Map;

        int cameraX = (int)MathF.Round(_cameraX + GetShakeOffset(1.0f));
        int cameraY = (int)MathF.Round(_cameraY + GetShakeOffset(1.7f));
        _drawnCameraX = cameraX;
        _drawnCameraY = cameraY;

        int firstX = Math.Max(0, cameraX / TILE_SIZE);
        int firstY = Math.Max(0, cameraY / TILE_SIZE);
        int lastX = Math.Min(map.Width - 1, (cameraX + _screenWidth) / TILE_SIZE);
        int lastY = Math.Min(map.Height - 1, (cameraY + _screenHeight) / TILE_SIZE + 1);

        for (int y = firstY; y <= lastY; y++)
        {
            for (int x = firstX; x <= lastX; x++)
            {
                _atlas.Draw(Art.GetGround(map, x, y), x * TILE_SIZE - cameraX, y * TILE_SIZE - cameraY, TILE_SIZE, WHITE);
            }
        }

        for (int y = firstY; y <= lastY; y++)
        {
            for (int x = firstX; x <= lastX; x++)
            {
                Sprite? spriteOrNull = Art.GetObjectOrNull(_session, x, y);
                if (spriteOrNull != null)
                {
                    _atlas.Draw(spriteOrNull.Value, x * TILE_SIZE - cameraX, y * TILE_SIZE - cameraY, TILE_SIZE, WHITE);
                }
            }
        }

        DrawTravelPath(cameraX, cameraY);
        DrawMarkedTile(cameraX, cameraY);
        DrawItems(map, cameraX, cameraY);
        DrawActors(map, cameraX, cameraY);

        for (int y = firstY; y <= lastY; y++)
        {
            for (int x = firstX; x <= lastX; x++)
            {
                Sprite? topOrNull = Art.GetTreeTopOrNull(map, x, y);
                if (topOrNull != null)
                {
                    _atlas.Draw(topOrNull.Value, x * TILE_SIZE - cameraX, (y - 1) * TILE_SIZE - cameraY, TILE_SIZE, WHITE);
                }
            }
        }

        DrawHpBars(map, cameraX, cameraY);
        DrawFloatingTexts(cameraX, cameraY);
    }

    public Vector2 GetTileScreenPosition(float tileX, float tileY)
    {
        return new Vector2(tileX * TILE_SIZE - _drawnCameraX, tileY * TILE_SIZE - _drawnCameraY);
    }

    public Vector2 GetActorScreenPosition(Actor actor)
    {
        ActorView view = _actorViews[actor];
        return GetTileScreenPosition(view.X, view.Y);
    }

    public bool TryGetTileAtScreen(Vector2 screenPosition, out int tileX, out int tileY)
    {
        tileX = (int)MathF.Floor((screenPosition.X + _drawnCameraX) / TILE_SIZE);
        tileY = (int)MathF.Floor((screenPosition.Y + _drawnCameraY) / TILE_SIZE);
        return _session.Map.IsInside(tileX, tileY);
    }

    private void UpdateActorView(Actor actor, ActorView view, float deltaTime)
    {
        float deltaX = actor.X - view.X;
        float deltaY = actor.Y - view.Y;
        float distance = MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
        if (distance > 0.0f)
        {
            if (deltaX != 0.0f)
            {
                view.IsFacingLeft = deltaX < 0.0f;
            }

            float step = Math.Max(MOVE_SPEED, distance * MOVE_SPEED) * deltaTime;
            if (step >= distance)
            {
                view.X = actor.X;
                view.Y = actor.Y;
            }
            else
            {
                view.X += deltaX / distance * step;
                view.Y += deltaY / distance * step;
            }
        }

        view.FlashTime = Math.Max(0.0f, view.FlashTime - deltaTime);
        view.LungeTime = Math.Max(0.0f, view.LungeTime - deltaTime);
        if (!actor.IsAlive)
        {
            view.DeadTime += deltaTime;
        }
    }

    private void DrawTravelPath(int cameraX, int cameraY)
    {
        IReadOnlyList<(int X, int Y)>? pathOrNull = TravelPathOrNull;
        if (pathOrNull == null || pathOrNull.Count == 0)
        {
            return;
        }

        for (int index = 0; index < pathOrNull.Count - 1; index++)
        {
            (int x, int y) = pathOrNull[index];
            Raylib.DrawCircle(x * TILE_SIZE - cameraX + TILE_SIZE / 2, y * TILE_SIZE - cameraY + TILE_SIZE / 2, 3.0f, TRAVEL_MARK);
        }

        (int goalX, int goalY) = pathOrNull[pathOrNull.Count - 1];
        Rectangle goal = new Rectangle(goalX * TILE_SIZE - cameraX + 4, goalY * TILE_SIZE - cameraY + 4, TILE_SIZE - 8, TILE_SIZE - 8);
        Raylib.DrawRectangleRoundedLinesEx(goal, 0.25f, 4, 2.0f, TRAVEL_MARK);
    }

    private void DrawMarkedTile(int cameraX, int cameraY)
    {
        if (MarkedTileOrNull == null)
        {
            return;
        }

        (int x, int y) = MarkedTileOrNull.Value;
        Rectangle tile = new Rectangle(x * TILE_SIZE - cameraX + 2, y * TILE_SIZE - cameraY + 2, TILE_SIZE - 4, TILE_SIZE - 4);
        Raylib.DrawRectangleRoundedLinesEx(tile, 0.25f, 4, 3.0f, MARKED_TILE);
    }

    private void DrawItems(Map map, int cameraX, int cameraY)
    {
        for (int index = 0; index < map.Items.Count; index++)
        {
            Item item = map.Items[index];

            float bob = MathF.Sin(_time * 3.0f + item.X) * 3.0f;
            int screenX = item.X * TILE_SIZE - cameraX;
            int screenY = item.Y * TILE_SIZE - cameraY;
            Raylib.DrawEllipse(screenX + TILE_SIZE / 2, screenY + TILE_SIZE - 6, 11.0f, 4.0f, SHADOW);
            _atlas.Draw(Art.GetItemSprite(item.Type), screenX, screenY - 4 + (int)MathF.Round(bob), TILE_SIZE, WHITE);
        }
    }

    private void DrawActors(Map map, int cameraX, int cameraY)
    {
        _drawOrder.Clear();
        for (int index = 0; index < map.Actors.Count; index++)
        {
            Actor actor = map.Actors[index];
            if (_actorViews[actor].DeadTime < FADE_OUT_SECONDS)
            {
                _drawOrder.Add(actor);
            }
        }

        _drawOrder.Sort(CompareByScreenY);

        for (int index = 0; index < _drawOrder.Count; index++)
        {
            Actor actor = _drawOrder[index];
            ActorView view = _actorViews[actor];
            GetActorScreenPosition(actor, view, cameraX, cameraY, out int screenX, out int screenY, out int hop);

            float alpha = 1.0f - view.DeadTime / FADE_OUT_SECONDS;
            Raylib.DrawEllipse(screenX + TILE_SIZE / 2, screenY + TILE_SIZE - 5, 14.0f, 5.0f, Raylib.Fade(SHADOW, alpha * SHADOW.A / 255.0f));

            Color tint = view.FlashTime > 0.0f ? HIT_TINT : WHITE;
            _atlas.Draw(Art.GetActorSprite(actor), screenX, screenY - 4 - hop, TILE_SIZE, Raylib.Fade(tint, alpha), view.IsFacingLeft);
        }
    }

    private void DrawHpBars(Map map, int cameraX, int cameraY)
    {
        const int BAR_WIDTH = 34;
        const int BAR_HEIGHT = 6;

        for (int index = 0; index < map.Actors.Count; index++)
        {
            Actor actor = map.Actors[index];

            if (!actor.IsAlive || actor.Hp == actor.Type.MaxHp || actor == _session.Player)
            {
                continue;
            }

            GetActorScreenPosition(actor, _actorViews[actor], cameraX, cameraY, out int screenX, out int screenY, out int hop);
            int left = screenX + (TILE_SIZE - BAR_WIDTH) / 2;
            int top = screenY - 12 - hop;
            int fill = (BAR_WIDTH - 2) * actor.Hp / actor.Type.MaxHp;
            Raylib.DrawRectangle(left, top, BAR_WIDTH, BAR_HEIGHT, HP_BAR_BACK);
            Raylib.DrawRectangle(left + 1, top + 1, fill, BAR_HEIGHT - 2, HP_BAR_FILL);
        }
    }

    private void DrawFloatingTexts(int cameraX, int cameraY)
    {
        for (int index = 0; index < _floatingTexts.Count; index++)
        {
            FloatingText text = _floatingTexts[index];
            float progress = text.Age / FLOATING_TEXT_SECONDS;

            float rise = (1.0f - (1.0f - progress) * (1.0f - progress)) * FLOATING_TEXT_RISE;
            float alpha = progress < 0.6f ? 1.0f : 1.0f - (progress - 0.6f) / 0.4f;
            int centerX = (int)MathF.Round(text.Anchor.X * TILE_SIZE) + TILE_SIZE / 2 - cameraX;
            int y = (int)MathF.Round(text.Anchor.Y * TILE_SIZE - 34.0f - rise) - cameraY;
            _font.DrawCentered(text.Text, centerX, y, ETextSize.Normal, text.Color, alpha);
        }
    }

    private void GetActorScreenPosition(Actor actor, ActorView view, int cameraX, int cameraY, out int screenX, out int screenY, out int hop)
    {
        float lunge = 0.0f;
        if (view.LungeTime > 0.0f)
        {
            lunge = MathF.Sin(view.LungeTime / LUNGE_SECONDS * MathF.PI) * view.LungeDistance;
        }

        screenX = (int)MathF.Round((view.X + view.LungeX * lunge) * TILE_SIZE) - cameraX;
        screenY = (int)MathF.Round((view.Y + view.LungeY * lunge) * TILE_SIZE) - cameraY;

        float remaining = MathF.Sqrt((actor.X - view.X) * (actor.X - view.X) + (actor.Y - view.Y) * (actor.Y - view.Y));
        hop = (int)MathF.Round(MathF.Sin(Math.Min(1.0f, remaining) * MathF.PI) * HOP_HEIGHT);
    }

    private void GetCameraTarget(out float targetX, out float targetY)
    {
        Map map = _session.Map;
        ActorView player = _actorViews[_session.Player];

        float centerX = (player.X + 0.5f) * TILE_SIZE;
        float centerY = (player.Y + 0.5f) * TILE_SIZE;
        float maxX = Math.Max(0.0f, map.Width * TILE_SIZE - _screenWidth);
        float maxY = Math.Max(0.0f, map.Height * TILE_SIZE - _screenHeight);
        targetX = Math.Clamp(centerX - _screenWidth / 2.0f, 0.0f, maxX);
        targetY = Math.Clamp(centerY - _screenHeight / 2.0f, 0.0f, maxY);
    }

    private float GetShakeOffset(float frequencyScale)
    {
        if (_shakeTime <= 0.0f)
        {
            return 0.0f;
        }

        return MathF.Sin(_time * 70.0f * frequencyScale) * SHAKE_STRENGTH * (_shakeTime / SHAKE_SECONDS);
    }

    private void AddFloatingText(string text, Actor actor, Color color)
    {
        _floatingTexts.Add(new FloatingText(text, _actorViews[actor], color));
    }

    private int CompareByScreenY(Actor left, Actor right)
    {
        return _actorViews[left].Y.CompareTo(_actorViews[right].Y);
    }

    private static void StartLunge(ActorView view, float directionX, float directionY, float distance)
    {
        view.LungeTime = LUNGE_SECONDS;
        view.LungeX = directionX;
        view.LungeY = directionY;
        view.LungeDistance = distance;
    }

    private static bool IsAlert(IBehavior? behaviorOrNull)
    {
        return behaviorOrNull is ChaseBehavior || behaviorOrNull is AttackBehavior || behaviorOrNull is FleeBehavior;
    }

    private sealed class FloatingText
    {
        public string Text { get; }
        public ActorView Anchor { get; }
        public Color Color { get; }
        public float Age { get; set; }

        public FloatingText(string text, ActorView anchor, Color color)
        {
            Text = text;
            Anchor = anchor;
            Color = color;
        }
    }
}
