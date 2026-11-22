using System.Diagnostics.CodeAnalysis;
using Elonia.Simulation;

namespace Elonia.Game
{
    internal static class InteractionTargets
    {
        public static bool TryGetOtherLivingActor(Map map, Actor actor, int x, int y, [NotNullWhen(true)] out Actor? targetOrNull)
        {
            targetOrNull = map.GetLivingActorAtOrNull(x, y);
            if (targetOrNull == actor)
            {
                targetOrNull = null;
            }

            return targetOrNull != null;
        }
    }
}
