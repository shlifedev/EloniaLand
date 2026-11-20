using System;
using System.Collections.Generic;
using Elonia.Data;
using Elonia.Game;
using Elonia.Simulation;

namespace Elonia.Playtest;

internal static class HouseSprites
{
    public static Dictionary<(int X, int Y, PropType Prop), Sprite> Create(GameSession session, GameContent content)
    {
        Dictionary<(int X, int Y, PropType Prop), Sprite> sprites = new Dictionary<(int X, int Y, PropType Prop), Sprite>();
        Map map = session.Map;
        for (int y = 0; y < map.Height; y++)
        {
            int x = 0;
            while (x < map.Width)
            {
                EHouseStyle style = session.GetLegend(x, y).HouseStyle;
                if (style == EHouseStyle.None)
                {
                    x++;
                    continue;
                }

                int left = x;
                while (x < map.Width && session.GetLegend(x, y).HouseStyle == style)
                {
                    x++;
                }

                int top = y;
                while (top > 0 && session.GetLegend(left, top - 1).HouseStyle == style)
                {
                    top--;
                }

                int houseWidth = x - left;
                for (int column = 0; column < houseWidth; column++)
                {
                    Sprite sprite = style switch
                    {
                        EHouseStyle.Wood => Art.GetWoodHouseSprite(column, y - top, houseWidth),
                        EHouseStyle.Stone => Art.GetStoneHouseSprite(column, y - top, houseWidth),
                        _ => throw new InvalidOperationException($"({left + column}, {y})에 지원하지 않는 집 외형 '{style}'이 있습니다."),
                    };
                    PropType prop = content.GetProp(session.GetLegend(left + column, y).Prop.Key);
                    sprites.Add((left + column, y, prop), sprite);
                }
            }
        }

        return sprites;
    }
}
