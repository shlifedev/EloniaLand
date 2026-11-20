using System;
using System.Collections.Generic;
using System.Numerics;
using Cysharp.Text;
using Elonia.Events;
using Elonia.Game;
using Elonia.Simulation;
using Raylib_cs;

namespace Elonia.Playtest;

internal sealed class GameView : IEventListener<ActorAttacked>, IEventListener<ActorDied>, IEventListener<ItemPickedUp>, IEventListener<PropHarvested>,
    IEventListener<ActorDamaged>, IEventListener<MoveBlocked>, IEventListener<ActorNoticedPlayer>
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

    private static readonly Color WALL_TOP = new Color(150, 160, 186, 255);
    private static readonly Color WALL_FRONT = new Color(92, 86, 116, 255);
    private static readonly Color WALL_MORTAR = new Color(68, 62, 92, 255);
    private static readonly Color WALL_OUTLINE = new Color(24, 20, 37, 255);
    private static readonly Color DOOR_PANEL = new Color(150, 96, 56, 255);
    private const int WALL_OUTLINE_WIDTH = 3;

    private const int WALL_TOP_HEIGHT = TILE_SIZE;

    private const float WALL_SEE_THROUGH_ALPHA = 0.45f;

    private static readonly Comparison<WorldDrawItem> COMPARE_BY_FOOT_Y = CompareByFootY;

    private readonly SpriteAtlas _atlas;
    private readonly UiFont _font;
    private readonly GameSession _session;

    private readonly IReadOnlyDictionary<(int X, int Y, PropType Prop), Sprite> _houseSprites;
    private readonly int _screenWidth;
    private readonly int _screenHeight;
    private readonly Dictionary<Actor, ActorView> _actorViews = new Dictionary<Actor, ActorView>();
    private readonly List<WorldDrawItem> _drawItems = new List<WorldDrawItem>();
    private readonly List<FloatingText> _floatingTexts = new List<FloatingText>();

    private float _cameraX;
    private float _cameraY;
    private float _shakeTime;

    private int _drawnCameraX;
    private int _drawnCameraY;
    private float _time;

    public IReadOnlyList<(int X, int Y)>? TravelPathOrNull { get; set; }

    public (int X, int Y)? MarkedTileOrNull { get; set; }

    public GameView(SpriteAtlas atlas, UiFont font, GameSession session, IReadOnlyDictionary<(int X, int Y, PropType Prop), Sprite> houseSprites,
        int screenWidth, int screenHeight)
    {
        _atlas = atlas;
        _font = font;
        _session = session;
        _houseSprites = houseSprites;
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;

        Map map = session.Map;
        for (int index = 0; index < map.Actors.Count; index++)
        {
            Actor actor = map.Actors[index];
            _actorViews.Add(actor, new ActorView(actor));
        }

        session.World.Events.Register<ActorAttacked, ActorDied, ItemPickedUp, PropHarvested, ActorDamaged, MoveBlocked, ActorNoticedPlayer>(this);
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

    public void OnEvent(ActorDamaged message)
    {
        ActorView target = _actorViews[message.Actor];
        target.FlashTime = FLASH_SECONDS;
        AddFloatingText(ZString.Concat("-", message.Damage), message.Actor, HIT_TINT);
        if (message.Actor == _session.Player)
        {
            _shakeTime = SHAKE_SECONDS;
        }
    }

    public void OnEvent(ActorDied message)
    {
    }

    public void OnEvent(ItemPickedUp message)
    {
        switch (message.Effect)
        {
            case EItemEffect.Heal:
                AddFloatingText(ZString.Concat("체력 +", message.Amount), message.Actor, MessageLog.GOOD);
                break;
            case EItemEffect.Gold:
                AddFloatingText(ZString.Concat("+", message.Amount, " 골드"), message.Actor, MessageLog.GOLD);
                break;
            default:
                AddFloatingText(ZString.Concat("+", message.Amount, " ", GameText.GetThingName(message.Thing.Type)), message.Actor, MessageLog.NORMAL);
                break;
        }
    }

    public void OnEvent(PropHarvested message)
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

    public void OnEvent(MoveBlocked message)
    {
        ActorView view = _actorViews[message.Actor];
        if (message.DeltaX != 0)
        {
            view.IsFacingLeft = message.DeltaX < 0;
        }

        StartLunge(view, message.DeltaX, message.DeltaY, 0.18f);
    }

    public void OnEvent(ActorNoticedPlayer message)
    {
        AddFloatingText("!", message.Actor, message.Kind == EAlertKind.Scared ? ALERT_SCARED : ALERT_HOSTILE);
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

        CollectDrawItems(map, firstX, firstY, lastX, lastY);
        _drawItems.Sort(COMPARE_BY_FOOT_Y);
        for (int index = 0; index < _drawItems.Count; index++)
        {
            DrawItem(map, _drawItems[index], cameraX, cameraY);
        }

        DrawTravelPath(cameraX, cameraY);
        DrawMarkedTile(cameraX, cameraY);

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

    private void CollectDrawItems(Map map, int firstX, int firstY, int lastX, int lastY)
    {
        _drawItems.Clear();
        for (int y = firstY; y <= lastY; y++)
        {
            for (int x = firstX; x <= lastX; x++)
            {
                if (map.GetPropOrNull(x, y) != null)
                {
                    _drawItems.Add(new WorldDrawItem(y + 1, EWorldDrawKind.Prop, x, y, -1));
                }
            }
        }

        for (int index = 0; index < map.Things.Count; index++)
        {
            Thing thing = map.Things[index];
            _drawItems.Add(new WorldDrawItem(thing.Y + 1, EWorldDrawKind.Thing, thing.X, thing.Y, index));
        }

        for (int index = 0; index < map.Actors.Count; index++)
        {
            Actor actor = map.Actors[index];
            ActorView view = _actorViews[actor];
            if (view.DeadTime < FADE_OUT_SECONDS)
            {
                _drawItems.Add(new WorldDrawItem(view.Y + 1.0f, EWorldDrawKind.Actor, actor.X, actor.Y, index));
            }
        }
    }

    private void DrawItem(Map map, WorldDrawItem item, int cameraX, int cameraY)
    {
        switch (item.Kind)
        {
            case EWorldDrawKind.Prop:
                DrawProp(map, item.X, item.Y, cameraX, cameraY);
                break;
            case EWorldDrawKind.Thing:
                DrawThing(map.Things[item.Index], cameraX, cameraY);
                break;
            default:
                DrawActor(map.Actors[item.Index], cameraX, cameraY);
                break;
        }
    }

    private void DrawProp(Map map, int x, int y, int cameraX, int cameraY)
    {
        int screenX = x * TILE_SIZE - cameraX;
        int screenY = y * TILE_SIZE - cameraY;
        PropType? propOrNull = map.GetPropOrNull(x, y);
        if (propOrNull != null && _houseSprites.TryGetValue((x, y, propOrNull), out Sprite house))
        {
            _atlas.Draw(house, screenX, screenY, TILE_SIZE, WHITE);
            return;
        }

        WallPiece? wallOrNull = Art.GetWallPieceOrNull(map, x, y);
        if (wallOrNull != null)
        {
            DrawWall(wallOrNull.Value, x, y, screenX, screenY);
            return;
        }

        Sprite? spriteOrNull = Art.GetPropSpriteOrNull(map, x, y);
        if (spriteOrNull != null)
        {
            _atlas.Draw(spriteOrNull.Value, screenX, screenY, TILE_SIZE, WHITE);
        }

        Sprite? topOrNull = Art.GetTreeTopOrNull(map, x, y);
        if (topOrNull != null)
        {
            _atlas.Draw(topOrNull.Value, screenX, screenY - TILE_SIZE, TILE_SIZE, WHITE);
        }
    }

    private void DrawWall(WallPiece piece, int x, int y, int screenX, int screenY)
    {
        float alpha = IsPlayerBehindWall(x, y) ? WALL_SEE_THROUGH_ALPHA : 1.0f;
        Color top = Raylib.Fade(WALL_TOP, alpha);
        Color front = Raylib.Fade(WALL_FRONT, alpha);
        Color mortar = Raylib.Fade(WALL_MORTAR, alpha);
        Color outline = Raylib.Fade(WALL_OUTLINE, alpha);

        int topY = screenY - WALL_TOP_HEIGHT;
        int height = WALL_TOP_HEIGHT + TILE_SIZE;
        Raylib.DrawRectangle(screenX, topY, TILE_SIZE, piece.HasFrontFace ? WALL_TOP_HEIGHT : height, top);

        if (piece.HasFrontFace)
        {
            Raylib.DrawRectangle(screenX, screenY, TILE_SIZE, TILE_SIZE, front);

            int brickHeight = TILE_SIZE / 3;
            for (int row = 0; row < 3; row++)
            {
                int rowY = screenY + row * brickHeight;
                if (row > 0)
                {
                    Raylib.DrawRectangle(screenX, rowY, TILE_SIZE, 2, mortar);
                }

                int jointX = screenX + (row % 2 == 0 ? TILE_SIZE / 3 : TILE_SIZE * 2 / 3);
                Raylib.DrawRectangle(jointX, rowY, 2, brickHeight, mortar);
            }

            Raylib.DrawRectangle(screenX, screenY, TILE_SIZE, WALL_OUTLINE_WIDTH, outline);
            Raylib.DrawRectangle(screenX, screenY + TILE_SIZE - WALL_OUTLINE_WIDTH, TILE_SIZE, WALL_OUTLINE_WIDTH, outline);
        }

        if (piece.IsDoor)
        {
            int doorWidth = TILE_SIZE / 2;
            int doorTop = screenY + TILE_SIZE / 6;
            Rectangle door = new Rectangle(screenX + (TILE_SIZE - doorWidth) / 2, doorTop, doorWidth, screenY + TILE_SIZE - doorTop);
            Raylib.DrawRectangleRec(door, Raylib.Fade(DOOR_PANEL, alpha));
            Raylib.DrawRectangleLinesEx(door, 2.0f, outline);
        }

        if (!piece.IsNorthConnected)
        {
            Raylib.DrawRectangle(screenX, topY, TILE_SIZE, WALL_OUTLINE_WIDTH, outline);
        }

        if (!piece.IsWestConnected)
        {
            Raylib.DrawRectangle(screenX, topY, WALL_OUTLINE_WIDTH, height, outline);
        }

        if (!piece.IsEastConnected)
        {
            Raylib.DrawRectangle(screenX + TILE_SIZE - WALL_OUTLINE_WIDTH, topY, WALL_OUTLINE_WIDTH, height, outline);
        }
    }

    private bool IsPlayerBehindWall(int x, int y)
    {
        ActorView player = _actorViews[_session.Player];
        float topHeightInTiles = WALL_TOP_HEIGHT / (float)TILE_SIZE;
        return MathF.Abs(player.X - x) < 1.5f && player.Y > y - 1.0f - topHeightInTiles && player.Y < y;
    }

    private void DrawThing(Thing thing, int cameraX, int cameraY)
    {
        float bob = MathF.Sin(_time * 3.0f + thing.X) * 3.0f;
        int screenX = thing.X * TILE_SIZE - cameraX;
        int screenY = thing.Y * TILE_SIZE - cameraY;
        Raylib.DrawEllipse(screenX + TILE_SIZE / 2, screenY + TILE_SIZE - 6, 11.0f, 4.0f, SHADOW);
        _atlas.Draw(Art.GetThingSprite(thing.Type), screenX, screenY - 4 + (int)MathF.Round(bob), TILE_SIZE, WHITE);
    }

    private void DrawActor(Actor actor, int cameraX, int cameraY)
    {
        ActorView view = _actorViews[actor];
        GetActorScreenPosition(actor, view, cameraX, cameraY, out int screenX, out int screenY, out int hop);

        float alpha = 1.0f - view.DeadTime / FADE_OUT_SECONDS;
        Raylib.DrawEllipse(screenX + TILE_SIZE / 2, screenY + TILE_SIZE - 5, 14.0f, 5.0f, Raylib.Fade(SHADOW, alpha * SHADOW.A / 255.0f));

        Color tint = view.FlashTime > 0.0f ? HIT_TINT : WHITE;
        _atlas.Draw(Art.GetActorSprite(actor), screenX, screenY - 4 - hop, TILE_SIZE, Raylib.Fade(tint, alpha), view.IsFacingLeft);
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

    private static int CompareByFootY(WorldDrawItem left, WorldDrawItem right)
    {
        int result = left.FootY.CompareTo(right.FootY);
        if (result != 0)
        {
            return result;
        }

        result = ((int)left.Kind).CompareTo((int)right.Kind);
        return result != 0 ? result : left.X.CompareTo(right.X);
    }

    private static void StartLunge(ActorView view, float directionX, float directionY, float distance)
    {
        view.LungeTime = LUNGE_SECONDS;
        view.LungeX = directionX;
        view.LungeY = directionY;
        view.LungeDistance = distance;
    }

    private enum EWorldDrawKind
    {
        Prop,
        Thing,
        Actor
    }

    private readonly struct WorldDrawItem
    {
        public float FootY { get; }
        public EWorldDrawKind Kind { get; }
        public int X { get; }
        public int Y { get; }
        public int Index { get; }

        public WorldDrawItem(float footY, EWorldDrawKind kind, int x, int y, int index)
        {
            FootY = footY;
            Kind = kind;
            X = x;
            Y = y;
            Index = index;
        }
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
