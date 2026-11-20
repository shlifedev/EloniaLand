using System.Collections.Generic;
using Cysharp.Text;
using Elonia.Game;
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
        DrawInventoryPanel(atlas, font, session.Player.Inventory);
        DrawControls(font, screenWidth);

        if (session.Runner.IsGameOver)
        {
            Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, OVERLAY);
            font.DrawCentered("당신은 쓰러졌다", screenWidth / 2, screenHeight / 2 - 70, ETextSize.Title, MessageLog.DANGER);
            font.DrawCentered("R 키를 누르면 다시 시작한다", screenWidth / 2, screenHeight / 2 + 10, ETextSize.Normal, MessageLog.NORMAL);
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

        font.Draw("체력", LEFT + 16, TOP + 8, ETextSize.Normal, MessageLog.NORMAL);
        int barLeft = LEFT + 76;
        int barTop = TOP + 16;
        Raylib.DrawRectangle(barLeft, barTop, BAR_WIDTH, BAR_HEIGHT, HP_BACK);
        Raylib.DrawRectangle(barLeft, barTop, BAR_WIDTH * player.Hp / player.Type.MaxHp, BAR_HEIGHT, HP_FILL);
        string hpText = ZString.Concat(player.Hp, "/", player.Type.MaxHp);
        font.DrawCentered(hpText, barLeft + BAR_WIDTH / 2, barTop, ETextSize.Small, WHITE);

        atlas.Draw(Art.GOLD, LEFT + 10, TOP + 46, 32, WHITE);
        font.Draw($"{player.Gold}", LEFT + 52, TOP + 46, ETextSize.Normal, MessageLog.GOLD);
        string tickText = ZString.Concat("틱 ", session.World.Tick);
        font.Draw(tickText, LEFT + WIDTH - 16 - font.Measure(tickText, ETextSize.Small), TOP + 62, ETextSize.Small, MessageLog.MUTED);
    }

    private static void DrawInventoryPanel(SpriteAtlas atlas, UiFont font, Inventory inventory)
    {
        const int LEFT = 16;
        const int TOP = 116;
        const int WIDTH = 268;
        const int TITLE_HEIGHT = 30;
        const int ROW_HEIGHT = 34;

        IReadOnlyList<ItemStack> stacks = inventory.Stacks;
        if (stacks.Count == 0)
        {
            return;
        }

        Rectangle panel = new Rectangle(LEFT, TOP, WIDTH, TITLE_HEIGHT + stacks.Count * ROW_HEIGHT + 8);
        Raylib.DrawRectangleRounded(panel, 0.18f, 6, PANEL);
        Raylib.DrawRectangleRoundedLinesEx(panel, 0.18f, 6, 2.0f, PANEL_BORDER);
        font.Draw("소지품", LEFT + 16, TOP + 8, ETextSize.Small, MessageLog.MUTED);

        for (int index = 0; index < stacks.Count; index++)
        {
            ItemStack stack = stacks[index];
            int rowTop = TOP + TITLE_HEIGHT + index * ROW_HEIGHT;
            atlas.Draw(Art.GetThingSprite(stack.Type), LEFT + 10, rowTop, 32, WHITE);
            font.Draw(ZString.Concat(GameText.GetThingName(stack.Type), "  x", stack.Count), LEFT + 52, rowTop, ETextSize.Normal, MessageLog.NORMAL);
        }
    }

    private static void DrawControls(UiFont font, int screenWidth)
    {
        const string CONTROLS = "WASD/방향키  이동/공격/베기     두 번 클릭  자동 이동     우클릭  행동     E  줍기     스페이스  대기     R  재시작     F1  디버그";
        int width = font.Measure(CONTROLS, ETextSize.Small);
        font.Draw(CONTROLS, screenWidth - width - 16, 16, ETextSize.Small, MessageLog.NORMAL);
    }
}
