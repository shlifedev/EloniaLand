using System;
using System.Collections.Generic;
using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class PlayerTravel
    {
        private readonly PathFinder _pathFinder = new PathFinder();
        private readonly ThreatWatch _threatWatch = new ThreatWatch();
        private readonly List<(int X, int Y)> _path = new List<(int X, int Y)>();
        private int _goalX;
        private int _goalY;

        private Interaction? _interactionOrNull;
        private int _lastX;
        private int _lastY;

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
            _lastX = player.X;
            _lastY = player.Y;
            _threatWatch.Reset(session, _interactionOrNull?.TargetActorOrNull);
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

        public ETravelStep NextStep(GameSession session, out EDirection direction)
        {
            direction = default;
            if (!IsActive)
            {
                return ETravelStep.None;
            }

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

            direction = GetDirection(nextX - player.X, nextY - player.Y);
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

            _threatWatch.Update(session, out bool bHurt, out bool bHostileAppeared);

            bool bAboutToInteract = _interactionOrNull != null && _interactionOrNull.Value.IsInReach(player);
            if (bHurt && !bAboutToInteract)
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

        private static EDirection GetDirection(int deltaX, int deltaY)
        {
            return (deltaX, deltaY) switch
            {
                (0, -1) => EDirection.Up,
                (0, 1) => EDirection.Down,
                (-1, 0) => EDirection.Left,
                (1, 0) => EDirection.Right,
                (-1, -1) => EDirection.UpLeft,
                (1, -1) => EDirection.UpRight,
                (-1, 1) => EDirection.DownLeft,
                (1, 1) => EDirection.DownRight,
                _ => throw new InvalidOperationException("경로의 다음 칸이 이웃 칸이 아닙니다.")
            };
        }
    }
}
