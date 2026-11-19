using Elonia.Simulation;

namespace Elonia.Playtest;

internal static class Art
{
    public static readonly Sprite POTION = Sprite.Dungeon(115);
    public static readonly Sprite GOLD = Sprite.Town(93);
    public static readonly Sprite BRANCH = Sprite.Town(106);
    public static readonly Sprite SPIKE_TRAP = Sprite.Dungeon(41);

    private static readonly Sprite GRASS_PLAIN = Sprite.Town(0);
    private static readonly Sprite GRASS_TUFTS = Sprite.Town(1);
    private static readonly Sprite GRASS_FLOWERS = Sprite.Town(2);
    private static readonly Sprite PATH = Sprite.Town(43);

    private static readonly Sprite WOOD_FLOOR = Sprite.Town(73);
    private static readonly Sprite BUSH = Sprite.Town(5);
    private static readonly Sprite WELL = Sprite.Town(104);
    private static readonly Sprite SIGN = Sprite.Town(83);
    private static readonly Sprite GATE = Sprite.Town(69);

    private const int DIRT_TOP_LEFT = 12;
    private const int SHEET_COLUMN_COUNT = 12;

    private static readonly Sprite TALL_GREEN_TREE_TOP = Sprite.Town(4);
    private static readonly Sprite TALL_GREEN_TREE_BASE = Sprite.Town(16);
    private static readonly Sprite TALL_AUTUMN_TREE_TOP = Sprite.Town(3);
    private static readonly Sprite TALL_AUTUMN_TREE_BASE = Sprite.Town(15);
    private static readonly Sprite SMALL_GREEN_TREE = Sprite.Town(28);
    private static readonly Sprite SMALL_AUTUMN_TREE = Sprite.Town(27);

    private static readonly Sprite HERO = Sprite.Dungeon(98);
    private static readonly Sprite RAT = Sprite.Dungeon(123);
    private static readonly Sprite SLIME = Sprite.Dungeon(108);
    private static readonly Sprite BAT = Sprite.Dungeon(120);
    private static readonly Sprite[] VILLAGERS = new Sprite[] { Sprite.Dungeon(99), Sprite.Dungeon(86), Sprite.Dungeon(85) };

    public static Sprite GetGround(Map map, int x, int y)
    {
        FloorType floor = map.GetFloor(x, y);
        if (floor.Id == "path")
        {
            return PATH;
        }

        if (floor.Id == "dirt")
        {
            return GetDirtPiece(map, x, y, floor);
        }

        if (floor.Id == "wood_floor")
        {
            return WOOD_FLOOR;
        }

        int variant = GetVariant(x, y) % 10;
        if (variant == 9)
        {
            return GRASS_FLOWERS;
        }

        return variant >= 6 ? GRASS_TUFTS : GRASS_PLAIN;
    }

    public static Sprite? GetPropSpriteOrNull(Map map, int x, int y)
    {
        PropType? propOrNull = map.GetPropOrNull(x, y);
        if (propOrNull == null)
        {
            return null;
        }

        if (propOrNull.Id == "tree")
        {
            switch (GetTreeKind(x, y))
            {
                case ETreeKind.TallGreen:
                    return TALL_GREEN_TREE_BASE;
                case ETreeKind.TallAutumn:
                    return TALL_AUTUMN_TREE_BASE;
                case ETreeKind.SmallGreen:
                    return SMALL_GREEN_TREE;
                default:
                    return SMALL_AUTUMN_TREE;
            }
        }

        if (propOrNull.Id == "bush")
        {
            return BUSH;
        }

        if (propOrNull.Id == "fence")
        {
            return GetFencePiece(map, x, y);
        }

        if (propOrNull.Id == "gate")
        {
            return GATE;
        }

        if (propOrNull.Id == "well")
        {
            return WELL;
        }

        if (propOrNull.Id == "sign")
        {
            return SIGN;
        }

        return null;
    }

    public static Sprite? GetTreeTopOrNull(Map map, int x, int y)
    {
        if (map.GetPropOrNull(x, y)?.Id != "tree")
        {
            return null;
        }

        switch (GetTreeKind(x, y))
        {
            case ETreeKind.TallGreen:
                return TALL_GREEN_TREE_TOP;
            case ETreeKind.TallAutumn:
                return TALL_AUTUMN_TREE_TOP;
            default:
                return null;
        }
    }

    public static WallPiece? GetWallPieceOrNull(Map map, int x, int y)
    {
        PropType? propOrNull = map.GetPropOrNull(x, y);
        if (propOrNull?.Id is not ("wall" or "door"))
        {
            return null;
        }

        return new WallPiece(
            propOrNull.Id == "door",
            IsWall(map, x, y - 1),
            IsWall(map, x, y + 1),
            IsWall(map, x - 1, y),
            IsWall(map, x + 1, y));
    }

    public static Sprite GetActorSprite(Actor actor)
    {
        ActorType type = actor.Type;
        if (type.Id == "hero")
        {
            return HERO;
        }

        if (type.Id == "rat")
        {
            return RAT;
        }

        if (type.Id == "slime")
        {
            return SLIME;
        }

        if (type.Id == "bat")
        {
            return BAT;
        }

        return VILLAGERS[actor.ID % VILLAGERS.Length];
    }

    public static Sprite GetThingSprite(ThingType type)
    {
        if (type.Id == "potion")
        {
            return POTION;
        }

        if (type.Id == "branch")
        {
            return BRANCH;
        }

        if (type.Id == "spike_trap")
        {
            return SPIKE_TRAP;
        }

        return GOLD;
    }

    public static Sprite GetWoodHouseSprite(int column, int row, int width)
    {
        return GetHouseSprite(column, row, width, 48, 84, 85, 73, 75);
    }

    public static Sprite GetStoneHouseSprite(int column, int row, int width)
    {
        return GetHouseSprite(column, row, width, 52, 88, 89, 77, 79);
    }

    private static Sprite GetHouseSprite(int column, int row, int width, int roofTopLeft, int window, int door, int wall, int wallRight)
    {
        const int DOOR_COLUMN = 1;
        bool bLeft = column == 0;
        bool bRight = column == width - 1;

        if (row == 0)
        {
            int piece = bLeft ? 0 : (bRight ? 2 : 1);
            return Sprite.Town(roofTopLeft + piece);
        }

        if (row == 1)
        {
            int piece = column == DOOR_COLUMN ? 3 : (bLeft ? 0 : (bRight ? 2 : 1));
            return Sprite.Town(roofTopLeft + SHEET_COLUMN_COUNT + piece);
        }

        if (column == DOOR_COLUMN)
        {
            return Sprite.Town(door);
        }

        if (bLeft)
        {
            return Sprite.Town(window);
        }

        return Sprite.Town(bRight ? wallRight : wall);
    }

    private static Sprite GetDirtPiece(Map map, int x, int y, FloorType floor)
    {
        bool bNorth = IsFloor(map, x, y - 1, floor);
        bool bSouth = IsFloor(map, x, y + 1, floor);
        bool bWest = IsFloor(map, x - 1, y, floor);
        bool bEast = IsFloor(map, x + 1, y, floor);
        int row = !bNorth ? 0 : (bSouth ? 1 : 2);
        int column = !bWest ? 0 : (bEast ? 1 : 2);
        return Sprite.Town(DIRT_TOP_LEFT + row * SHEET_COLUMN_COUNT + column);
    }

    private static Sprite GetFencePiece(Map map, int x, int y)
    {
        bool bNorth = IsFence(map, x, y - 1);
        bool bSouth = IsFence(map, x, y + 1);
        bool bWest = IsFence(map, x - 1, y);
        bool bEast = IsFence(map, x + 1, y);

        if (bSouth && bEast && !bNorth && !bWest)
        {
            return Sprite.Town(44);
        }

        if (bSouth && bWest && !bNorth && !bEast)
        {
            return Sprite.Town(46);
        }

        if (bNorth && bEast && !bSouth && !bWest)
        {
            return Sprite.Town(68);
        }

        if (bNorth && bWest && !bSouth && !bEast)
        {
            return Sprite.Town(70);
        }

        if (bNorth || bSouth)
        {
            int topY = y;
            while (IsFence(map, x, topY - 1))
            {
                topY--;
            }

            if (IsFence(map, x + 1, topY))
            {
                return Sprite.Town(56);
            }

            return IsFence(map, x - 1, topY) ? Sprite.Town(58) : Sprite.Town(59);
        }

        return Sprite.Town(45);
    }

    private static bool IsFence(Map map, int x, int y)
    {
        return map.IsInside(x, y) && map.GetPropOrNull(x, y)?.Id is "fence" or "gate";
    }

    private static bool IsWall(Map map, int x, int y)
    {
        return map.IsInside(x, y) && map.GetPropOrNull(x, y)?.Id is "wall" or "door";
    }

    private static bool IsFloor(Map map, int x, int y, FloorType type)
    {
        return map.IsInside(x, y) && map.GetFloor(x, y) == type;
    }

    private enum ETreeKind
    {
        TallGreen,
        TallAutumn,
        SmallGreen,
        SmallAutumn
    }

    private static ETreeKind GetTreeKind(int x, int y)
    {
        int variant = GetVariant(x, y) % 20;
        if (variant < 11)
        {
            return ETreeKind.TallGreen;
        }

        if (variant < 14)
        {
            return ETreeKind.TallAutumn;
        }

        return variant < 19 ? ETreeKind.SmallGreen : ETreeKind.SmallAutumn;
    }

    private static int GetVariant(int x, int y)
    {
        uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663);
        hash ^= hash >> 13;
        hash *= 0x5bd1e995;
        hash ^= hash >> 15;
        return (int)(hash & 0x7fffffff);
    }
}
