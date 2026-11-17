using System;
using System.IO;
using System.Numerics;
using Raylib_cs;

namespace Elonia.Playtest;

internal enum ETextSize
{
    Small,
    Normal,
    Title
}

internal sealed class UiFont : IDisposable
{
    private const int SMALL_SIZE = 16;
    private const int NORMAL_SIZE = 32;
    private const int TITLE_SIZE = 64;

    private static readonly Color SHADOW = new Color(24, 20, 37, 255);

    private readonly Font _small;
    private readonly Font _normal;
    private readonly Font _title;

    public UiFont()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "kenney-mini.ttf");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The font is missing.", path);
        }

        _small = LoadSize(path, SMALL_SIZE);
        _normal = LoadSize(path, NORMAL_SIZE);
        _title = LoadSize(path, TITLE_SIZE);
    }

    public void Draw(string text, int x, int y, ETextSize size, Color color, float alpha = 1.0f)
    {
        Font font = GetFont(size);
        int pixelSize = GetPixelSize(size);
        int shadowOffset = pixelSize / SMALL_SIZE;
        float spacing = GetSpacing(size);
        Raylib.DrawTextEx(font, text, new Vector2(x + shadowOffset, y + shadowOffset), pixelSize, spacing, Raylib.Fade(SHADOW, alpha));
        Raylib.DrawTextEx(font, text, new Vector2(x, y), pixelSize, spacing, Raylib.Fade(color, alpha));
    }

    public void DrawCentered(string text, int centerX, int y, ETextSize size, Color color, float alpha = 1.0f)
    {
        Draw(text, centerX - Measure(text, size) / 2, y, size, color, alpha);
    }

    public int Measure(string text, ETextSize size)
    {
        return (int)Raylib.MeasureTextEx(GetFont(size), text, GetPixelSize(size), GetSpacing(size)).X;
    }

    public void Dispose()
    {
        Raylib.UnloadFont(_small);
        Raylib.UnloadFont(_normal);
        Raylib.UnloadFont(_title);
    }

    private Font GetFont(ETextSize size)
    {
        switch (size)
        {
            case ETextSize.Small:
                return _small;
            case ETextSize.Title:
                return _title;
            default:
                return _normal;
        }
    }

    private static int GetPixelSize(ETextSize size)
    {
        switch (size)
        {
            case ETextSize.Small:
                return SMALL_SIZE;
            case ETextSize.Title:
                return TITLE_SIZE;
            default:
                return NORMAL_SIZE;
        }
    }

    private static float GetSpacing(ETextSize size)
    {
        return GetPixelSize(size) / (float)SMALL_SIZE;
    }

    private static Font LoadSize(string path, int pixelSize)
    {
        Font font = Raylib.LoadFontEx(path, pixelSize, null, 0);
        Raylib.SetTextureFilter(font.Texture, TextureFilter.Point);
        return font;
    }
}
