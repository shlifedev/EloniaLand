using System;
using System.Collections.Generic;
using Elonia.Data;
using Elonia.Simulation;

namespace Elonia.Game
{
    public static class TownMap
    {
        private static readonly string[] ROWS = new string[]
        {
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TT....T.......TT............T.........b.....TTTT",
            "TT..AAA....BBBB.........$.........T.........x.TT",
            "TT..AAA....BBBB..T..............s.......T.....TT",
            "TT..AAA....BBBB.........b...................$.TT",
            "TT...=......=.......m..........T..............TT",
            "TT...=......=................................TTT",
            "TT...========.....T......$.........::::::....TTT",
            "TT....v..W.=..............s........::::::..s..TT",
            "TT.........=.S.....b...............::::::.....TT",
            "TT.#####...=...................T...::::::.....TT",
            "TT.#:::#...=........m..................!..x...TT",
            "TT.#:::#...==========.................b.......TT",
            "TT.#:::#...=........=.........T...............TT",
            "TT.##G##...=...AAA..=....!..............s.....TT",
            "TT.........=...AAA..=.......^.................TT",
            "TT....@....=...AAA..=......b.........T.....$..TT",
            "TT......^..=....=...=.........^.......^.......TT",
            "TT..b......======...=....T.......s............TT",
            "TT..wwwwwwwww.......=..............m......T...TT",
            "TTT.w,,,w,,,w..v....=.....$...................TT",
            "TTT.w,,,w,,,w.......=............b....x.......TT",
            "TT..w,,,,,,,w..$....=......T..................TT",
            "TT..w,,,,,,,w.......=..................$......TT",
            "TT..wwDwwwwww.......=.......m.......T.........TT",
            "TT......!.....T.....=.....................!...TT",
            "TT..................=..........b..............TT",
            "TTTTTTTTTTTTTTTTTTTT=TTTTTTTTTTTTTTTTTTTTTTTTTTT",
            "TTTTTTTTTTTTTTTTTTTT=TTTTTTTTTTTTTTTTTTTTTTTTTTT",
        };

        public static GameSession Create(GameContent content, Random random)
        {
            return Create(content, random, ROWS);
        }

        public static GameSession Create(GameContent content, Random random, IReadOnlyList<string> rows)
        {
            if (rows.Count == 0 || rows[0].Length == 0)
            {
                throw new InvalidOperationException("마을 지도에는 한 칸 이상 있어야 합니다.");
            }

            Map map = new Map(rows[0].Length, rows.Count);
            TownLegendRow[,] layout = new TownLegendRow[map.Height, map.Width];
            Actor player = PlaceTerrainAndPlayer(content, rows, map, layout);
            PlaceNpcs(content, layout, map, player, random);
            PlaceThings(content, layout, map);

            return new GameSession(new GameRunner(new World(map, content.Tables), player), layout);
        }

        private static Actor PlaceTerrainAndPlayer(GameContent content, IReadOnlyList<string> rows, Map map, TownLegendRow[,] layout)
        {
            Actor? playerOrNull = null;
            for (int y = 0; y < map.Height; y++)
            {
                if (rows[y].Length != map.Width)
                {
                    throw new InvalidOperationException($"마을 지도의 {y}행 너비가 {map.Width}와 다릅니다.");
                }

                for (int x = 0; x < map.Width; x++)
                {
                    TownLegendRow legend = content.GetLegend(rows[y][x], x, y);
                    layout[y, x] = legend;
                    map.SetFloor(x, y, content.Tables.Floor.Get(legend.Floor));
                    if (legend.Prop.HasValue)
                    {
                        map.SetProp(x, y, content.Tables.Prop.Get(legend.Prop));
                    }

                    if (legend.Actor.HasValue)
                    {
                        ActorRow row = content.Tables.Actor.Get(legend.Actor);
                        if (row.Behavior != EActorBehavior.Player)
                        {
                            continue;
                        }

                        if (playerOrNull != null)
                        {
                            throw new InvalidOperationException($"마을 지도에 플레이어 시작 지점이 둘 이상입니다. ({playerOrNull.X}, {playerOrNull.Y}), ({x}, {y})");
                        }

                        playerOrNull = map.AddEntity(new Actor(map.CreateEntityID(), row, x, y));
                    }
                }
            }

            return playerOrNull ?? throw new InvalidOperationException("마을 지도에 플레이어 시작 지점이 없습니다.");
        }

        private static void PlaceNpcs(GameContent content, TownLegendRow[,] layout, Map map, Actor player, Random random)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    ActorRef actor = layout[y, x].Actor;
                    if (!actor.HasValue)
                    {
                        continue;
                    }

                    ActorRow row = content.Tables.Actor.Get(actor);
                    if (row.Behavior != EActorBehavior.Player)
                    {
                        Brain brain = CreateBrain(row, player, random);
                        map.AddEntity(new Actor(map.CreateEntityID(), row, x, y, brain));
                    }
                }
            }
        }

        private static void PlaceThings(GameContent content, TownLegendRow[,] layout, Map map)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    ThingRef thing = layout[y, x].Thing;
                    if (thing.HasValue)
                    {
                        map.AddEntity(new Thing(map.CreateEntityID(), content.Tables.Thing.Get(thing), x, y));
                    }
                }
            }
        }

        private static Brain CreateBrain(ActorRow row, Actor player, Random random)
        {
            switch (row.Behavior)
            {
                case EActorBehavior.Wander:
                    return new Brain(new WanderBehavior(random));
                case EActorBehavior.Flee:
                    return new Brain(
                        new FleeBehavior(player, row.SenseRange),
                        new WanderBehavior(random));
                case EActorBehavior.ChaseAttack:
                    return new Brain(
                        new AttackBehavior(player),
                        new ChaseBehavior(player, row.SenseRange),
                        new WanderBehavior(random));
                default:
                    throw new InvalidOperationException($"Actor 표의 '{row.Id}'에 지원하지 않는 NPC 행동 '{row.Behavior}'이 있습니다.");
            }
        }
    }
}
