using System;
using System.IO;
using System.Numerics;
using ImGuiNET;
using rlImGui_cs;

namespace Elonia.Playtest;

internal sealed class ImGuiLayer : IDisposable
{
    private const string FONT_FILE_NAME = "Galmuri11.ttf";
    private const float FONT_SIZE = 12.0f;

    public ImGuiLayer()
    {
        rlImGui.LoadDefaultFont = false;
        rlImGui.LoadFontAwesome = false;
        rlImGui.SetupUserFonts = LoadKoreanFont;
        rlImGui.Setup(true);
        DisableIniFile();
        ApplyStyle();
    }

    public void Begin(float deltaTime)
    {
        rlImGui.Begin(deltaTime);
    }

    public void End()
    {
        rlImGui.End();
    }

    public void Dispose()
    {
        rlImGui.Shutdown();
    }

    private static unsafe void LoadKoreanFont(ImGuiIOPtr io)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", FONT_FILE_NAME);

        ImFontConfigPtr config = new ImFontConfigPtr(ImGuiNative.ImFontConfig_ImFontConfig());
        config.OversampleH = 1;
        config.OversampleV = 1;
        config.PixelSnapH = true;
        io.Fonts.AddFontFromFileTTF(path, FONT_SIZE, config, io.Fonts.GetGlyphRangesKorean());
        config.Destroy();
    }

    private static unsafe void DisableIniFile()
    {
        ImGui.GetIO().NativePtr->IniFilename = null;
    }

    private static void ApplyStyle()
    {
        ImGuiStylePtr style = ImGui.GetStyle();
        style.WindowRounding = 0.0f;
        style.FrameRounding = 0.0f;
        style.TabRounding = 0.0f;
        style.ScrollbarRounding = 0.0f;
        style.GrabRounding = 0.0f;
        style.WindowBorderSize = 1.0f;
        style.ItemSpacing = new Vector2(6.0f, 4.0f);
    }
}
