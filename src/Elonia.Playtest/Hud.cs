using Elonia.Simulation;
using Raylib_cs;

namespace Elonia.Playtest;

internal static class Hud
{
    private static readonly Color PANEL = new Color(24, 20, 37, 215);
    private static readonly Color PANEL_BORDER = new Color(240, 236, 220, 60);
    private static readonly Color HP_BACK = new Color(70, 40, 52, 255);
    private static readonly Color HP_FILL = new Color(232, 72, 72, 255);
    private static readonly Color WHITE = new Color(255, 255, 255, 255);
    private static readonly Color OVERLAY = new Color(24, 20, 37, 170);

    public static void Draw(SpriteAtlas atlas, UiFont font, GameSession session, string? promptOrNull, int screenWidth, int screenHeight)
    {
        DrawStatusPanel(atlas, font, session);
        DrawControls(font, screenWidth);

        if (session.Runner.IsGameOver)
        {
            Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, OVERLAY);
            font.DrawCentered("YOU DIED", screenWidth / 2, screenHeight / 2 - 70, ETextSize.Title, MessageLog.DANGER);
            font.DrawCentered("Press R to try again", screenWidth / 2, screenHeight / 2 + 10, ETextSize.Normal, MessageLog.NORMAL);
        }
        else if (promptOrNull != null)
        {
            font.DrawCentered(promptOrNull, screenWidth / 2, screenHeight - 64, ETextSize.Normal, MessageLog.NORMAL);
        }
    }

    private static void DrawStatusPanel(SpriteAtlas atlas, UiFont font, GameSession session)
    {
        const int LEFT = 16;
        const int TOP = 16;
        const int WIDTH = 268;
        const int HEIGHT = 92;
        const int BAR_WIDTH = 170;
        const int BAR_HEIGHT = 16;

        Actor player = session.Player;
        Rectangle panel = new Rectangle(LEFT, TOP, WIDTH, HEIGHT);
        Raylib.DrawRectangleRounded(panel, 0.18f, 6, PANEL);
        Raylib.DrawRectangleRoundedLinesEx(panel, 0.18f, 6, 2.0f, PANEL_BORDER);

        font.Draw("HP", LEFT + 16, TOP + 8, ETextSize.Normal, MessageLog.NORMAL);
        int barLeft = LEFT + 52;
        int barTop = TOP + 16;
        Raylib.DrawRectangle(barLeft, barTop, BAR_WIDTH, BAR_HEIGHT, HP_BACK);
        Raylib.DrawRectangle(barLeft, barTop, BAR_WIDTH * player.Hp / player.Type.MaxHp, BAR_HEIGHT, HP_FILL);
        string hpText = $"{player.Hp}/{player.Type.MaxHp}";
        font.DrawCentered(hpText, barLeft + BAR_WIDTH / 2, barTop, ETextSize.Small, WHITE);

        atlas.Draw(Art.GOLD, LEFT + 10, TOP + 46, 32, WHITE);
        font.Draw($"{player.Gold}", LEFT + 52, TOP + 46, ETextSize.Normal, MessageLog.GOLD);
        string tickText = $"TICK {session.World.Tick}";
        font.Draw(tickText, LEFT + WIDTH - 16 - font.Measure(tickText, ETextSize.Small), TOP + 62, ETextSize.Small, MessageLog.MUTED);
    }

    private static void DrawControls(UiFont font, int screenWidth)
    {
        const string CONTROLS = "WASD / ARROWS  Move / Attack     E  Pick up     SPACE  Wait     R  Restart     F1  Debug";
        int width = font.Measure(CONTROLS, ETextSize.Small);
        font.Draw(CONTROLS, screenWidth - width - 16, 16, ETextSize.Small, MessageLog.NORMAL);
    }
}
