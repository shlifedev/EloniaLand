using Cysharp.Text;
using Elonia.Simulation;

namespace Elonia.Playtest;

internal static class GameText
{
    private const char FIRST_HANGUL_SYLLABLE = '가';
    private const char LAST_HANGUL_SYLLABLE = '힣';
    private const int FINAL_CONSONANT_COUNT = 28;

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

    public static string GetItemName(EItemType type)
    {
        switch (type)
        {
            case EItemType.Potion:
                return "물약";
            case EItemType.Gold:
                return "골드";
            case EItemType.Branch:
                return "나뭇가지";
            default:
                return type.ToString();
        }
    }

    public static string GetPickUpLabel(EItemType type)
    {
        if (type == EItemType.Potion)
        {
            return "물약 마시기";
        }

        return ZString.Concat(GetItemName(type), " 줍기");
    }

    public static string GetHarvestLabel(TileType tile)
    {
        if (tile == GameContent.TREE)
        {
            return "나무 베기";
        }

        return ZString.Concat(GetTileName(tile), " 캐기");
    }

    public static string GetInteractionLabel(Interaction interaction, Map map)
    {
        switch (interaction.Action)
        {
            case AttackAction attack:
                return ZString.Concat(GetActorTypeName(attack.Target.Type), " 공격");
            case HarvestAction harvest:
                return GetHarvestLabel(map.GetTile(harvest.TargetX, harvest.TargetY));
            case PickUpAction _:
                Item? itemOrNull = map.GetItemAtOrNull(interaction.TargetX, interaction.TargetY);
                return itemOrNull != null ? GetPickUpLabel(itemOrNull.Type) : "줍기";
            default:
                return interaction.Action.Name;
        }
    }

    public static string WithObjectParticle(string word)
    {
        return AppendParticle(word, "을", "를");
    }

    public static string WithSubjectParticle(string word)
    {
        return AppendParticle(word, "이", "가");
    }

    private static string AppendParticle(string word, string afterConsonant, string afterVowel)
    {
        char last = word.Length > 0 ? word[word.Length - 1] : ' ';
        if (last < FIRST_HANGUL_SYLLABLE || last > LAST_HANGUL_SYLLABLE)
        {
            return ZString.Concat(word, afterConsonant, "(", afterVowel, ")");
        }

        bool bHasFinalConsonant = (last - FIRST_HANGUL_SYLLABLE) % FINAL_CONSONANT_COUNT != 0;
        return ZString.Concat(word, bHasFinalConsonant ? afterConsonant : afterVowel);
    }
}
