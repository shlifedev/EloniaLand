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
    private const string FONT_FILE_NAME = "DNFBitBitTTF.ttf";

    private const int SMALL_SIZE = 16;
    private const int NORMAL_SIZE = 24;
    private const int TITLE_SIZE = 48;

    private const int PIXELS_PER_OFFSET = 12;

    private const int FIRST_ASCII = 0x20;
    private const int LAST_ASCII = 0x7E;
    private const int FIRST_HANGUL_JAMO = 0x3131;
    private const int LAST_HANGUL_JAMO = 0x318E;
    private const int FIRST_HANGUL_SYLLABLE = 0xAC00;
    private const int LAST_HANGUL_SYLLABLE = 0xD7A3;

    private static readonly Color SHADOW = new Color(24, 20, 37, 255);

    private readonly Font _smallFont;
    private readonly Font _normalFont;
    private readonly Font _titleFont;

    public UiFont()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", FONT_FILE_NAME);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The font is missing.", path);
        }

        int[] codepoints = CreateCodepoints();
        _smallFont = LoadFont(path, SMALL_SIZE, codepoints);
        _normalFont = LoadFont(path, NORMAL_SIZE, codepoints);
        _titleFont = LoadFont(path, TITLE_SIZE, codepoints);
    }

    public void Draw(string text, int x, int y, ETextSize size, Color color, float alpha = 1.0f)
    {
        Font font = GetFont(size);
        int pixelSize = GetPixelSize(size);
        int shadowOffset = GetShadowOffset(size);
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
        Raylib.UnloadFont(_smallFont);
        Raylib.UnloadFont(_normalFont);
        Raylib.UnloadFont(_titleFont);
    }

    private static Font LoadFont(string path, int pixelSize, int[] codepoints)
    {
        Font font = Raylib.LoadFontEx(path, pixelSize, codepoints, codepoints.Length);
        Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
        return font;
    }

    private Font GetFont(ETextSize size)
    {
        switch (size)
        {
            case ETextSize.Small:
                return _smallFont;
            case ETextSize.Title:
                return _titleFont;
            default:
                return _normalFont;
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

    private static int GetShadowOffset(ETextSize size)
    {
        return GetPixelSize(size) / PIXELS_PER_OFFSET;
    }

    private static float GetSpacing(ETextSize size)
    {
        return GetPixelSize(size) / PIXELS_PER_OFFSET;
    }

    private static int[] CreateCodepoints()
    {
        int asciiCount = LAST_ASCII - FIRST_ASCII + 1;
        int jamoCount = LAST_HANGUL_JAMO - FIRST_HANGUL_JAMO + 1;
        int syllableCount = LAST_HANGUL_SYLLABLE - FIRST_HANGUL_SYLLABLE + 1;
        int[] codepoints = new int[asciiCount + jamoCount + syllableCount];

        int index = 0;
        index = FillRange(codepoints, index, FIRST_ASCII, LAST_ASCII);
        index = FillRange(codepoints, index, FIRST_HANGUL_JAMO, LAST_HANGUL_JAMO);
        FillRange(codepoints, index, FIRST_HANGUL_SYLLABLE, LAST_HANGUL_SYLLABLE);
        return codepoints;
    }

    private static int FillRange(int[] codepoints, int startIndex, int first, int last)
    {
        int index = startIndex;
        for (int codepoint = first; codepoint <= last; codepoint++)
        {
            codepoints[index] = codepoint;
            index++;
        }

        return index;
    }
}
