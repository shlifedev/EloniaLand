using System.Numerics;
using Elonia.Game;
using ImGuiNET;
using Raylib_cs;

namespace Elonia.Playtest;

internal sealed class PlayerInput
{
    private const float REPEAT_DELAY = 0.22f;
    private const float REPEAT_INTERVAL = 0.12f;

    private EPlayerCommand _heldMove = EPlayerCommand.None;
    private float _repeatTimer;

    public EPlayerCommand Read(float deltaTime)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.R))
        {
            return EPlayerCommand.Restart;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            return EPlayerCommand.Wait;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.E) || Raylib.IsKeyPressed(KeyboardKey.G))
        {
            return EPlayerCommand.PickUp;
        }

        return ReadMove(deltaTime);
    }

    public bool TryReadMapClick(out Vector2 screenPosition)
    {
        screenPosition = Raylib.GetMousePosition();
        return Raylib.IsMouseButtonPressed(MouseButton.Left) && !ImGui.GetIO().WantCaptureMouse;
    }

    public bool TryReadMenuClick(out Vector2 screenPosition)
    {
        screenPosition = Raylib.GetMousePosition();
        return Raylib.IsMouseButtonPressed(MouseButton.Right) && !ImGui.GetIO().WantCaptureMouse;
    }

    private EPlayerCommand ReadMove(float deltaTime)
    {
        EPlayerCommand pressed = GetPressedMove();
        if (pressed != EPlayerCommand.None)
        {
            _heldMove = pressed;
            _repeatTimer = REPEAT_DELAY;
            return pressed;
        }

        if (_heldMove == EPlayerCommand.None || !IsMoveDown(_heldMove))
        {
            _heldMove = EPlayerCommand.None;
            return EPlayerCommand.None;
        }

        _repeatTimer -= deltaTime;
        if (_repeatTimer > 0.0f)
        {
            return EPlayerCommand.None;
        }

        _repeatTimer = REPEAT_INTERVAL;
        return _heldMove;
    }

    private static EPlayerCommand GetPressedMove()
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Up) || Raylib.IsKeyPressed(KeyboardKey.W))
        {
            return EPlayerCommand.MoveUp;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Down) || Raylib.IsKeyPressed(KeyboardKey.S))
        {
            return EPlayerCommand.MoveDown;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Left) || Raylib.IsKeyPressed(KeyboardKey.A))
        {
            return EPlayerCommand.MoveLeft;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Right) || Raylib.IsKeyPressed(KeyboardKey.D))
        {
            return EPlayerCommand.MoveRight;
        }

        return EPlayerCommand.None;
    }

    private static bool IsMoveDown(EPlayerCommand move)
    {
        switch (move)
        {
            case EPlayerCommand.MoveUp:
                return Raylib.IsKeyDown(KeyboardKey.Up) || Raylib.IsKeyDown(KeyboardKey.W);
            case EPlayerCommand.MoveDown:
                return Raylib.IsKeyDown(KeyboardKey.Down) || Raylib.IsKeyDown(KeyboardKey.S);
            case EPlayerCommand.MoveLeft:
                return Raylib.IsKeyDown(KeyboardKey.Left) || Raylib.IsKeyDown(KeyboardKey.A);
            case EPlayerCommand.MoveRight:
                return Raylib.IsKeyDown(KeyboardKey.Right) || Raylib.IsKeyDown(KeyboardKey.D);
            default:
                return false;
        }
    }
}
