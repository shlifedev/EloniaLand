using System;
using System.Collections.Generic;
using Elonia.Simulation;

namespace Elonia.Playtest;

internal static class TownMap
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
        "TT.........=...AAA..=.........................TT",
        "TT....@....=...AAA..=......b.........T.....$..TT",
        "TT.........=....=...=.........................TT",
        "TT..b......======...=....T.......s............TT",
        "TT..................=..............m......T...TT",
        "TTT....v............=.....$...................TT",
        "TTT.......T.........=............b....x.......TT",
        "TT....$.............=......T..................TT",
        "TT.........b........=..................$......TT",
        "TT..T...............=.......m.......T.........TT",
        "TT......!.....T.....=.....................!...TT",
        "TT..................=..........b..............TT",
        "TTTTTTTTTTTTTTTTTTTT=TTTTTTTTTTTTTTTTTTTTTTTTTTT",
        "TTTTTTTTTTTTTTTTTTTT=TTTTTTTTTTTTTTTTTTTTTTTTTTT",
    };

    private const int HOUSE_HEIGHT = 3;

    public static GameSession Create(Random random)
    {
        int width = ROWS[0].Length;
        int height = ROWS.Length;
        Map map = new Map(width, height);
        Dictionary<(int X, int Y), Sprite> buildingSprites = new Dictionary<(int X, int Y), Sprite>();

        for (int y = 0; y < height; y++)
        {
            if (ROWS[y].Length != width)
            {
                throw new InvalidOperationException($"Map row {y} must be {width} tiles wide.");
            }

            for (int x = 0; x < width; x++)
            {
                map.SetTile(x, y, GetTileType(ROWS[y][x]));
                if (IsHouseTopLeft(x, y))
                {
                    AddHouseSprites(buildingSprites, x, y);
                }
            }
        }

        int nextActorID = 1;
        Actor? playerOrNull = null;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (ROWS[y][x] == '@')
                {
                    playerOrNull = map.AddActor(new Actor(nextActorID, GameContent.HERO, x, y));
                    nextActorID++;
                }
            }
        }

        if (playerOrNull == null)
        {
            throw new InvalidOperationException("The map needs a player start (@).");
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                char symbol = ROWS[y][x];
                Brain? brainOrNull = CreateBrainOrNull(symbol, playerOrNull, random);
                if (brainOrNull != null)
                {
                    map.AddActor(new Actor(nextActorID, GetNpcType(symbol), x, y, brainOrNull));
                    nextActorID++;
                }
                else if (symbol == '!')
                {
                    map.AddItem(new Item(EItemType.Potion, x, y, GameContent.POTION_HEAL));
                }
                else if (symbol == '$')
                {
                    map.AddItem(new Item(EItemType.Gold, x, y, GameContent.GOLD_AMOUNT));
                }
            }
        }

        return new GameSession(new GameRunner(new World(map), playerOrNull), buildingSprites);
    }

    private static TileType GetTileType(char symbol)
    {
        switch (symbol)
        {
            case '=':
                return GameContent.PATH;
            case ':':
                return GameContent.DIRT;
            case 'G':
                return GameContent.GATE;
            case 'T':
                return GameContent.TREE;
            case 'b':
                return GameContent.BUSH;
            case '#':
                return GameContent.FENCE;
            case 'A':
            case 'B':
                return GameContent.BUILDING;
            case 'W':
                return GameContent.WELL;
            case 'S':
                return GameContent.SIGN;
            default:
                return GameContent.GRASS;
        }
    }

    private static Brain? CreateBrainOrNull(char symbol, Actor player, Random random)
    {
        switch (symbol)
        {
            case 'v':
                return new Brain(new WanderBehavior(random));
            case 'm':
                return new Brain(
                    new FleeBehavior(player, GameContent.RAT_SENSE_RANGE),
                    new WanderBehavior(random));
            case 's':
                return new Brain(
                    new AttackBehavior(player),
                    new ChaseBehavior(player, GameContent.SLIME_SENSE_RANGE),
                    new WanderBehavior(random));
            case 'x':
                return new Brain(
                    new AttackBehavior(player),
                    new ChaseBehavior(player, GameContent.BAT_SENSE_RANGE),
                    new WanderBehavior(random));
            default:
                return null;
        }
    }

    private static ActorType GetNpcType(char symbol)
    {
        switch (symbol)
        {
            case 'v':
                return GameContent.VILLAGER;
            case 'm':
                return GameContent.RAT;
            case 's':
                return GameContent.SLIME;
            default:
                return GameContent.BAT;
        }
    }

    private static bool IsHouse(int x, int y, char symbol)
    {
        return x >= 0 && y >= 0 && y < ROWS.Length && x < ROWS[y].Length && ROWS[y][x] == symbol;
    }

    private static bool IsHouseTopLeft(int x, int y)
    {
        char symbol = ROWS[y][x];
        if (symbol != 'A' && symbol != 'B')
        {
            return false;
        }

        return !IsHouse(x - 1, y, symbol) && !IsHouse(x, y - 1, symbol);
    }

    private static void AddHouseSprites(Dictionary<(int X, int Y), Sprite> sprites, int left, int top)
    {
        char symbol = ROWS[top][left];
        int width = 0;
        while (IsHouse(left + width, top, symbol))
        {
            width++;
        }

        for (int row = 0; row < HOUSE_HEIGHT; row++)
        {
            for (int column = 0; column < width; column++)
            {
                Sprite sprite = symbol == 'A'
                    ? Art.GetWoodHouseSprite(column, row, width)
                    : Art.GetStoneHouseSprite(column, row, width);
                sprites.Add((left + column, top + row), sprite);
            }
        }
    }
}
