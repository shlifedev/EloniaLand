namespace Elonia.Simulation
{
    internal static class GridMath
    {
        public static long GetSquaredDistance(int x1, int y1, int x2, int y2)
        {
            long deltaX = x1 - x2;
            long deltaY = y1 - y2;
            return deltaX * deltaX + deltaY * deltaY;
        }
    }
}
