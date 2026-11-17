using System;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace Elonia.Playtest;

internal sealed class SpriteAtlas : IDisposable
{
    public const int SOURCE_TILE_SIZE = 16;
    private const int COLUMN_COUNT = 12;

    private readonly Texture2D _town;
    private readonly Texture2D _dungeon;

    public SpriteAtlas()
    {
        _town = LoadSheet("tiny-town.png");
        _dungeon = LoadSheet("tiny-dungeon.png");
    }

    public void Draw(Sprite sprite, int screenX, int screenY, int size, Color tint, bool bFlipX = false)
    {
        Texture2D texture = sprite.Sheet == ESpriteSheet.Town ? _town : _dungeon;
        int sourceX = sprite.Index % COLUMN_COUNT * SOURCE_TILE_SIZE;
        int sourceY = sprite.Index / COLUMN_COUNT * SOURCE_TILE_SIZE;

        float sourceWidth = bFlipX ? -SOURCE_TILE_SIZE : SOURCE_TILE_SIZE;
        Rectangle source = new Rectangle(sourceX, sourceY, sourceWidth, SOURCE_TILE_SIZE);
        Rectangle destination = new Rectangle(screenX, screenY, size, size);
        Raylib.DrawTexturePro(texture, source, destination, Vector2.Zero, 0.0f, tint);
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(_town);
        Raylib.UnloadTexture(_dungeon);
    }

    private static Texture2D LoadSheet(string fileName)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("A sprite sheet is missing.", path);
        }

        Texture2D texture = Raylib.LoadTexture(path);

        Raylib.SetTextureFilter(texture, TextureFilter.Point);
        return texture;
    }
}
