using System;
using System.Collections.Generic;
using Elonia.Simulation;

namespace Elonia.Playtest;

internal sealed class PlayerTravel
{
    private const float STEP_INTERVAL = 0.12f;

    private const int ALERT_RANGE = 5;

    private readonly PathFinder _pathFinder = new PathFinder();
    private readonly List<(int X, int Y)> _path = new List<(int X, int Y)>();
    private int _goalX;
    private int _goalY;

    private Interaction? _interactionOrNull;
    private float _stepTimer;
    private int _lastX;
    private int _lastY;
    private int _lastHp;
    private int _lastNearbyHostileCount;

    public bool IsActive { get; private set; }

    public bool IsPreviewing { get; private set; }

    public IReadOnlyList<(int X, int Y)> Path => _path;

    public Interaction Interaction => _interactionOrNull!.Value;

    public bool IsPreviewGoal(int x, int y)
    {
        return IsPreviewing && x == _goalX && y == _goalY;
    }

    public bool Preview(GameSession session, int goalX, int goalY, Interaction? interactionOrNull)
    {
        Actor player = session.Player;
        _goalX = goalX;
        _goalY = goalY;
        _interactionOrNull = interactionOrNull;

        bool bPathFound = _pathFinder.TryFindPath(session.Map, player.X, player.Y, goalX, goalY, _path);
        bool bInReach = interactionOrNull != null && interactionOrNull.Value.IsInReach(player);
        if (!bPathFound && !bInReach)
        {
            Cancel();
            return false;
        }

        IsActive = false;
        IsPreviewing = true;
        return true;
    }

    public void Confirm(GameSession session)
    {
        if (!IsPreviewing)
        {
            return;
        }

        Actor player = session.Player;
        IsPreviewing = false;
        IsActive = true;
        _stepTimer = 0.0f;
        _lastX = player.X;
        _lastY = player.Y;
        _lastHp = player.Hp;
        _lastNearbyHostileCount = CountNearbyHostiles(session);
    }

    public bool Start(GameSession session, int goalX, int goalY, Interaction? interactionOrNull)
    {
        if (!Preview(session, goalX, goalY, interactionOrNull))
        {
            return false;
        }

        Confirm(session);
        return true;
    }

    public void Cancel()
    {
        IsActive = false;
        IsPreviewing = false;
        _interactionOrNull = null;
        _path.Clear();
    }

    public ETravelStep NextStep(GameSession session, float deltaTime, out EPlayerCommand moveCommand)
    {
        moveCommand = EPlayerCommand.None;
        if (!IsActive)
        {
            return ETravelStep.None;
        }

        _stepTimer -= deltaTime;
        if (_stepTimer > 0.0f)
        {
            return ETravelStep.None;
        }

        _stepTimer = STEP_INTERVAL;
        Actor player = session.Player;

        if (_interactionOrNull != null)
        {
            Interaction interaction = _interactionOrNull.Value;
            if (interaction.TargetActorOrNull != null && !interaction.TargetActorOrNull.IsAlive)
            {
                Cancel();
                return ETravelStep.None;
            }

            _goalX = interaction.TargetX;
            _goalY = interaction.TargetY;
            if (interaction.IsInReach(player))
            {
                _path.Clear();
                return ETravelStep.Interact;
            }
        }

        if (!_pathFinder.TryFindPath(session.Map, player.X, player.Y, _goalX, _goalY, _path))
        {
            Cancel();
            return ETravelStep.None;
        }

        (int nextX, int nextY) = _path[0];
        if (session.Map.GetLivingActorAtOrNull(nextX, nextY) != null)
        {
            Cancel();
            return ETravelStep.None;
        }

        moveCommand = GetMoveCommand(nextX - player.X, nextY - player.Y);
        return ETravelStep.Move;
    }

    public string? CheckInterruptOrNull(GameSession session)
    {
        Actor player = session.Player;
        bool bMoved = player.X != _lastX || player.Y != _lastY;
        _lastX = player.X;
        _lastY = player.Y;

        bool bArrived = player.X == _goalX && player.Y == _goalY && _interactionOrNull == null;
        if (session.Runner.IsGameOver || bArrived || !bMoved)
        {
            Cancel();
            return null;
        }

        int hp = player.Hp;
        int nearbyHostileCount = CountNearbyHostiles(session);

        bool bAboutToInteract = _interactionOrNull != null && _interactionOrNull.Value.IsInReach(player);
        bool bHurt = hp < _lastHp && !bAboutToInteract;
        bool bHostileAppeared = nearbyHostileCount > _lastNearbyHostileCount;
        _lastHp = hp;
        _lastNearbyHostileCount = nearbyHostileCount;

        if (bHurt)
        {
            Cancel();
            return "걸음을 멈췄다. 무언가 공격해 온다!";
        }

        if (bHostileAppeared)
        {
            Cancel();
            return "걸음을 멈췄다. 근처에 몬스터가 있다.";
        }

        return null;
    }

    private int CountNearbyHostiles(GameSession session)
    {
        Actor player = session.Player;
        IReadOnlyList<Actor> actors = session.Map.Actors;
        Actor? targetOrNull = _interactionOrNull != null ? _interactionOrNull.Value.TargetActorOrNull : null;
        int count = 0;
        for (int index = 0; index < actors.Count; index++)
        {
            Actor actor = actors[index];
            if (actor != targetOrNull && actor.IsAlive && actor.Type.IsHostile && actor.Type.AttackPower > 0
                && Math.Abs(actor.X - player.X) + Math.Abs(actor.Y - player.Y) <= ALERT_RANGE)
            {
                count++;
            }
        }

        return count;
    }

    private static EPlayerCommand GetMoveCommand(int deltaX, int deltaY)
    {
        if (deltaX < 0)
        {
            return EPlayerCommand.MoveLeft;
        }

        if (deltaX > 0)
        {
            return EPlayerCommand.MoveRight;
        }

        return deltaY < 0 ? EPlayerCommand.MoveUp : EPlayerCommand.MoveDown;
    }
}
