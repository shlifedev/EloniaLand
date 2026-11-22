using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public static class ActorFactory
    {
        public static Actor Spawn(Map map, ActorRow row, int x, int y, IActorController? controllerOrNull = null)
        {
            return map.AddEntity(new Actor(map.CreateEntityID(), row, new ActorStats(row), x, y, controllerOrNull));
        }
    }
}
