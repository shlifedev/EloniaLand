using System;
using System.Collections.Generic;
using System.IO;
using Elonia.Data;

namespace Elonia.Game
{
    public sealed class GameContent
    {
        private readonly Dictionary<char, TownLegendRow> _legend = new Dictionary<char, TownLegendRow>();

        public GameTables Tables { get; }

        public static GameContent Load(string directory)
        {
            return new GameContent(GameTables.Load(directory));
        }

        public GameContent(GameTables tables)
        {
            if (tables == null)
            {
                throw new ArgumentNullException(nameof(tables));
            }

            Tables = tables;
            foreach (ThingRow row in tables.Thing.Rows)
            {
                ValidateThing(row, tables.Item);
            }

            foreach (FloorRow row in tables.Floor.Rows)
            {
                ValidateIdentity(row.Id, row.Name, "A floor type needs a name.");
            }

            foreach (PropRow row in tables.Prop.Rows)
            {
                ValidateProp(row, tables.Thing);
            }

            foreach (ActorRow row in tables.Actor.Rows)
            {
                ValidateActor(row);
            }

            foreach (TownLegendRow row in tables.TownLegend.Rows)
            {
                if (row.Symbol.Length != 1)
                {
                    throw new InvalidDataException($"TownLegend '{row.Id}'에는 한 글자 기호가 필요합니다.");
                }

                if (row.HouseStyle != EHouseStyle.None && !row.Prop.HasValue)
                {
                    throw new InvalidDataException($"TownLegend '{row.Id}'에 집 외형을 사용할 물체가 필요합니다.");
                }

                _legend.Add(row.Symbol[0], row);
            }
        }

        public TownLegendRow GetLegend(char symbol, int x, int y)
        {
            TownLegendRow? row;
            if (_legend.TryGetValue(symbol, out row))
            {
                return row;
            }

            throw new InvalidDataException($"마을 좌표 ({x}, {y})의 기호 '{symbol}'가 TownLegend에 없습니다.");
        }

        private static void ValidateIdentity(string id, string name, string nameError)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("종류 ID가 필요합니다.", nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameError, nameof(name));
            }
        }

        private static void ValidateThing(ThingRow row, ItemTable items)
        {
            if (row.StepDamage < 0)
            {
                throw new InvalidDataException($"Thing '{row.Id}'의 StepDamage는 0 이상이어야 합니다.");
            }

            if (row.Item.HasValue)
            {
                ItemRow item = items.Get(row.Item);
                if (item.Effect != EItemEffect.None && item.Effect != EItemEffect.Heal && item.Effect != EItemEffect.Gold)
                {
                    throw new InvalidDataException($"Item '{item.Id}'에 지원하지 않는 습득 효과 '{item.Effect}'가 있습니다.");
                }

                if (item.Amount <= 0)
                {
                    throw new ArgumentOutOfRangeException("amount", "An item amount must be positive.");
                }
            }

            ValidateIdentity(row.Id, row.Name, "A thing type needs a name.");
        }

        private static void ValidateProp(PropRow row, ThingTable things)
        {
            if (!row.DropThing.HasValue && row.DropAmount != 0)
            {
                throw new InvalidDataException($"Prop '{row.Id}'의 DropAmount를 사용하려면 DropThing이 필요합니다.");
            }

            if (row.DropThing.HasValue)
            {
                things.Get(row.DropThing);
                if (row.DropAmount <= 0)
                {
                    throw new ArgumentOutOfRangeException("dropCount", "A harvest must drop a positive count.");
                }
            }

            ValidateIdentity(row.Id, row.Name, "A prop type needs a name.");
            if (row.IsWalkable && row.DropThing.HasValue)
            {
                throw new ArgumentException("A harvestable prop must not be walkable.", "harvestOrNull");
            }
        }

        private static void ValidateActor(ActorRow row)
        {
            bool bNeedsRange = row.Behavior is EActorBehavior.Flee or EActorBehavior.ChaseAttack;
            if (row.SenseRange < 0 || (bNeedsRange && row.SenseRange == 0))
            {
                throw new InvalidDataException($"Actor '{row.Id}'의 행동 {row.Behavior}에 쓸 수 없는 SenseRange 값 {row.SenseRange}입니다.");
            }

            ValidateIdentity(row.Id, row.Name, "An actor type needs a name.");
            if (row.MaxHp <= 0)
            {
                throw new ArgumentOutOfRangeException("maxHp", "A max HP must be positive.");
            }

            if (row.Attack < 0)
            {
                throw new ArgumentOutOfRangeException("attackPower", "An attack power must not be negative.");
            }

            if (row.Speed <= 0)
            {
                throw new ArgumentOutOfRangeException("speed", "An actor speed must be positive.");
            }
        }
    }
}
