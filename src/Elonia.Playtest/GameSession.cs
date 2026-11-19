using System.Collections.Generic;
using Elonia.Simulation;

namespace Elonia.Playtest;

internal sealed class GameSession
{
    public GameRunner Runner { get; }
    public World World => Runner.World;

    public Map Map => World.CurrentMap;
    public Actor Player => Runner.Player;

    public IReadOnlyDictionary<(int X, int Y, PropType Prop), Sprite> BuildingSprites { get; }

    public GameSession(GameRunner runner, IReadOnlyDictionary<(int X, int Y, PropType Prop), Sprite> buildingSprites)
    {
        Runner = runner;
        BuildingSprites = buildingSprites;
    }
}
