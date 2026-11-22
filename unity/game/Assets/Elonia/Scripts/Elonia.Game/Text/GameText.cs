using Cysharp.Text;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public static class GameText
    {
        private const char FIRST_HANGUL_SYLLABLE = '가';
        private const char LAST_HANGUL_SYLLABLE = '힣';
        private const int FINAL_CONSONANT_COUNT = 28;

        public static string GetActorTypeName(ActorRow row)
        {
            return row.Id switch
            {
                "hero" => "영웅",
                "villager" => "마을 사람",
                "rat" => "쥐",
                "slime" => "슬라임",
                "bat" => "박쥐",
                _ => row.Name
            };
        }

        public static string GetStatName(EStat stat)
        {
            return stat switch
            {
                EStat.MaxHp => "최대 체력",
                EStat.Attack => "공격력",
                EStat.Speed => "속도",
                _ => stat.ToString()
            };
        }

        public static string GetFloorName(FloorRow row)
        {
            return row.Id switch
            {
                "grass" => "잔디",
                "path" => "돌길",
                "dirt" => "흙",
                "wood_floor" => "나무 바닥",
                _ => row.Name
            };
        }

        public static string GetPropName(PropRow row)
        {
            return row.Id switch
            {
                "tree" => "나무",
                "bush" => "덤불",
                "fence" => "울타리",
                "gate" => "울타리 문",
                "building" => "건물",
                "well" => "우물",
                "sign" => "표지판",
                "wall" => "벽",
                "door" => "문",
                _ => row.Name
            };
        }

        public static string GetThingName(ThingRow row)
        {
            return row.Id switch
            {
                "potion" => "물약",
                "gold" => "골드",
                "branch" => "나뭇가지",
                "spike_trap" => "가시 함정",
                _ => row.Name
            };
        }

        public static string GetPickUpLabel(ThingRow row)
        {
            if (row.Id == "potion")
            {
                return "물약 마시기";
            }

            return ZString.Concat(GetThingName(row), " 줍기");
        }

        public static string GetHarvestLabel(PropRow prop)
        {
            if (prop.Id == "tree")
            {
                return "나무 베기";
            }

            return ZString.Concat(GetPropName(prop), " 캐기");
        }

        public static string GetInteractionLabel(Interaction interaction, Map map)
        {
            switch (interaction.Action)
            {
                case AttackAction attack:
                    return ZString.Concat(GetActorTypeName(attack.Target.Row()), " 공격");
                case TalkAction talk:
                    return ZString.Concat(GetActorTypeName(talk.Target.Row()), "에게 말 걸기");
                case HarvestAction harvest:
                    PropRow? propOrNull = map.GetPropOrNull(harvest.TargetX, harvest.TargetY)?.Row();
                    return propOrNull != null ? GetHarvestLabel(propOrNull) : "캐기";
                case PickUpAction _:
                    Thing? itemOrNull = map.GetItemAtOrNull(interaction.TargetX, interaction.TargetY);
                    return itemOrNull != null ? GetPickUpLabel(itemOrNull.Row()) : "줍기";
                default:
                    return interaction.Action.Id;
            }
        }

        public static string WithObjectParticle(string word)
        {
            return ZString.Concat(word, GetObjectParticle(word));
        }

        public static string WithSubjectParticle(string word)
        {
            return ZString.Concat(word, GetSubjectParticle(word));
        }

        internal static string GetObjectParticle(string word)
        {
            return GetParticle(word, "을", "를", "을(를)");
        }

        internal static string GetSubjectParticle(string word)
        {
            return GetParticle(word, "이", "가", "이(가)");
        }

        private static string GetParticle(string word, string afterConsonant, string afterVowel, string fallback)
        {
            char last = word.Length > 0 ? word[word.Length - 1] : ' ';
            if (last < FIRST_HANGUL_SYLLABLE || last > LAST_HANGUL_SYLLABLE)
            {
                return fallback;
            }

            bool bHasFinalConsonant = (last - FIRST_HANGUL_SYLLABLE) % FINAL_CONSONANT_COUNT != 0;
            return bHasFinalConsonant ? afterConsonant : afterVowel;
        }
    }
}
