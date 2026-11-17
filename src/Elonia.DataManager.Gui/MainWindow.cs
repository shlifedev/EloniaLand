using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Elonia.DataManager.Gui;

internal sealed class MainWindow : Window
{
    private static readonly TimeSpan POLL_INTERVAL = TimeSpan.FromMilliseconds(500);

    private readonly TableWorkspace _workspace;
    private readonly StartOptions _start;

    private readonly Button _buildButton;
    private readonly Button _openButton;
    private readonly CheckBox _autoCheck;
    private readonly TextBlock _summary;
    private readonly ListBox _sheetList;
    private readonly SheetGridView _grid;
    private readonly TabControl _bottomTabs;
    private readonly TabItem _problemTab;
    private readonly DataGrid _problemGrid;
    private readonly TextBox _output;
    private readonly DispatcherTimer _pollTimer;

    private readonly List<SheetItem> _sheetItems = new List<SheetItem>();
    private readonly List<ProblemItem> _problemItems = new List<ProblemItem>();

    private bool _isUpdatingLists;

    private Sheet? _shownSheetOrNull;

    private bool _isChangePending;

    public MainWindow(TableWorkspace workspace, StartOptions start)
    {
        _workspace = workspace;
        _start = start;
        Title = "Elonia Data — " + Path.GetFullPath(workspace.InputDirectory);
        Width = 1280;
        Height = 800;
        MinWidth = 800;
        MinHeight = 500;

        Button refreshButton = CreateButton("검증 (F5)", "엑셀을 다시 읽고 검증합니다. 아무것도 쓰지 않습니다.");
        refreshButton.Click += (object? sender, Avalonia.Interactivity.RoutedEventArgs args) => Refresh();
        _buildButton = CreateButton("생성", "코드와 bytes를 만들어 결과 폴더에 반영합니다. 오류를 모두 고쳐야 누를 수 있습니다.");
        _buildButton.Classes.Add("accent");
        _buildButton.Click += (object? sender, Avalonia.Interactivity.RoutedEventArgs args) => Build();
        _openButton = CreateButton("엑셀로 열기", "고른 시트의 파일을 엑셀(기본 프로그램)로 엽니다.");
        _openButton.Click += (object? sender, Avalonia.Interactivity.RoutedEventArgs args) => OpenSelectedWorkbook();
        _autoCheck = new CheckBox { Content = "저장하면 자동 검증", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(_autoCheck, "엑셀에서 저장하면 다시 검증합니다. 생성은 자동으로 하지 않습니다.");
        _summary = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };

        StackPanel toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(10, 8) };
        toolbar.Children.Add(refreshButton);
        toolbar.Children.Add(_buildButton);
        toolbar.Children.Add(_openButton);
        toolbar.Children.Add(_autoCheck);
        toolbar.Children.Add(_summary);

        _sheetList = new ListBox { ItemsSource = _sheetItems, ItemTemplate = CreateSheetItemTemplate() };
        _sheetList.SelectionChanged += (object? sender, SelectionChangedEventArgs args) => OnSheetSelected();

        _grid = new SheetGridView();

        _problemGrid = CreateProblemGrid();
        _problemGrid.ItemsSource = _problemItems;
        _problemGrid.SelectionChanged += (object? sender, SelectionChangedEventArgs args) => OnProblemSelected();
        _output = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Menlo, Consolas, monospace"),
            Text = "아직 생성하지 않았습니다. 위의 [생성] 버튼을 누르면 결과가 여기에 나옵니다.",
        };
        _problemTab = CreateTab("문제", _problemGrid);
        _bottomTabs = new TabControl { Padding = new Thickness(0) };
        _bottomTabs.Items.Add(_problemTab);
        _bottomTabs.Items.Add(CreateTab("생성 결과", _output));

        Grid body = new Grid { ColumnDefinitions = new ColumnDefinitions("240,4,*") };
        AddToGrid(body, WrapInCard(_sheetList), 0, 0);
        AddToGrid(body, new GridSplitter { ResizeDirection = GridResizeDirection.Columns, Background = Brushes.Transparent }, 0, 1);
        AddToGrid(body, WrapInCard(_grid), 0, 2);

        Grid root = new Grid { RowDefinitions = new RowDefinitions("Auto,*,4,230"), Margin = new Thickness(0, 0, 0, 8) };
        root.Bind(Panel.BackgroundProperty, root.GetResourceObservable("SystemControlBackgroundAltHighBrush"));
        AddToGrid(root, toolbar, 0, 0);
        AddToGrid(root, WithHorizontalMargin(body), 1, 0);
        AddToGrid(root, new GridSplitter { ResizeDirection = GridResizeDirection.Rows, Background = Brushes.Transparent }, 2, 0);
        AddToGrid(root, WithHorizontalMargin(WrapInCard(_bottomTabs)), 3, 0);
        Content = root;

        KeyDown += OnKeyDown;
        _pollTimer = new DispatcherTimer { Interval = POLL_INTERVAL };
        _pollTimer.Tick += (object? sender, EventArgs args) => PollInput();
        _pollTimer.Start();
        Opened += (object? sender, EventArgs args) => OnOpened();

        Refresh();
        if (_start.SheetNameOrNull != null)
        {
            SelectSheet(null, _start.SheetNameOrNull);
        }
    }

    private void OnOpened()
    {
        if (_start.ProblemNumber > 0 && _start.ProblemNumber <= _problemItems.Count)
        {
            _problemGrid.SelectedIndex = _start.ProblemNumber - 1;
        }

        if (_start.ScreenshotPathOrNull != null)
        {
            DispatcherTimer.RunOnce(SaveScreenshotAndClose, TimeSpan.FromMilliseconds(1200));
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.F5)
        {
            Refresh();
            args.Handled = true;
        }
    }

    private void PollInput()
    {
        if (_autoCheck.IsChecked != true)
        {
            return;
        }

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

    private void Refresh()
    {
        SheetItem? selectedOrNull = _sheetList.SelectedItem as SheetItem;
        string? workbookNameOrNull = selectedOrNull?.Sheet.WorkbookName;
        string? sheetNameOrNull = selectedOrNull?.Sheet.Name;
        TableBuildResult result = _workspace.Refresh();
        _isChangePending = false;

        _isUpdatingLists = true;
        _sheetItems.Clear();
        bool bShowWorkbook = result.Workbooks.Count > 1;
        for (int workbookIndex = 0; workbookIndex < result.Workbooks.Count; workbookIndex++)
        {
            IReadOnlyList<Sheet> sheets = result.Workbooks[workbookIndex].Sheets;
            for (int sheetIndex = 0; sheetIndex < sheets.Count; sheetIndex++)
            {
                _workspace.CountProblems(sheets[sheetIndex], out int errorCount, out int warningCount);
                _sheetItems.Add(new SheetItem(sheets[sheetIndex], bShowWorkbook, errorCount, warningCount));
            }
        }

        _sheetList.ItemsSource = null;
        _sheetList.ItemsSource = _sheetItems;

        _problemItems.Clear();
        IReadOnlyList<Diagnostic> diagnostics = result.Diagnostics.Items;
        for (int index = 0; index < diagnostics.Count; index++)
        {
            _problemItems.Add(new ProblemItem(diagnostics[index]));
        }

        _problemGrid.ItemsSource = null;
        _problemGrid.ItemsSource = _problemItems;
        _problemTab.Header = "문제 (" + diagnostics.Count + ")";
        _isUpdatingLists = false;

        if (sheetNameOrNull == null || !SelectSheet(workbookNameOrNull, sheetNameOrNull))
        {
            SelectFirstReadableSheet();
        }

        ShowSelectedSheet(true);
        UpdateSummary(result);
    }

    private void UpdateSummary(TableBuildResult result)
    {
        int rowCount = 0;
        for (int index = 0; index < result.Tables.Count; index++)
        {
            rowCount += result.Tables[index].Rows.Count;
        }

        DiagnosticBag diagnostics = result.Diagnostics;
        _summary.Text = $"표 {result.Tables.Count} · 행 {rowCount} · enum {result.Enums.Count} · 오류 {diagnostics.ErrorCount} · 경고 {diagnostics.WarningCount}   ({DateTime.Now:HH:mm:ss} 검증)";
        _summary.Foreground = diagnostics.ErrorCount > 0 ? Palette.ERROR_TEXT : diagnostics.WarningCount > 0 ? Palette.WARNING_TEXT : Palette.GOOD_TEXT;
        _buildButton.IsEnabled = result.IsSuccess;
    }

    private void Build()
    {
        StringBuilder output = new StringBuilder();
        try
        {
            CommitReport? reportOrNull = _workspace.BuildOrNull();
            Refresh();
            if (reportOrNull == null)
            {
                output.Append("오류가 있어서 아무것도 쓰지 않았습니다. 문제 탭을 확인하세요.\n");
            }
            else
            {
                for (int index = 0; index < reportOrNull.Written.Count; index++)
                {
                    output.Append("씀    ").Append(reportOrNull.Written[index]).Append('\n');
                }

                for (int index = 0; index < reportOrNull.Deleted.Count; index++)
                {
                    output.Append("지움  ").Append(reportOrNull.Deleted[index]).Append('\n');
                }

                string changes = reportOrNull.HasChanges ? $"파일 {reportOrNull.Written.Count}개를 쓰고 {reportOrNull.Deleted.Count}개를 지웠습니다" : "바뀐 것이 없습니다";
                output.Append($"{DateTime.Now:HH:mm:ss} 완료: {changes}(그대로 {reportOrNull.UnchangedCount}개).\n");
                output.Append("코드:   ").Append(Path.GetFullPath(_workspace.CodeDirectory)).Append('\n');
                output.Append("데이터: ").Append(Path.GetFullPath(_workspace.DataDirectory)).Append('\n');
            }
        }
        catch (IOException exception)
        {
            output.Append(exception.Message).Append('\n');
        }

        ShowOutput(output.ToString());
    }

    private void ShowOutput(string text)
    {
        _output.Text = text;
        _bottomTabs.SelectedIndex = 1;
    }

    private void OnSheetSelected()
    {
        if (!_isUpdatingLists)
        {
            ShowSelectedSheet(false);
        }
    }

    private void ShowSelectedSheet(bool bForce)
    {
        SheetItem? itemOrNull = _sheetList.SelectedItem as SheetItem;
        Sheet? sheetOrNull = itemOrNull?.Sheet;
        _openButton.IsEnabled = sheetOrNull != null;
        if (!bForce && sheetOrNull == _shownSheetOrNull)
        {
            return;
        }

        _shownSheetOrNull = sheetOrNull;
        _grid.ShowSheet(_workspace, sheetOrNull);
    }

    private void OnProblemSelected()
    {
        if (_isUpdatingLists || !(_problemGrid.SelectedItem is ProblemItem item))
        {
            return;
        }

        Diagnostic diagnostic = item.Diagnostic;
        if (diagnostic.OriginOrNull == null)
        {
            for (int index = 0; index < _sheetItems.Count; index++)
            {
                if (_sheetItems[index].Sheet.Location == diagnostic.Location)
                {
                    _sheetList.SelectedIndex = index;
                }
            }

            return;
        }

        CellOrigin origin = diagnostic.OriginOrNull.Value;
        if (SelectSheet(origin.Workbook, origin.Sheet))
        {
            Dispatcher.UIThread.Post(() => _grid.FocusCell(origin), DispatcherPriority.Background);
        }
    }

    private bool SelectSheet(string? workbookNameOrNull, string sheetName)
    {
        for (int index = 0; index < _sheetItems.Count; index++)
        {
            Sheet sheet = _sheetItems[index].Sheet;
            if (sheet.Name == sheetName && (workbookNameOrNull == null || sheet.WorkbookName == workbookNameOrNull))
            {
                _sheetList.SelectedIndex = index;
                return true;
            }
        }

        return false;
    }

    private void SelectFirstReadableSheet()
    {
        for (int index = 0; index < _sheetItems.Count; index++)
        {
            if (SheetRole.FromName(_sheetItems[index].Sheet.Name).Kind != ESheetKind.Ignored)
            {
                _sheetList.SelectedIndex = index;
                return;
            }
        }

        _sheetList.SelectedIndex = _sheetItems.Count > 0 ? 0 : -1;
    }

    private void OpenSelectedWorkbook()
    {
        if (!(_sheetList.SelectedItem is SheetItem item))
        {
            return;
        }

        string path = Path.GetFullPath(Path.Combine(_workspace.InputDirectory, item.Sheet.WorkbookName));
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(path);
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception || exception is IOException || exception is InvalidOperationException)
        {
            ShowOutput("파일을 열지 못했습니다: " + exception.Message);
        }
    }

    private void SaveScreenshotAndClose()
    {
        if (Content is Control root && _start.ScreenshotPathOrNull != null)
        {
            PixelSize size = new PixelSize((int)root.Bounds.Width, (int)root.Bounds.Height);
            using (RenderTargetBitmap bitmap = new RenderTargetBitmap(size, new Vector(96, 96)))
            {
                bitmap.Render(root);
                bitmap.Save(_start.ScreenshotPathOrNull);
            }
        }

        Close();
    }

    private static TabItem CreateTab(string header, Control content)
    {
        return new TabItem { Header = header, Content = content, FontSize = 14, MinHeight = 34, Padding = new Thickness(12, 4) };
    }

    private static Button CreateButton(string text, string toolTip)
    {
        Button button = new Button { Content = text, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(button, toolTip);
        return button;
    }

    private static IDataTemplate CreateSheetItemTemplate()
    {
        return new FuncDataTemplate<SheetItem>((SheetItem item, INameScope scope) =>
        {
            Grid line = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,32") };
            TextBlock title = new TextBlock { TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
            title.Bind(TextBlock.TextProperty, new Binding(nameof(SheetItem.Title)));
            title.Bind(OpacityProperty, new Binding(nameof(SheetItem.Opacity)));
            TextBlock kind = new TextBlock { Foreground = Palette.MUTED_TEXT, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0) };
            kind.Bind(TextBlock.TextProperty, new Binding(nameof(SheetItem.Kind)));
            TextBlock count = new TextBlock { FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            count.Bind(TextBlock.TextProperty, new Binding(nameof(SheetItem.Count)));
            count.Bind(TextBlock.ForegroundProperty, new Binding(nameof(SheetItem.CountBrush)));
            AddToGrid(line, title, 0, 0);
            AddToGrid(line, kind, 0, 1);
            AddToGrid(line, count, 0, 2);
            return line;
        });
    }

    private static DataGrid CreateProblemGrid()
    {
        DataGrid grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserSortColumns = false,
            CanUserReorderColumns = false,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            SelectionMode = DataGridSelectionMode.Single,
            RowHeight = 26,
        };
        grid.Columns.Add(CreateProblemColumn("종류", nameof(ProblemItem.Kind), new DataGridLength(68), true));
        grid.Columns.Add(CreateProblemColumn("위치", nameof(ProblemItem.Location), new DataGridLength(240), false));
        grid.Columns.Add(CreateProblemColumn("값", nameof(ProblemItem.Value), new DataGridLength(140), false));
        grid.Columns.Add(CreateProblemColumn("이유", nameof(ProblemItem.Reason), new DataGridLength(1, DataGridLengthUnitType.Star), false));
        return grid;
    }

    private static DataGridColumn CreateProblemColumn(string header, string path, DataGridLength width, bool bColoredByKind)
    {
        return new DataGridTemplateColumn
        {
            Header = header,
            Width = width,
            CellTemplate = new FuncDataTemplate<ProblemItem>((ProblemItem item, INameScope scope) =>
            {
                TextBlock text = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(6, 2) };
                text.Bind(TextBlock.TextProperty, new Binding(path));
                if (bColoredByKind)
                {
                    text.FontWeight = FontWeight.SemiBold;
                    text.Bind(TextBlock.ForegroundProperty, new Binding(nameof(ProblemItem.KindBrush)));
                }

                Border border = new Border { Background = Brushes.Transparent, Child = text };
                border.Bind(ToolTip.TipProperty, new Binding(nameof(ProblemItem.Details)));
                return border;
            }),
        };
    }

    private static Border WrapInCard(Control content)
    {
        Border card = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true,
            Child = content,
        };
        card.Bind(Border.BorderBrushProperty, card.GetResourceObservable("SystemControlForegroundBaseLowBrush"));
        return card;
    }

    private static Control WithHorizontalMargin(Control control)
    {
        control.Margin = new Thickness(10, 0);
        return control;
    }

    private static void AddToGrid(Grid grid, Control control, int row, int column)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        grid.Children.Add(control);
    }
}
