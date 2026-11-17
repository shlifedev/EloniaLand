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
    private const string FONT_FILE_NAME = "Galmuri11.ttf";

    private const int BASE_SIZE = 12;
    private const int SMALL_SIZE = BASE_SIZE;
    private const int NORMAL_SIZE = BASE_SIZE * 2;
    private const int TITLE_SIZE = BASE_SIZE * 4;

    private const int FIRST_ASCII = 0x20;
    private const int LAST_ASCII = 0x7E;
    private const int FIRST_HANGUL_JAMO = 0x3131;
    private const int LAST_HANGUL_JAMO = 0x318E;
    private const int FIRST_HANGUL_SYLLABLE = 0xAC00;
    private const int LAST_HANGUL_SYLLABLE = 0xD7A3;

    private static readonly Color SHADOW = new Color(24, 20, 37, 255);

    private readonly Font _font;

    public UiFont()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", FONT_FILE_NAME);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The font is missing.", path);
        }

        int[] codepoints = CreateCodepoints();
        _font = Raylib.LoadFontEx(path, BASE_SIZE, codepoints, codepoints.Length);
        Raylib.SetTextureFilter(_font.Texture, TextureFilter.Point);
    }

    public void Draw(string text, int x, int y, ETextSize size, Color color, float alpha = 1.0f)
    {
        int pixelSize = GetPixelSize(size);
        int shadowOffset = pixelSize / BASE_SIZE;
        float spacing = GetSpacing(size);
        Raylib.DrawTextEx(_font, text, new Vector2(x + shadowOffset, y + shadowOffset), pixelSize, spacing, Raylib.Fade(SHADOW, alpha));
        Raylib.DrawTextEx(_font, text, new Vector2(x, y), pixelSize, spacing, Raylib.Fade(color, alpha));
    }

    public void DrawCentered(string text, int centerX, int y, ETextSize size, Color color, float alpha = 1.0f)
    {
        Draw(text, centerX - Measure(text, size) / 2, y, size, color, alpha);
    }

    public int Measure(string text, ETextSize size)
    {
        return (int)Raylib.MeasureTextEx(_font, text, GetPixelSize(size), GetSpacing(size)).X;
    }

    public void Dispose()
    {
        Raylib.UnloadFont(_font);
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
        return GetPixelSize(size) / BASE_SIZE;
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
