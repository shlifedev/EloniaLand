using Elonia.Simulation;

namespace Elonia.Playtest;

internal static class GameContent
{
    public static readonly TileType GRASS = new TileType("Grass", true);
    public static readonly TileType PATH = new TileType("Path", true);
    public static readonly TileType DIRT = new TileType("Dirt", true);
    public static readonly TileType GATE = new TileType("Gate", true);
    public static readonly TileType TREE = new TileType("Tree", false, new TileHarvest(GRASS, EItemType.Branch, BRANCH_AMOUNT));
    public static readonly TileType BUSH = new TileType("Bush", false);
    public static readonly TileType FENCE = new TileType("Fence", false);
    public static readonly TileType BUILDING = new TileType("Building", false);
    public static readonly TileType WELL = new TileType("Well", false);
    public static readonly TileType SIGN = new TileType("Sign", false);

    public static readonly ActorType HERO = new ActorType("Hero", 12, 2, 100);
    public static readonly ActorType VILLAGER = new ActorType("Villager", 5, 0, 40);
    public static readonly ActorType RAT = new ActorType("Rat", 2, 0, 80, true);
    public static readonly ActorType SLIME = new ActorType("Slime", 6, 1, 50, true);
    public static readonly ActorType BAT = new ActorType("Bat", 3, 1, 140, true);

    public const int RAT_SENSE_RANGE = 4;
    public const int SLIME_SENSE_RANGE = 5;
    public const int BAT_SENSE_RANGE = 7;

    public const int POTION_HEAL = 5;
    public const int GOLD_AMOUNT = 10;
    public const int BRANCH_AMOUNT = 1;
}
