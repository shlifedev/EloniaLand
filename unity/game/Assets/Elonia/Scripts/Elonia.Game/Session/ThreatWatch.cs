using System.Collections.Generic;
using Elonia.Simulation;

namespace Elonia.Game
{
    internal sealed class ThreatWatch
    {
        private const int ALERT_RANGE = 5;

        private int _lastHp;
        private int _lastNearbyThreatCount;

        private Actor? _ignoredActorOrNull;

        public void Reset(GameSession session, Actor? ignoredActorOrNull)
        {
            _ignoredActorOrNull = ignoredActorOrNull;
            _lastHp = session.Player.Hp;
            _lastNearbyThreatCount = CountNearbyThreats(session);
        }

        public void Update(GameSession session, out bool bHurt, out bool bThreatAppeared)
        {
            int hp = session.Player.Hp;
            int nearbyThreatCount = CountNearbyThreats(session);
            bHurt = hp < _lastHp;
            bThreatAppeared = nearbyThreatCount > _lastNearbyThreatCount;
            _lastHp = hp;
            _lastNearbyThreatCount = nearbyThreatCount;
        }

        private int CountNearbyThreats(GameSession session)
        {
            Actor player = session.Player;
            IReadOnlyList<Actor> actors = session.Map.Actors;
            int count = 0;
            for (int index = 0; index < actors.Count; index++)
            {
                Actor actor = actors[index];
                if (actor != _ignoredActorOrNull && actor.IsAlive && actor.IsThreat()
                    && GridMath.GetChebyshevDistance(actor.X, actor.Y, player.X, player.Y) <= ALERT_RANGE)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
