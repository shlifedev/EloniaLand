using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using ImGuiNET;

namespace Elonia.DataManager.Gui;

internal sealed class MainWindow
{
    private const float POLL_SECONDS = 0.5f;

    private const float SHEET_LIST_WIDTH = 260.0f;
    private const float PROBLEM_PANEL_HEIGHT = 230.0f;

    private readonly TableWorkspace _workspace;
    private readonly SheetListPanel _sheetList = new SheetListPanel();
    private readonly SheetGridPanel _grid = new SheetGridPanel();
    private readonly ProblemPanel _problems = new ProblemPanel();

    private Sheet? _selectedSheetOrNull;
    private bool _isAutoCheckOn = true;
    private float _pollTimer;

    private bool _isChangePending;

    private string _summary = "";
    private Vector4 _summaryColor = Palette.MUTED_TEXT;

    public MainWindow(TableWorkspace workspace, string? initialSheetNameOrNull)
    {
        _workspace = workspace;
        Refresh();
        if (initialSheetNameOrNull != null)
        {
            _selectedSheetOrNull = FindSheetOrNull(null, initialSheetNameOrNull) ?? _selectedSheetOrNull;
        }
    }

    public void SelectProblem(int index)
    {
        IReadOnlyList<Diagnostic> diagnostics = _workspace.ResultOrNull!.Diagnostics.Items;
        if (index >= 0 && index < diagnostics.Count)
        {
            GoToProblem(diagnostics[index]);
        }
    }

    public void Update(float deltaTime)
    {
        if (ImGui.IsKeyPressed(ImGuiKey.F5))
        {
            Refresh();
        }

        _pollTimer += deltaTime;
        if (!_isAutoCheckOn || _pollTimer < POLL_SECONDS)
        {
            return;
        }

        _pollTimer = 0.0f;
        if (_isChangePending)
        {
            _isChangePending = false;
            Refresh();
        }
        else if (_workspace.HasInputChanged())
        {
            _isChangePending = true;
        }
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(new Vector2(screenWidth, screenHeight));
        ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoScrollbar;
        if (!ImGui.Begin("main", flags))
        {
            ImGui.End();
            return;
        }

        DrawToolbar();
        ImGui.Separator();

        float bodyHeight = Math.Max(120.0f, ImGui.GetContentRegionAvail().Y - PROBLEM_PANEL_HEIGHT - ImGui.GetStyle().ItemSpacing.Y);
        if (ImGui.BeginChild("sheets", new Vector2(SHEET_LIST_WIDTH, bodyHeight), ImGuiChildFlags.Borders))
        {
            Sheet? clickedOrNull = _sheetList.Draw(_workspace, _selectedSheetOrNull);
            if (clickedOrNull != null)
            {
                _selectedSheetOrNull = clickedOrNull;
                _grid.ClearFocus();
            }
        }

        ImGui.EndChild();
        ImGui.SameLine();
        if (ImGui.BeginChild("grid", new Vector2(0.0f, bodyHeight), ImGuiChildFlags.Borders))
        {
            if (_selectedSheetOrNull != null)
            {
                _grid.Draw(_workspace, _selectedSheetOrNull);
            }
            else
            {
                ImGui.TextColored(Palette.MUTED_TEXT, "보여 줄 시트가 없습니다. 엑셀 폴더: " + Path.GetFullPath(_workspace.InputDirectory));
            }
        }

        ImGui.EndChild();
        if (ImGui.BeginChild("problems", new Vector2(0.0f, 0.0f), ImGuiChildFlags.Borders))
        {
            Diagnostic? clickedOrNull = _problems.Draw(_workspace.ResultOrNull);
            if (clickedOrNull != null)
            {
                GoToProblem(clickedOrNull);
            }
        }

        ImGui.EndChild();
        ImGui.End();
    }

    private void DrawToolbar()
    {
        if (ImGui.Button("검증 (F5)"))
        {
            Refresh();
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("엑셀을 다시 읽고 검증합니다. 아무것도 쓰지 않습니다.");
        }

        ImGui.SameLine();
        bool bCanBuild = _workspace.ResultOrNull != null && _workspace.ResultOrNull.IsSuccess;
        ImGui.BeginDisabled(!bCanBuild);
        if (ImGui.Button("생성"))
        {
            Build();
        }

        ImGui.EndDisabled();
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
        {
            ImGui.SetTooltip(bCanBuild ? "코드와 bytes를 만들어 결과 폴더에 반영합니다." : "오류를 모두 고쳐야 생성할 수 있습니다.");
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(_selectedSheetOrNull == null);
        if (ImGui.Button("엑셀로 열기") && _selectedSheetOrNull != null)
        {
            OpenInDefaultProgram(Path.Combine(_workspace.InputDirectory, _selectedSheetOrNull.WorkbookName));
        }

        ImGui.EndDisabled();
        ImGui.SameLine();
        ImGui.Checkbox("저장하면 자동 검증", ref _isAutoCheckOn);
        ImGui.SameLine();
        ImGui.TextColored(_summaryColor, _summary);
    }

    private void Refresh()
    {
        string? selectedWorkbookOrNull = _selectedSheetOrNull?.WorkbookName;
        string? selectedNameOrNull = _selectedSheetOrNull?.Name;
        TableBuildResult result = _workspace.Refresh();
        _isChangePending = false;

        _selectedSheetOrNull = selectedNameOrNull != null ? FindSheetOrNull(selectedWorkbookOrNull, selectedNameOrNull) : null;
        if (_selectedSheetOrNull == null)
        {
            _selectedSheetOrNull = FindFirstReadableSheetOrNull();
            _grid.ClearFocus();
        }

        int rowCount = 0;
        for (int index = 0; index < result.Tables.Count; index++)
        {
            rowCount += result.Tables[index].Rows.Count;
        }

        string time = DateTime.Now.ToString("HH:mm:ss");
        _summary = $"표 {result.Tables.Count} · 행 {rowCount} · enum {result.Enums.Count} · 오류 {result.Diagnostics.ErrorCount} · 경고 {result.Diagnostics.WarningCount}   ({time} 검증)";
        _summaryColor = result.Diagnostics.ErrorCount > 0 ? Palette.ERROR_TEXT : result.Diagnostics.WarningCount > 0 ? Palette.WARNING_TEXT : Palette.GOOD_TEXT;
    }

    private void Build()
    {
        _problems.OutputLines.Clear();
        _problems.IsOutputShown = true;
        try
        {
            CommitReport? reportOrNull = _workspace.BuildOrNull();
            Refresh();
            if (reportOrNull == null)
            {
                _problems.OutputLines.Add("오류가 있어서 아무것도 쓰지 않았습니다. 문제 탭을 확인하세요.");
                return;
            }

            for (int index = 0; index < reportOrNull.Written.Count; index++)
            {
                _problems.OutputLines.Add("씀    " + reportOrNull.Written[index]);
            }

            for (int index = 0; index < reportOrNull.Deleted.Count; index++)
            {
                _problems.OutputLines.Add("지움  " + reportOrNull.Deleted[index]);
            }

            string changes = reportOrNull.HasChanges ? $"파일 {reportOrNull.Written.Count}개를 쓰고 {reportOrNull.Deleted.Count}개를 지웠습니다" : "바뀐 것이 없습니다";
            _problems.OutputLines.Add($"{DateTime.Now:HH:mm:ss} 완료: {changes}(그대로 {reportOrNull.UnchangedCount}개).");
            _problems.OutputLines.Add("코드: " + Path.GetFullPath(_workspace.CodeDirectory));
            _problems.OutputLines.Add("데이터: " + Path.GetFullPath(_workspace.DataDirectory));
        }
        catch (IOException exception)
        {
            _problems.OutputLines.Add(exception.Message);
        }
    }

    private void GoToProblem(Diagnostic diagnostic)
    {
        if (diagnostic.OriginOrNull != null)
        {
            CellOrigin origin = diagnostic.OriginOrNull.Value;
            Sheet? sheetOrNull = FindSheetOrNull(origin.Workbook, origin.Sheet);
            if (sheetOrNull != null)
            {
                _selectedSheetOrNull = sheetOrNull;
                _grid.Focus(origin);
            }

            return;
        }

        TableBuildResult result = _workspace.ResultOrNull!;
        for (int workbookIndex = 0; workbookIndex < result.Workbooks.Count; workbookIndex++)
        {
            IReadOnlyList<Sheet> sheets = result.Workbooks[workbookIndex].Sheets;
            for (int sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
            {
                if (sheets[sheetIndex].Location == diagnostic.Location)
                {
                    _selectedSheetOrNull = sheets[sheetIndex];
                    _grid.ClearFocus();
                    return;
                }
            }
        }
    }

    private Sheet? FindSheetOrNull(string? workbookNameOrNull, string sheetName)
    {
        TableBuildResult? resultOrNull = _workspace.ResultOrNull;
        if (resultOrNull == null)
        {
            return null;
        }

        for (int workbookIndex = 0; workbookIndex < resultOrNull.Workbooks.Count; workbookIndex++)
        {
            Workbook workbook = resultOrNull.Workbooks[workbookIndex];
            if (workbookNameOrNull != null && workbook.Name != workbookNameOrNull)
            {
                continue;
            }

            for (int sheetIndex = 0; sheetIndex < workbook.Sheets.Count; sheetIndex++)
            {
                if (workbook.Sheets[sheetIndex].Name == sheetName)
                {
                    return workbook.Sheets[sheetIndex];
                }
            }
        }

        return null;
    }

    private Sheet? FindFirstReadableSheetOrNull()
    {
        TableBuildResult? resultOrNull = _workspace.ResultOrNull;
        Sheet? firstOrNull = null;
        if (resultOrNull == null)
        {
            return null;
        }

        for (int workbookIndex = 0; workbookIndex < resultOrNull.Workbooks.Count; workbookIndex++)
        {
            IReadOnlyList<Sheet> sheets = resultOrNull.Workbooks[workbookIndex].Sheets;
            for (int sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
            {
                firstOrNull ??= sheets[sheetIndex];
                if (SheetRole.FromName(sheets[sheetIndex].Name).Kind != ESheetKind.Ignored)
                {
                    return sheets[sheetIndex];
                }
            }
        }

        return firstOrNull;
    }

    private void OpenInDefaultProgram(string path)
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(Path.GetFullPath(path));
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception || exception is IOException || exception is InvalidOperationException)
        {
            _problems.OutputLines.Clear();
            _problems.OutputLines.Add("파일을 열지 못했습니다: " + exception.Message);
            _problems.IsOutputShown = true;
        }
    }
}
