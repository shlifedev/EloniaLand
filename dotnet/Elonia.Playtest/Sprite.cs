namespace Elonia.Playtest;

internal enum ESpriteSheet
{
    Town,
    Dungeon
}

internal readonly struct Sprite
{
    public ESpriteSheet Sheet { get; }
    public int Index { get; }

    public Sprite(ESpriteSheet sheet, int index)
    {
        Sheet = sheet;
        Index = index;
    }

    public static Sprite Town(int index)
    {
        return new Sprite(ESpriteSheet.Town, index);
    }

    public static Sprite Dungeon(int index)
    {
        return new Sprite(ESpriteSheet.Dungeon, index);
    }
}
