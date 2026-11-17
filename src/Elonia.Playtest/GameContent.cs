using Elonia.Simulation;

namespace Elonia.Playtest;

internal static class GameContent
{
    public static readonly TileType GRASS = new TileType("Grass", true);
    public static readonly TileType PATH = new TileType("Path", true);
    public static readonly TileType DIRT = new TileType("Dirt", true);
    public static readonly TileType GATE = new TileType("Gate", true);
    public static readonly TileType TREE = new TileType("Tree", false);
    public static readonly TileType BUSH = new TileType("Bush", false);
    public static readonly TileType FENCE = new TileType("Fence", false);
    public static readonly TileType BUILDING = new TileType("Building", false);
    public static readonly TileType WELL = new TileType("Well", false);
    public static readonly TileType SIGN = new TileType("Sign", false);

    public static readonly ActorType HERO = new ActorType("Hero", 12, 2, 10);
    public static readonly ActorType VILLAGER = new ActorType("Villager", 5, 0, 4);
    public static readonly ActorType RAT = new ActorType("Rat", 2, 0, 8);
    public static readonly ActorType SLIME = new ActorType("Slime", 6, 1, 5);
    public static readonly ActorType BAT = new ActorType("Bat", 3, 1, 14);

    public const int RAT_SENSE_RANGE = 4;
    public const int SLIME_SENSE_RANGE = 5;
    public const int BAT_SENSE_RANGE = 7;

    public const int POTION_HEAL = 5;
    public const int GOLD_AMOUNT = 10;

    public static bool IsAttackable(Actor actor)
    {
        return actor.Type == SLIME || actor.Type == BAT || actor.Type == RAT;
    }
}
