using System;
using System.Collections.Generic;
using System.IO;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public sealed class GameContent
    {
        private readonly Dictionary<string, ThingType> _things = new Dictionary<string, ThingType>(StringComparer.Ordinal);
        private readonly Dictionary<string, FloorType> _floors = new Dictionary<string, FloorType>(StringComparer.Ordinal);
        private readonly Dictionary<string, PropType> _props = new Dictionary<string, PropType>(StringComparer.Ordinal);
        private readonly Dictionary<string, ActorType> _actors = new Dictionary<string, ActorType>(StringComparer.Ordinal);
        private readonly Dictionary<char, TownLegendRow> _legend = new Dictionary<char, TownLegendRow>();
        private readonly ActorTable _actorTable;

        public static GameContent Load(string directory)
        {
            return new GameContent(GameTables.Load(directory));
        }

        public GameContent(GameTables tables)
        {
            _actorTable = tables.Actor;
            foreach (ThingRow row in tables.Thing.Rows)
            {
                _things.Add(row.Id, CreateThingType(row, tables.Item));
            }

            foreach (FloorRow row in tables.Floor.Rows)
            {
                _floors.Add(row.Id, new FloorType(row.Id, row.Name, row.IsWalkable));
            }

            foreach (PropRow row in tables.Prop.Rows)
            {
                if (!row.DropThing.HasValue && row.DropAmount != 0)
                {
                    throw new InvalidDataException($"Prop '{row.Id}'의 DropAmount를 사용하려면 DropThing이 필요합니다.");
                }

                PropHarvest? harvestOrNull = row.DropThing.HasValue ? new PropHarvest(_things[row.DropThing.Key], row.DropAmount) : null;
                _props.Add(row.Id, new PropType(row.Id, row.Name, row.IsWalkable, harvestOrNull));
            }

            foreach (ActorRow row in tables.Actor.Rows)
            {
                bool bNeedsRange = row.Behavior is EActorBehavior.Flee or EActorBehavior.ChaseAttack;
                if (row.SenseRange < 0 || (bNeedsRange && row.SenseRange == 0))
                {
                    throw new InvalidDataException($"Actor '{row.Id}'의 행동 {row.Behavior}에 쓸 수 없는 SenseRange 값 {row.SenseRange}입니다.");
                }

                _actors.Add(row.Id, new ActorType(row.Id, row.Name, row.MaxHp, row.Attack, row.Speed, row.IsHostile));
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

        public ThingType GetThing(string id) => _things[id];
        public FloorType GetFloor(string id) => _floors[id];
        public PropType GetProp(string id) => _props[id];
        public ActorType GetActor(string id) => _actors[id];
        public ActorRow GetActorRow(string id) => _actorTable.Get(id);

        public TownLegendRow GetLegend(char symbol, int x, int y)
        {
            if (_legend.TryGetValue(symbol, out TownLegendRow? row))
            {
                return row;
            }

            throw new InvalidDataException($"마을 좌표 ({x}, {y})의 기호 '{symbol}'가 TownLegend에 없습니다.");
        }

        private static ThingType CreateThingType(ThingRow row, ItemTable items)
        {
            if (row.StepDamage < 0)
            {
                throw new InvalidDataException($"Thing '{row.Id}'의 StepDamage는 0 이상이어야 합니다.");
            }

            List<IThingPart> parts = new List<IThingPart>();
            if (row.Item.HasValue)
            {
                ItemRow item = items.Get(row.Item);
                Simulation.EItemEffect effect = item.Effect switch
                {
                    Data.EItemEffect.None => Simulation.EItemEffect.None,
                    Data.EItemEffect.Heal => Simulation.EItemEffect.Heal,
                    Data.EItemEffect.Gold => Simulation.EItemEffect.Gold,
                    _ => throw new InvalidDataException($"Item '{item.Id}'에 지원하지 않는 습득 효과 '{item.Effect}'가 있습니다.")
                };
                parts.Add(new ItemPart(effect, item.Amount));
            }

            if (row.StepDamage > 0)
            {
                parts.Add(new StepTriggerPart(new DamageEffect(row.StepDamage)));
            }

            return new ThingType(row.Id, row.Name, parts.ToArray());
        }
    }
}
