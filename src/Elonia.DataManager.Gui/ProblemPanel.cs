using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace Elonia.DataManager.Gui;

internal sealed class ProblemPanel
{
    public List<string> OutputLines { get; } = new List<string>();

    public bool IsOutputShown { get; set; }

    public Diagnostic? Draw(TableBuildResult? resultOrNull)
    {
        int problemCount = resultOrNull == null ? 0 : resultOrNull.Diagnostics.Items.Count;
        if (ImGui.RadioButton($"문제 ({problemCount})", !IsOutputShown))
        {
            IsOutputShown = false;
        }

        ImGui.SameLine();
        if (ImGui.RadioButton("생성 결과", IsOutputShown))
        {
            IsOutputShown = true;
        }

        ImGui.Separator();
        if (!IsOutputShown)
        {
            return resultOrNull == null ? null : DrawProblems(resultOrNull.Diagnostics.Items);
        }

        if (OutputLines.Count == 0)
        {
            ImGui.TextColored(Palette.MUTED_TEXT, "아직 생성하지 않았습니다. 위의 [생성] 버튼을 누르면 결과가 여기에 나옵니다.");
        }

        for (int index = 0; index < OutputLines.Count; index++)
        {
            ImGui.TextUnformatted(OutputLines[index]);
        }

        return null;
    }

    private static Diagnostic? DrawProblems(IReadOnlyList<Diagnostic> diagnostics)
    {
        if (diagnostics.Count == 0)
        {
            ImGui.TextColored(Palette.GOOD_TEXT, "문제가 없습니다.");
            return null;
        }

        Diagnostic? clickedOrNull = null;
        ImGuiTableFlags flags = ImGuiTableFlags.ScrollY | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable;
        if (!ImGui.BeginTable("problems", 4, flags, new Vector2(0.0f, -1.0f)))
        {
            return null;
        }

        ImGui.TableSetupScrollFreeze(0, 1);
        ImGui.TableSetupColumn("종류", ImGuiTableColumnFlags.WidthFixed, 44.0f);
        ImGui.TableSetupColumn("위치", ImGuiTableColumnFlags.WidthFixed, 230.0f);
        ImGui.TableSetupColumn("값", ImGuiTableColumnFlags.WidthFixed, 130.0f);
        ImGui.TableSetupColumn("이유");
        ImGui.TableHeadersRow();
        for (int index = 0; index < diagnostics.Count; index++)
        {
            Diagnostic diagnostic = diagnostics[index];
            bool bError = diagnostic.Severity == ESeverity.Error;
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.PushID(index);
            ImGui.PushStyleColor(ImGuiCol.Text, bError ? Palette.ERROR_TEXT : Palette.WARNING_TEXT);
            if (ImGui.Selectable(bError ? "오류" : "경고", false, ImGuiSelectableFlags.SpanAllColumns))
            {
                clickedOrNull = diagnostic;
            }

            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
            {
                DrawTooltip(diagnostic);
            }

            ImGui.PopID();
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(diagnostic.Location);
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(diagnostic.ValueOrNull ?? "");
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(diagnostic.Reason);
        }

        ImGui.EndTable();
        return clickedOrNull;
    }

    public static void DrawTooltip(Diagnostic diagnostic)
    {
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(520.0f);
        ImGui.TextColored(diagnostic.Severity == ESeverity.Error ? Palette.ERROR_TEXT : Palette.WARNING_TEXT, diagnostic.Location);
        if (diagnostic.ValueOrNull != null)
        {
            ImGui.TextUnformatted("값: \"" + diagnostic.ValueOrNull + "\"");
        }

        ImGui.TextUnformatted("이유: " + diagnostic.Reason);
        if (diagnostic.FixOrNull != null)
        {
            ImGui.TextColored(Palette.GOOD_TEXT, "고치는 방법: " + diagnostic.FixOrNull);
        }

        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}
