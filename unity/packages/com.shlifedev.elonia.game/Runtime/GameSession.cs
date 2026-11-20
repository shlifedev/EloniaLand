using System;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class GameSession
    {
        private readonly TownLegendRow[,] _layout;

        public GameRunner Runner { get; }
        public World World => Runner.World;

        public Map Map => World.CurrentMap;
        public Actor Player => Runner.Player;

        public GameSession(GameRunner runner, TownLegendRow[,] layout)
        {
            if (runner == null)
            {
                throw new ArgumentNullException(nameof(runner));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (layout.GetLength(0) != runner.World.CurrentMap.Height || layout.GetLength(1) != runner.World.CurrentMap.Width)
            {
                throw new ArgumentException("범례 배치의 크기가 맵과 다릅니다.", nameof(layout));
            }

            Runner = runner;
            _layout = layout;
        }

        public TownLegendRow GetLegend(int x, int y)
        {
            return _layout[y, x];
        }
    }
}
