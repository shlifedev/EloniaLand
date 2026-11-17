using System.Numerics;

namespace Elonia.DataManager.Gui;

internal static class Palette
{
    public static readonly Vector4 ERROR_TEXT = new Vector4(1.00f, 0.45f, 0.42f, 1.0f);
    public static readonly Vector4 WARNING_TEXT = new Vector4(1.00f, 0.82f, 0.35f, 1.0f);
    public static readonly Vector4 GOOD_TEXT = new Vector4(0.55f, 0.90f, 0.60f, 1.0f);
    public static readonly Vector4 MUTED_TEXT = new Vector4(0.55f, 0.56f, 0.62f, 1.0f);
    public static readonly Vector4 HEADER_TEXT = new Vector4(0.60f, 0.82f, 1.00f, 1.0f);

    public static readonly Vector4 ERROR_CELL = new Vector4(0.52f, 0.14f, 0.14f, 1.0f);
    public static readonly Vector4 WARNING_CELL = new Vector4(0.46f, 0.36f, 0.08f, 1.0f);
    public static readonly Vector4 HEADER_CELL = new Vector4(0.16f, 0.19f, 0.25f, 1.0f);
    public static readonly Vector4 FOCUSED_CELL = new Vector4(0.20f, 0.38f, 0.68f, 1.0f);

    public static readonly Vector4 FOCUSED_ERROR_CELL = new Vector4(0.86f, 0.24f, 0.22f, 1.0f);
    public static readonly Vector4 FOCUSED_WARNING_CELL = new Vector4(0.78f, 0.60f, 0.10f, 1.0f);

    public static readonly Raylib_cs.Color BACKGROUND = new Raylib_cs.Color(24, 26, 31, 255);
}
