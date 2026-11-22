using System;
using System.Collections.Generic;

namespace Elonia.Simulation
{
    public sealed class PathFinder
    {
        private const int NO_PARENT = -1;

        private int[] _parents = Array.Empty<int>();
        private int[] _visitStamps = Array.Empty<int>();
        private int[] _queue = Array.Empty<int>();
        private int _stamp;

        public bool TryFindPath(Map map, int fromX, int fromY, int goalX, int goalY, List<(int X, int Y)> path)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!map.IsInside(fromX, fromY))
            {
                throw new ArgumentOutOfRangeException(nameof(fromX), $"({fromX}, {fromY}) is outside the map.");
            }

            if (!map.IsInside(goalX, goalY))
            {
                throw new ArgumentOutOfRangeException(nameof(goalX), $"({goalX}, {goalY}) is outside the map.");
            }

            path.Clear();
            bool bGoalEnterable = map.IsWalkable(goalX, goalY);
            int minimumDistance = bGoalEnterable ? 0 : 1;
            int bestDistance = GridMath.GetChebyshevDistance(fromX, fromY, goalX, goalY);
            if (bestDistance <= minimumDistance)
            {
                return false;
            }

            int width = map.Width;
            int startIndex = fromY * width + fromX;
            int goalIndex = goalY * width + goalX;
            PrepareBuffers(width * map.Height);

            int bestIndex = startIndex;
            int head = 0;
            int tail = 0;
            _visitStamps[startIndex] = _stamp;
            _parents[startIndex] = NO_PARENT;
            _queue[tail++] = startIndex;

            while (head < tail && bestDistance > minimumDistance)
            {
                int index = _queue[head++];
                int x = index % width;
                int y = index / width;
                for (int direction = 0; direction < GridMath.DIRECTIONS.Count; direction++)
                {
                    (int DeltaX, int DeltaY) move = GridMath.DIRECTIONS[direction];
                    int nextX = x + move.DeltaX;
                    int nextY = y + move.DeltaY;
                    if (!map.IsInside(nextX, nextY))
                    {
                        continue;
                    }

                    int nextIndex = nextY * width + nextX;
                    if (_visitStamps[nextIndex] == _stamp)
                    {
                        continue;
                    }

                    bool bGoal = nextIndex == goalIndex;
                    if (!map.CanEnter(nextX, nextY) && !(bGoal && bGoalEnterable))
                    {
                        continue;
                    }

                    if (!map.IsCornerClear(x, y, move.DeltaX, move.DeltaY))
                    {
                        continue;
                    }

                    _visitStamps[nextIndex] = _stamp;
                    _parents[nextIndex] = index;
                    int distance = GridMath.GetChebyshevDistance(nextX, nextY, goalX, goalY);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = nextIndex;
                    }

                    if (!bGoal)
                    {
                        _queue[tail++] = nextIndex;
                    }
                }
            }

            if (bestIndex == startIndex)
            {
                return false;
            }

            for (int index = bestIndex; index != startIndex; index = _parents[index])
            {
                path.Add((index % width, index / width));
            }

            path.Reverse();
            return true;
        }

        private void PrepareBuffers(int tileCount)
        {
            if (_parents.Length != tileCount)
            {
                _parents = new int[tileCount];
                _visitStamps = new int[tileCount];
                _queue = new int[tileCount];
                _stamp = 0;
            }

            _stamp++;
            if (_stamp == int.MaxValue)
            {
                Array.Clear(_visitStamps, 0, _visitStamps.Length);
                _stamp = 1;
            }
        }
    }
}
