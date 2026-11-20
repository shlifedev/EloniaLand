using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Text;
using Elonia.Data;
using Elonia.Game;
using Elonia.Playtest;
using Elonia.Simulation;

namespace Elonia.Checks;

internal static class Program
{
    public static void Main()
    {
        GameTables tables = GameTables.Load(Path.Combine(AppContext.BaseDirectory, "Tables"));
        GameContent content = new GameContent(tables);
        CheckHouseSprites(content, tables);
        Console.WriteLine("플레이테스트 화면 검사 통과");
    }

    private static void CheckHouseSprites(GameContent content, GameTables tables)
    {
        string wood = new string(tables.TownLegend.Get("wood_house").Symbol[0], 3);
        string stone = new string(tables.TownLegend.Get("stone_house").Symbol[0], 4);
        string middle = ZString.Concat(wood, tables.TownLegend.Get("grass").Symbol, stone);
        string top = ZString.Concat(wood, tables.TownLegend.Get("hero").Symbol, stone);
        GameSession session = TownMap.Create(content, new Random(1), new string[] { top, middle, middle });
        Dictionary<(int X, int Y, IPropDef Prop), Sprite> sprites = HouseSprites.Create(session, content);
        Check(sprites.Count == 21, "나무집과 돌집의 모든 칸에 그림이 배정된다.");
        IPropDef woodHouse = tables.Prop.Get(tables.TownLegend.Get("wood_house").Prop);
        IPropDef stoneHouse = tables.Prop.Get(tables.TownLegend.Get("stone_house").Prop);
        Check(sprites[(0, 0, woodHouse)].Equals(Art.GetWoodHouseSprite(0, 0, 3)), "나무집에 나무 지붕이 표시된다.");
        Check(sprites[(7, 2, stoneHouse)].Equals(Art.GetStoneHouseSprite(3, 2, 4)), "돌집에 돌벽이 표시된다.");
    }

    private static void Check(bool bCondition, string message)
    {
        if (!bCondition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
