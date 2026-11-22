using System;
using System.Collections.Generic;

namespace Elonia.Simulation
{
    public static class GridMath
    {
        public static readonly IReadOnlyList<(int DeltaX, int DeltaY)> DIRECTIONS =
            Array.AsReadOnly(new (int, int)[] { (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1) });

        public static int GetChebyshevDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Max(Math.Abs(x1 - x2), Math.Abs(y1 - y2));
        }

        public static bool IsInReach(int fromX, int fromY, int toX, int toY, int reach)
        {
            return GetChebyshevDistance(fromX, fromY, toX, toY) == reach;
        }

        public static bool IsDiagonal(int deltaX, int deltaY)
        {
            return deltaX != 0 && deltaY != 0;
        }

        public static long GetSquaredDistance(int x1, int y1, int x2, int y2)
        {
            long deltaX = x1 - x2;
            long deltaY = y1 - y2;
            return deltaX * deltaX + deltaY * deltaY;
        }
    }
}
