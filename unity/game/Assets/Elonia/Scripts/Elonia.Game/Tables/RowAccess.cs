using System;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public static class RowAccess
    {
        public static ActorRow Row(this Actor actor) => (ActorRow)actor.Def;
        public static ThingRow Row(this Thing thing) => thing.Def.Row();
        public static ThingRow Row(this IThingDef def) => (ThingRow)def;
        public static FloorRow Row(this IFloorDef def) => (FloorRow)def;
        public static PropRow Row(this IPropDef def) => (PropRow)def;

        public static GameTables Tables(this World world)
        {
            if (world.GameDataOrNull is GameTables tables)
            {
                return tables;
            }

            throw new InvalidOperationException("이 월드에 게임 표가 설정되지 않았습니다.");
        }
    }
}
