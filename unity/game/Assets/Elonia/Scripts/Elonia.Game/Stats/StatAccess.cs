using System;
using Elonia.Simulation;

namespace Elonia.Game
{
    public static class StatAccess
    {
        public static ActorStats GameStats(this Actor actor)
        {
            if (actor.Stats is ActorStats stats)
            {
                return stats;
            }

            throw new InvalidOperationException("이 액터에는 게임 능력치(ActorStats)가 없습니다. ActorFactory로 만드세요.");
        }

        public static int Stat(this Actor actor, EStat stat)
        {
            return actor.GameStats().Get(stat);
        }

        public static void SetStatModifier(this Actor actor, string source, EStat stat, int amount)
        {
            actor.GameStats().SetModifier(source, stat, amount);
            actor.ClampHpToMax();
        }

        public static void RemoveStatModifiers(this Actor actor, string source)
        {
            actor.GameStats().RemoveModifiers(source);
            actor.ClampHpToMax();
        }

        public static bool IsThreat(this Actor actor)
        {
            return actor.Row().IsHostile && actor.Stat(EStat.Attack) > 0;
        }
    }
}
