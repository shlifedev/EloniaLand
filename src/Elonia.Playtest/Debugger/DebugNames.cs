using Cysharp.Text;
using Elonia.Simulation;

namespace Elonia.Playtest;

internal static class DebugNames
{
    public static string GetActorName(Actor actor)
    {
        return ZString.Concat(GetActorTypeName(actor.Type), " #", actor.ID);
    }

    public static string GetActorTypeName(ActorType type)
    {
        if (type == GameContent.HERO)
        {
            return "영웅";
        }

        if (type == GameContent.VILLAGER)
        {
            return "마을 사람";
        }

        if (type == GameContent.RAT)
        {
            return "쥐";
        }

        if (type == GameContent.SLIME)
        {
            return "슬라임";
        }

        if (type == GameContent.BAT)
        {
            return "박쥐";
        }

        return type.Name;
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
            default:
                return action.Name;
        }
    }

    public static string GetTileName(TileType type)
    {
        if (type == GameContent.GRASS)
        {
            return "잔디";
        }

        if (type == GameContent.PATH)
        {
            return "돌길";
        }

        if (type == GameContent.DIRT)
        {
            return "흙";
        }

        if (type == GameContent.GATE)
        {
            return "울타리 문";
        }

        if (type == GameContent.TREE)
        {
            return "나무";
        }

        if (type == GameContent.BUSH)
        {
            return "덤불";
        }

        if (type == GameContent.FENCE)
        {
            return "울타리";
        }

        if (type == GameContent.BUILDING)
        {
            return "건물";
        }

        if (type == GameContent.WELL)
        {
            return "우물";
        }

        if (type == GameContent.SIGN)
        {
            return "표지판";
        }

        return type.Name;
    }

    public static string GetItemName(Item item)
    {
        switch (item.Type)
        {
            case EItemType.Potion:
                return "물약";
            case EItemType.Gold:
                return "골드";
            default:
                return item.Type.ToString();
        }
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
