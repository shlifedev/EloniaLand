using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public static class DebugNames
    {
        public static string GetActorName(Actor actor)
        {
            return ZString.Concat(GetActorTypeName(actor.Row()), " #", actor.ID);
        }

        public static string GetActorTypeName(ActorRow row)
        {
            return GameText.GetActorTypeName(row);
        }

        public static string GetBehaviorName(IBehavior behavior)
        {
            switch (behavior)
            {
                case FleeBehavior _:
                    return "도망";
                case ChaseBehavior _:
                    return "추격";
                case AttackBehavior _:
                    return "공격";
                case WanderBehavior _:
                    return "배회";
                default:
                    return behavior.Name;
            }
        }

        public static string GetActionName(ActorAction action)
        {
            switch (action)
            {
                case MoveAction move:
                    return ZString.Concat("이동(", GetDirectionName(move.DeltaX, move.DeltaY), ")");
                case AttackAction attack:
                    return ZString.Concat("공격: ", GetActorName(attack.Target));
                case WaitAction _:
                    return "대기";
                case PickUpAction _:
                    return "줍기";
                case HarvestAction harvest:
                    return ZString.Concat("캐기: (", harvest.TargetX, ", ", harvest.TargetY, ")");
                default:
                    return action.Id;
            }
        }

        public static string GetStatText(ActorStats stats, EStat stat)
        {
            int modifierSum = stats.GetModifierSum(stat);
            if (modifierSum == 0)
            {
                return ZString.Concat(stats.Get(stat));
            }

            return ZString.Concat(stats.Get(stat), " (기본 ", stats.GetBase(stat), ", 수정 ", modifierSum > 0 ? "+" : "", modifierSum, ")");
        }

        public static string GetStatsSummary(ActorStats stats)
        {
            using (Utf16ValueStringBuilder builder = ZString.CreateStringBuilder())
            {
                for (int index = 0; index < ActorStats.STAT_COUNT; index++)
                {
                    EStat stat = (EStat)index;
                    if (index > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(GameText.GetStatName(stat));
                    builder.Append(' ');
                    builder.Append(GetStatText(stats, stat));
                }

                return builder.ToString();
            }
        }

        public static string GetModifiersText(ActorStats stats)
        {
            IReadOnlyList<StatModifier> modifiers = stats.Modifiers;
            if (modifiers.Count == 0)
            {
                return "없음";
            }

            using (Utf16ValueStringBuilder builder = ZString.CreateStringBuilder())
            {
                for (int index = 0; index < modifiers.Count; index++)
                {
                    StatModifier modifier = modifiers[index];
                    if (index > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append(modifier.Source);
                    builder.Append(": ");
                    builder.Append(GameText.GetStatName(modifier.Stat));
                    builder.Append(modifier.Amount > 0 ? " +" : " ");
                    builder.Append(modifier.Amount);
                }

                return builder.ToString();
            }
        }

        public static string GetFloorName(FloorRow row)
        {
            return GameText.GetFloorName(row);
        }

        public static string GetPropName(PropRow row)
        {
            return GameText.GetPropName(row);
        }

        public static string GetThingName(Thing thing)
        {
            return GameText.GetThingName(thing.Row());
        }

        private static string GetDirectionName(int deltaX, int deltaY)
        {
            if (deltaY < 0)
            {
                return "위";
            }

            if (deltaY > 0)
            {
                return "아래";
            }

            return deltaX < 0 ? "왼쪽" : "오른쪽";
        }
    }
}
