using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using NewGreen.Domain;
using NewGreen.Infrastructure.Export;
using NewGreen.Infrastructure.FileSystem;
using NewGreen.Infrastructure.Persistence;
using NewGreen.Infrastructure.Thumbnails;

namespace NewGreen.UI;

public sealed partial class MainWindow : Window
{
    private const int FixedHeaderFooterMarginMm = 0;
    private const int FixedOuterMarginMm = 0;
    private const int PhotoScaleLimit = 5;
    private const string ProgramName = AppInfo.Name;
    private const string ProgramVersion = AppInfo.Version;
    private const string GroupDragPrefix = "new-green-group:";
    private const string UnclassifiedRootKey = "__newgreen_unclassified_root__";
    private const double SettingsPreviewPaperWidth = 420;
    private const double SettingsPreviewPaperHeight = 594;

    private readonly FileScanner scanner = new();
    private readonly MetadataStore metadataStore = new();
    private readonly DocumentExporter documentExporter = new();
    private readonly ThumbnailService thumbnailService = new();

    private AppState state = new();
    private ScanResult scanResult = new();
    private string selectedPhoto = string.Empty;
    private string selectedGroupId = string.Empty;
    private string pendingDragPhoto = string.Empty;
    private string pendingDragGroupId = string.Empty;
    private Point? dragStartPoint;
    private Point? groupDragStartPoint;
    private bool isExplorerVisible;
    private bool isPreviewVisible;
    private bool isLoading;
    private bool isAutoSaveFeedbackVisible;
    private int unclassifiedPhotoScaleLevel;
    private int classifiedPhotoScaleLevel;
    private int autoSaveFeedbackVersion;
    private readonly HashSet<string> expandedExplorerPaths = [];
    private readonly HashSet<string> expandedUnclassifiedPaths = [];

    private readonly TextBlock status = new() { Text = "기준 폴더를 선택하세요.", VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock loadingMessage = new() { Text = "로딩 중...", FontSize = 18, FontWeight = FontWeight.Bold, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly ProgressBar loadingProgress = new() { Width = 360, Height = 10, Minimum = 0, Maximum = 1, IsIndeterminate = true };
    private readonly Border loadingOverlay = new() { IsVisible = false };
    private readonly ContentControl topBarHost = new();
    private readonly Grid mainGrid = new();
    private readonly StackPanel leftPanel = new() { Spacing = 6 };
    private readonly Grid centerPanel = new();
    private readonly StackPanel rightPanel = new() { Spacing = 0 };
    private readonly Grid detailPanel = new();
    private readonly TextBlock explorerHeader = Header("탐색기 영역");
    private readonly TextBlock unclassifiedHeader = Header("분류되지 않은 영역");
    private readonly TextBlock classifiedHeader = Header("분류된 영역");
    private Control? unclassifiedHeaderActions;
    private Control? classifiedHeaderActions;
    private Control? previewSplitter;
    private Control? previewFrame;
    private Popup? activePathTreePopup;

    public MainWindow()
    {
        Title = ProgramName;
        Width = 1520;
        Height = 820;
        MinWidth = 1180;
        MinHeight = 640;
        Background = Brushes.White;

        Content = BuildShell();
        Closed += (_, _) => ClearThumbnailCacheOnExit();
        RefreshAll();
    }

    private Control BuildShell()
    {
        var shell = new Grid();
        var root = new DockPanel();

        topBarHost.Content = BuildTopBar();
        DockPanel.SetDock(topBarHost, Dock.Top);
        root.Children.Add(topBarHost);

        var bottom = new Border
        {
            Padding = new Thickness(10, 6),
            BorderBrush = Brush("#d0d6dc"),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Child = status
        };
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);

        mainGrid.ColumnDefinitions = new ColumnDefinitions("0,0,*,2,*,0,0");
        AddToGrid(mainGrid, PanelFrame(explorerHeader, new ScrollViewer
        {
            Content = leftPanel,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        }), 0, 0);
        AddToGrid(mainGrid, Splitter(GridResizeDirection.Columns), 1, 0);
        AddToGrid(mainGrid, PanelFrame(unclassifiedHeader, centerPanel, HeaderZoomControls(isClassified: false)), 2, 0);
        AddToGrid(mainGrid, Splitter(GridResizeDirection.Columns), 3, 0);
        AddToGrid(mainGrid, PanelFrame(classifiedHeader, new ScrollViewer { Content = rightPanel }, HeaderZoomControls(isClassified: true)), 4, 0);
        previewSplitter = Splitter(GridResizeDirection.Columns);
        previewFrame = PanelFrame("미리보기", detailPanel);
        AddToGrid(mainGrid, previewSplitter, 5, 0);
        AddToGrid(mainGrid, previewFrame, 6, 0);
        ApplyPreviewVisibility();

        root.Children.Add(mainGrid);
        shell.Children.Add(root);
        shell.Children.Add(BuildLoadingOverlay());
        return shell;
    }

    private Control BuildLoadingOverlay()
    {
        loadingOverlay.Background = new SolidColorBrush(Color.FromArgb(210, 255, 255, 255));
        loadingOverlay.Child = new Border
        {
            Width = 460,
            Padding = new Thickness(28, 24),
            Background = Brushes.White,
            BorderBrush = Brush("#cfd6dd"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = new StackPanel
            {
                Spacing = 16,
                Children =
                {
                    loadingMessage,
                    loadingProgress
                }
            }
        };
        return loadingOverlay;
    }

    private Control BuildTopBar()
    {
        var bar = new DockPanel
        {
            Background = Brushes.White,
            LastChildFill = false,
            Height = 32
        };

        var info = InfoMenuButton();
        DockPanel.SetDock(info, Dock.Left);
        bar.Children.Add(info);

        var open = MenuButton("폴더 열기", async (_, _) => await OpenFolderAsync());
        DockPanel.SetDock(open, Dock.Left);
        bar.Children.Add(open);

        if (!string.IsNullOrWhiteSpace(state.RootDir))
        {
            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 0,
                Margin = new Thickness(0)
            };
            actions.Children.Add(MenuButton("종이 설정", async (_, _) => await ShowSettingsAsync()));
            actions.Children.Add(ExportMenuButton());
            actions.Children.Add(SaveMenuButton());
            actions.Children.Add(ExplorerToggleButton());
            actions.Children.Add(PreviewToggleButton());

            DockPanel.SetDock(actions, Dock.Right);
            bar.Children.Add(actions);
        }
        return bar;
    }

    private Button InfoMenuButton()
    {
        var button = MenuButton("ⓘ", async (_, _) => await ShowVersionAsync());
        button.Width = 42;
        button.MinWidth = 42;
        button.Padding = new Thickness(0);
        button.FontSize = 20;
        ToolTip.SetTip(button, "프로그램 정보");
        return button;
    }

    private static Button MenuButton(object content, EventHandler<RoutedEventArgs> action)
    {
        var button = new Button
        {
            Content = content,
            MinWidth = 82,
            Padding = new Thickness(12, 0),
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        button.Classes.Add("menu-bar-button");
        button.Resources["ButtonBackground"] = Brushes.Transparent;
        button.Resources["ButtonBackgroundPointerOver"] = Brush("#f1f3f5");
        button.Resources["ButtonBackgroundPressed"] = Brush("#e9ecef");
        button.Resources["ButtonBorderBrush"] = Brushes.Transparent;
        button.Resources["ButtonBorderBrushPointerOver"] = Brushes.Transparent;
        button.Resources["ButtonBorderBrushPressed"] = Brushes.Transparent;
        button.Resources["ButtonForeground"] = Brushes.Black;
        button.Resources["ButtonForegroundPointerOver"] = Brushes.Black;
        button.Resources["ButtonForegroundPressed"] = Brushes.Black;
        button.Click += action;
        return button;
    }

    private Button SaveMenuButton()
    {
        var button = MenuButton(isAutoSaveFeedbackVisible ? "저장 중" : "저장하기", (_, _) => Save());
        button.Width = 82;
        button.MinWidth = 82;

        if (!isAutoSaveFeedbackVisible)
        {
            return button;
        }

        button.IsEnabled = false;
        button.Background = Brush("#e9ecef");
        button.Foreground = Brush("#69737d");
        button.Resources["ButtonBackgroundDisabled"] = Brush("#e9ecef");
        button.Resources["ButtonBorderBrushDisabled"] = Brushes.Transparent;
        button.Resources["ButtonForegroundDisabled"] = Brush("#69737d");
        return button;
    }

    private Button PreviewToggleButton()
    {
        var button = MenuButton(SidePanelIcon(leftSide: false), (_, _) =>
        {
            isPreviewVisible = !isPreviewVisible;
            ApplyPreviewVisibility();
            RefreshTopBar();
        });
        button.MinWidth = 42;
        ToolTip.SetTip(button, isPreviewVisible ? "미리보기 닫기" : "미리보기 열기");
        return button;
    }

    private Button ExplorerToggleButton()
    {
        var button = MenuButton(SidePanelIcon(leftSide: true), (_, _) =>
        {
            isExplorerVisible = !isExplorerVisible;
            ApplyPanelVisibility();
            RefreshTopBar();
        });
        button.MinWidth = 42;
        ToolTip.SetTip(button, isExplorerVisible ? "탐색기 닫기" : "탐색기 열기");
        return button;
    }

    private static Control SidePanelIcon(bool leftSide)
    {
        var body = new Grid
        {
            Width = 15,
            Height = 15,
            ColumnDefinitions = leftSide
                ? new ColumnDefinitions("6,*")
                : new ColumnDefinitions("*,6")
        };

        var filledSide = new Border
        {
            Background = Brush("#202020")
        };
        AddToGrid(body, filledSide, leftSide ? 0 : 1, 0);

        return new Border
        {
            Width = 18,
            Height = 18,
            CornerRadius = new CornerRadius(5),
            BorderBrush = Brush("#202020"),
            BorderThickness = new Thickness(1.5),
            Background = Brushes.White,
            ClipToBounds = true,
            Child = body
        };
    }

    private void ApplyPreviewVisibility()
    {
        ApplyPanelVisibility();
    }

    private void ApplyPanelVisibility()
    {
        var explorerColumns = isExplorerVisible ? "380,2" : "0,0";
        var previewColumns = isPreviewVisible ? "2,360" : "0,0";
        mainGrid.ColumnDefinitions = new ColumnDefinitions($"{explorerColumns},*,2,*,{previewColumns}");
        mainGrid.Children[0].IsVisible = isExplorerVisible;
        mainGrid.Children[1].IsVisible = isExplorerVisible;
        if (previewSplitter is not null)
        {
            previewSplitter.IsVisible = isPreviewVisible;
        }

        if (previewFrame is not null)
        {
            previewFrame.IsVisible = isPreviewVisible;
        }
    }

    private Button ExportMenuButton()
    {
        var button = MenuButton("내보내기", (_, _) => { });
        var menu = new ContextMenu
        {
            Background = Brushes.White,
            Foreground = Brushes.Black,
            ItemsSource = new[]
            {
                ExportMenuItem(".hwpx 로 내보내기", async () => await ExportOneAsync(ExportFormat.Hwpx)),
                ExportMenuItem(".docx 로 내보내기", async () => await ExportOneAsync(ExportFormat.Docx))
            }
        };
        menu.Resources["MenuFlyoutPresenterBackground"] = Brushes.White;
        menu.Resources["MenuFlyoutPresenterBorderBrush"] = Brush("#d8dde2");
        menu.Resources["MenuItemBackground"] = Brushes.White;
        menu.Resources["MenuItemBackgroundPointerOver"] = Brush("#f1f3f5");
        menu.Resources["MenuItemBackgroundPressed"] = Brush("#e9ecef");
        menu.Resources["MenuItemForeground"] = Brushes.Black;
        menu.Resources["MenuItemForegroundPointerOver"] = Brushes.Black;
        menu.Resources["MenuItemForegroundPressed"] = Brushes.Black;

        button.ContextMenu = menu;
        button.Click += (_, _) => menu.Open(button);
        return button;
    }

    private static MenuItem ExportMenuItem(string text, Func<Task> action)
    {
        var content = new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(18, 10),
            MinWidth = 210,
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brushes.Black,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        var item = new MenuItem
        {
            Header = content,
            MinWidth = 210,
            Padding = new Thickness(0),
            FontSize = 14,
            Background = Brushes.White,
            Foreground = Brushes.Black
        };
        item.Resources["MenuItemBackground"] = Brushes.White;
        item.Resources["MenuItemBackgroundPointerOver"] = Brush("#f1f3f5");
        item.Resources["MenuItemBackgroundPressed"] = Brush("#e9ecef");
        item.Resources["MenuItemForeground"] = Brushes.Black;
        item.Resources["MenuItemForegroundPointerOver"] = Brushes.Black;
        item.Resources["MenuItemForegroundPressed"] = Brushes.Black;
        item.PointerEntered += (_, _) => content.Background = Brush("#f1f3f5");
        item.PointerExited += (_, _) => content.Background = Brushes.White;
        item.PointerPressed += (_, _) => content.Background = Brush("#e9ecef");
        item.PointerReleased += (_, _) => content.Background = Brush("#f1f3f5");
        item.Click += async (_, _) => await action();
        return item;
    }

    private static Control PanelFrame(string title, Control child)
    {
        return PanelFrame(Header(title), child);
    }

    private static Control PanelFrame(TextBlock header, Control child, Control? headerActions = null)
    {
        var headerContent = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        AddToGrid(headerContent, header, 0, 0);
        if (headerActions is not null)
        {
            AddToGrid(headerContent, headerActions, 1, 0);
        }

        var headerBar = new Border
        {
            Background = Brush("#e9ecef"),
            BorderBrush = Brush("#d0d6dc"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Height = 36,
            Child = headerContent
        };
        DockPanel.SetDock(headerBar, Dock.Top);
        return new Border
        {
            BorderBrush = Brush("#aeb7bf"),
            BorderThickness = new Thickness(1),
            Child = new DockPanel
            {
                Children =
                {
                    headerBar,
                    child
                }
            }
        };
    }

    private static TextBlock Header(string title)
    {
        var header = new TextBlock
        {
            Text = title,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.Black,
            Margin = new Thickness(10, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        return header;
    }

    private Control HeaderZoomControls(bool isClassified)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };

        if (isClassified)
        {
            panel.Children.Add(HeaderActionButton("공종 추가", AddPhotoGroup));
        }
        panel.Children.Add(HeaderZoomButton("⌕−", isClassified, -1));
        panel.Children.Add(HeaderZoomButton("⌕+", isClassified, 1));
        panel.IsVisible = !string.IsNullOrWhiteSpace(state.RootDir);
        if (isClassified)
        {
            classifiedHeaderActions = panel;
        }
        else
        {
            unclassifiedHeaderActions = panel;
        }
        return panel;
    }

    private void AddPhotoGroup()
    {
        var group = state.AddGroup();
        selectedGroupId = group.Id;
        RefreshAll();
    }

    private void SelectGroupForPreview(string groupId)
    {
        if (string.IsNullOrWhiteSpace(groupId) || state.GroupById(groupId) is null)
        {
            return;
        }

        selectedGroupId = groupId;
        RefreshDetail();
    }

    private static Button HeaderActionButton(string text, Action action)
    {
        var button = HeaderToolButton(text, enabled: true);
        button.Width = 78;
        button.Click += (_, _) => action();
        return button;
    }

    private Button HeaderZoomButton(string text, bool isClassified, int delta)
    {
        var level = isClassified ? classifiedPhotoScaleLevel : unclassifiedPhotoScaleLevel;
        var canClick = delta > 0 ? level < PhotoScaleLimit : level > -PhotoScaleLimit;
        var button = HeaderToolButton(text, canClick);
        ToolTip.SetTip(button, delta > 0 ? "사진 크게 보기" : "사진 작게 보기");
        button.Click += (_, _) => AdjustPhotoScale(isClassified, delta);
        return button;
    }

    private static Button HeaderToolButton(string text, bool enabled)
    {
        var button = new Button
        {
            Content = text,
            Width = 34,
            Height = 26,
            Padding = new Thickness(0),
            FontSize = 14,
            IsEnabled = enabled,
            Foreground = enabled ? Brushes.Black : Brush("#9aa3ab"),
            Background = Brushes.Transparent,
            BorderBrush = Brush("#b8c0c8"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Resources["ButtonBackground"] = Brushes.Transparent;
        button.Resources["ButtonBackgroundPointerOver"] = Brush("#f7f8f9");
        button.Resources["ButtonBackgroundPressed"] = Brush("#dfe4e8");
        button.Resources["ButtonBackgroundDisabled"] = Brushes.Transparent;
        button.Resources["ButtonForeground"] = Brushes.Black;
        button.Resources["ButtonForegroundPointerOver"] = Brushes.Black;
        button.Resources["ButtonForegroundPressed"] = Brushes.Black;
        button.Resources["ButtonForegroundDisabled"] = Brush("#9aa3ab");
        return button;
    }

    private void AdjustPhotoScale(bool isClassified, int delta)
    {
        if (isClassified)
        {
            var next = Math.Clamp(classifiedPhotoScaleLevel + delta, -PhotoScaleLimit, PhotoScaleLimit);
            if (next == classifiedPhotoScaleLevel)
            {
                return;
            }

            classifiedPhotoScaleLevel = next;
            RefreshTopBar();
            RefreshRight();
            return;
        }

        var unclassifiedNext = Math.Clamp(unclassifiedPhotoScaleLevel + delta, -PhotoScaleLimit, PhotoScaleLimit);
        if (unclassifiedNext == unclassifiedPhotoScaleLevel)
        {
            return;
        }

        unclassifiedPhotoScaleLevel = unclassifiedNext;
        RefreshTopBar();
        RefreshCenter();
    }

    private static int ScaledPhotoSize(int baseSize, int level)
    {
        var scale = 1.0 + level * 0.15;
        return Math.Max(1, (int)Math.Round(baseSize * scale));
    }

    private async Task OpenFolderAsync()
    {
        if (isLoading)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "기준 폴더 선택",
            AllowMultiple = false
        });

        var rootDir = folders.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(rootDir))
        {
            return;
        }

        try
        {
            SetLoading(true, "폴더를 스캔하는 중...", indeterminate: true);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

            var loaded = await Task.Run(() =>
            {
                var scanned = scanner.Scan(rootDir);
                var loadedState = metadataStore.Load(rootDir);
                return (scanned, loadedState);
            });

            scanResult = loaded.scanned;
            state = loaded.loadedState;
            expandedExplorerPaths.Clear();
            expandedUnclassifiedPaths.Clear();
            expandedExplorerPaths.Add(UnclassifiedRootKey);
            expandedUnclassifiedPaths.Add(UnclassifiedRootKey);
            selectedPhoto = string.Empty;
            selectedGroupId = state.Groups.FirstOrDefault()?.Id ?? string.Empty;
            await PreloadThumbnailsAsync(rootDir, scanResult.Photos);
            status.Text = $"{scanResult.Photos.Count}장의 사진을 불러왔습니다.";
            RefreshAll();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task PreloadThumbnailsAsync(string rootDir, IReadOnlyList<PhotoItem> photos)
    {
        if (photos.Count == 0)
        {
            SetLoading(true, "표시할 사진을 준비하는 중...", indeterminate: true);
            return;
        }

        SetLoading(true, $"사진 썸네일을 준비하는 중... 0 / {photos.Count}", indeterminate: false, value: 0, maximum: photos.Count);
        const int concurrency = 4;
        var completed = 0;
        using var throttler = new SemaphoreSlim(concurrency);
        var tasks = photos.Select(async photo =>
        {
            await throttler.WaitAsync();
            try
            {
                await thumbnailService.LoadAsync(rootDir, photo.RelativePath);
            }
            finally
            {
                throttler.Release();
                var done = Interlocked.Increment(ref completed);
                await Dispatcher.UIThread.InvokeAsync(() =>
                    SetLoading(true, $"사진 썸네일을 준비하는 중... {done} / {photos.Count}", indeterminate: false, value: done, maximum: photos.Count));
            }
        });

        await Task.WhenAll(tasks);
    }

    private void SetLoading(bool visible, string message = "로딩 중...", bool indeterminate = true, double value = 0, double maximum = 1)
    {
        isLoading = visible;
        loadingOverlay.IsVisible = visible;
        loadingOverlay.IsHitTestVisible = visible;
        loadingMessage.Text = message;
        loadingProgress.IsIndeterminate = indeterminate;
        loadingProgress.Maximum = Math.Max(1, maximum);
        loadingProgress.Value = Math.Clamp(value, 0, loadingProgress.Maximum);
        topBarHost.IsEnabled = !visible;
        mainGrid.IsEnabled = !visible;
        status.Text = visible ? message : status.Text;
    }

    private void RefreshAll()
    {
        RefreshPanelHeaders();
        RefreshTopBar();
        RefreshLeft();
        RefreshCenter();
        RefreshRight();
        RefreshDetail();
    }

    private void RefreshPanelHeaders()
    {
        var totalExplorer = scanResult.Folders.Count + scanResult.Photos.Count + scanResult.OtherFiles.Count;
        var photoCount = scanResult.Photos.Count;
        var photos = scanResult.Photos
            .Select(photo => AppState.NormalizePath(photo.RelativePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var assigned = state.AssignedSet();
        var classifiedCount = assigned.Count(path => photos.Contains(AppState.NormalizePath(path)));

        explorerHeader.Text = string.IsNullOrWhiteSpace(state.RootDir)
            ? "탐색기 영역"
            : $"탐색기 영역 : 파일 {totalExplorer}개 / 사진 {photoCount}개";
        unclassifiedHeader.Text = string.IsNullOrWhiteSpace(state.RootDir)
            ? "분류되지 않은 영역"
            : $"분류되지 않은 영역 : 사진 {photoCount}개";
        classifiedHeader.Text = string.IsNullOrWhiteSpace(state.RootDir)
            ? "분류된 영역"
            : $"분류된 영역 : 공종 {state.Groups.Count}개 / 사진 {classifiedCount}개";
        var hasRoot = !string.IsNullOrWhiteSpace(state.RootDir);
        if (unclassifiedHeaderActions is not null)
        {
            unclassifiedHeaderActions.IsVisible = hasRoot;
        }
        if (classifiedHeaderActions is not null)
        {
            classifiedHeaderActions.IsVisible = hasRoot;
        }
    }

    private void RefreshTopBar()
    {
        topBarHost.Content = BuildTopBar();
    }

    private void RefreshLeft()
    {
        leftPanel.Children.Clear();
        leftPanel.Margin = new Thickness(10);

        if (string.IsNullOrWhiteSpace(state.RootDir))
        {
            leftPanel.Children.Add(new TextBlock
            {
                Text = "기준 폴더 없음",
                Foreground = Brush("#6d747b"),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        leftPanel.Spacing = 0;
        leftPanel.Children.Add(BuildExplorerTree());
    }

    private Control BuildExplorerTree()
    {
        var root = BuildOpenedFolderRoot(includeOtherFiles: true);
        var tree = new StackPanel
        {
            Spacing = 0,
            Margin = new Thickness(0, 4, 0, 0)
        };

        var rowIndex = 0;
        AddExplorerRows(tree, root, 0, ref rowIndex);
        return tree;
    }

    private ExplorerNode BuildOpenedFolderRoot(bool includeOtherFiles)
    {
        var root = new ExplorerNode(string.Empty, true, string.Empty);

        foreach (var folder in scanResult.Folders)
        {
            AddExplorerPath(root, folder.RelativePath, true);
        }

        foreach (var photo in scanResult.Photos)
        {
            AddExplorerPath(root, photo.RelativePath, false);
        }

        if (includeOtherFiles)
        {
            foreach (var file in scanResult.OtherFiles)
            {
                AddExplorerPath(root, file.RelativePath, false);
            }
        }

        var rootName = OpenedRootName();
        var selectedRoot = new ExplorerNode(rootName, true, UnclassifiedRootKey);
        selectedRoot.Children.AddRange(root.Children);
        return selectedRoot;
    }

    private string OpenedRootName()
    {
        var rootName = string.IsNullOrWhiteSpace(state.RootDir)
            ? "Root"
            : Path.GetFileName(state.RootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(rootName) ? state.RootDir : rootName;
    }

    private static IEnumerable<ExplorerNode> OrderedExplorerChildren(ExplorerNode node)
    {
        return node.Children
            .OrderByDescending(x => x.IsDirectory)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static void AddExplorerPath(ExplorerNode root, string relativePath, bool isDirectory)
    {
        var parts = SplitRelativePath(relativePath);
        if (parts.Length == 0)
        {
            return;
        }

        var current = root;
        for (var i = 0; i < parts.Length; i++)
        {
            var isLast = i == parts.Length - 1;
            var childIsDirectory = !isLast || isDirectory;
            var child = current.Children.FirstOrDefault(x =>
                x.IsDirectory == childIsDirectory &&
                string.Equals(x.Name, parts[i], StringComparison.OrdinalIgnoreCase));

            if (child is null)
            {
                child = new ExplorerNode(
                    parts[i],
                    childIsDirectory,
                    ExplorerKey(current.Key, parts[i]));
                current.Children.Add(child);
            }

            if (childIsDirectory)
            {
                current = child;
            }
        }
    }

    private void AddExplorerRows(StackPanel tree, ExplorerNode node, int depth, ref int rowIndex)
    {
        tree.Children.Add(ExplorerRow(node, depth, rowIndex++));
        if (!node.IsDirectory || !expandedExplorerPaths.Contains(node.Key))
        {
            return;
        }

        foreach (var child in OrderedExplorerChildren(node))
        {
            AddExplorerRows(tree, child, depth + 1, ref rowIndex);
        }
    }

    private Control ExplorerRow(ExplorerNode node, int depth, int rowIndex)
    {
        var isPhoto = IsExplorerPhotoNode(node);
        var isAssignedPhoto = isPhoto && state.AssignedSet().Contains(AppState.NormalizePath(node.Key));
        var extension = node.IsDirectory ? string.Empty : Path.GetExtension(node.Name);
        var row = new Border
        {
            Background = node.IsDirectory ? Brush("#f7f9fa") : isPhoto ? Brushes.White : Brush("#fff1f1"),
            Height = 34,
            Padding = new Thickness(Math.Min(depth * 18, 96), 0, 4, 0)
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (node.IsDirectory)
        {
            content.Children.Add(ExplorerDisclosure(node));
        }
        else
        {
            content.Children.Add(new Border { Width = 18 });
        }

        content.Children.Add(node.IsDirectory ? FolderGlyph() : FileGlyph());
        content.Children.Add(EditableExplorerNameHost(node, isPhoto, isAssignedPhoto));
        if (isPhoto && !string.IsNullOrWhiteSpace(extension))
        {
            content.Children.Add(ExplorerExtensionTag(extension, isPhoto));
        }

        row.Child = content;
        if (node.IsDirectory)
        {
            row.PointerPressed += (_, args) =>
            {
                if (!args.GetCurrentPoint(row).Properties.IsLeftButtonPressed)
                {
                    return;
                }

                ToggleExplorerFolder(node.Key);
                args.Handled = true;
            };
        }
        return row;
    }

    private Control ExplorerDisclosure(ExplorerNode node)
    {
        var isExpanded = expandedExplorerPaths.Contains(node.Key);
        var arrow = new TextBlock
        {
            Text = isExpanded ? "▾" : "▸",
            Width = 18,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = node.Children.Count == 0 ? Brush("#b8c0c8") : Brush("#3f464d"),
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (node.Children.Count > 0)
        {
            arrow.PointerPressed += (_, args) =>
            {
                ToggleExplorerFolder(node.Key);
                args.Handled = true;
            };
        }

        return arrow;
    }

    private void ToggleExplorerFolder(string key)
    {
        ToggleSharedFolder(key);
        RefreshLeft();
        RefreshCenter();
    }

    private bool IsExplorerPhotoNode(ExplorerNode node)
    {
        return !node.IsDirectory &&
            scanResult.Photos.Any(photo => string.Equals(
                AppState.NormalizePath(photo.RelativePath),
                AppState.NormalizePath(node.Key),
                StringComparison.OrdinalIgnoreCase));
    }

    private Control EditableExplorerNameHost(ExplorerNode node, bool isPhoto, bool isAssignedPhoto)
    {
        if (string.Equals(node.Key, UnclassifiedRootKey, StringComparison.OrdinalIgnoreCase))
        {
            return ExplorerNameText(node.Name);
        }

        if (!node.IsDirectory && !isPhoto)
        {
            return ExplorerNameText(node.Name);
        }

        var width = ExplorerFileNameWidth(EditableName(node.Key, node.IsDirectory));
        var foreground = isAssignedPhoto ? Brush("#c7ced6") : Brush("#202428");
        return EditableFileNameHost(node.Key, 14, foreground, new Thickness(0), width, TextAlignment.Left, node.IsDirectory);
    }

    private static TextBlock ExplorerNameText(string text)
    {
        return new TextBlock
        {
            Text = text,
            Width = ExplorerFileNameWidth(text),
            FontSize = 14,
            Foreground = Brush("#202428"),
            TextAlignment = TextAlignment.Left,
            TextWrapping = TextWrapping.NoWrap,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static double ExplorerFileNameWidth(string text)
    {
        var width = 28.0;
        foreach (var ch in text)
        {
            width += ch > 127 ? 16 : 9;
        }

        return Math.Max(48, width);
    }

    private static Control ExplorerExtensionTag(string extension, bool isPhoto)
    {
        return new Border
        {
            MinWidth = 34,
            Height = 24,
            Padding = new Thickness(8, 0),
            Background = isPhoto ? Brush("#f1f3f5") : Brush("#ffc4c4"),
            CornerRadius = new CornerRadius(5),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = extension,
                FontSize = 14,
                Foreground = Brush("#202428"),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static Control FolderGlyph()
    {
        var icon = new Grid
        {
            Width = 24,
            Height = 20,
            Margin = new Thickness(0, 0, 2, 0)
        };

        icon.Children.Add(new Border
        {
            Width = 10,
            Height = 5,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(3, 2, 0, 0),
            Background = Brush("#74c9ec"),
            CornerRadius = new CornerRadius(2, 2, 0, 0)
        });
        icon.Children.Add(new Border
        {
            Width = 20,
            Height = 13,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(2, 6, 0, 0),
            Background = Brush("#42b4df"),
            BorderBrush = Brush("#2e9ec8"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2)
        });

        return icon;
    }

    private static Control FileGlyph()
    {
        return new Border
        {
            Width = 16,
            Height = 19,
            Margin = new Thickness(4, 0, 6, 0),
            Background = Brush("#ffffff"),
            BorderBrush = Brush("#d2d7dc"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(2)
        };
    }

    private static string[] SplitRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath == ".")
        {
            return [];
        }

        return relativePath
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string ExplorerKey(string parentKey, string name)
    {
        return string.IsNullOrWhiteSpace(parentKey)
            ? name
            : parentKey.TrimEnd('/', '\\') + "/" + name;
    }

    private void RefreshCenter()
    {
        centerPanel.Children.Clear();
        centerPanel.RowDefinitions = new RowDefinitions("*");
        if (string.IsNullOrWhiteSpace(state.RootDir))
        {
            return;
        }

        var photoSection = new DockPanel();
        ConfigureUnclassifiedDropTarget(photoSection);
        DockPanel.SetDock(photoSection, Dock.Top);
        var photoHeader = new StackPanel { Margin = new Thickness(10, 8, 10, 4), Spacing = 2 };
        if (scanResult.Photos.Count > 0)
        {
            photoHeader.Children.Add(new TextBlock { Text = "사진을 오른쪽 영역으로 드래그하거나, 분류된 사진을 이곳으로 드래그해 제외하세요.", Foreground = Brush("#69737d") });
            DockPanel.SetDock(photoHeader, Dock.Top);
            photoSection.Children.Add(photoHeader);
        }
        photoSection.Children.Add(new ScrollViewer { Content = BuildUnclassifiedTree() });

        AddToGrid(centerPanel, photoSection, 0, 0);
    }

    private Control BuildUnclassifiedTree()
    {
        var assignedGroupNumbers = AssignedGroupNumbers();
        var root = new ExplorerNode(string.Empty, true, string.Empty);
        foreach (var photo in scanResult.Photos)
        {
            AddExplorerPath(root, photo.RelativePath, false);
        }

        var tree = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(8)
        };
        ConfigureUnclassifiedDropTarget(tree);

        if (root.Children.Count == 0)
        {
            tree.Children.Add(new TextBlock
            {
                Text = "분류되지 않은 사진이 없습니다.",
                Foreground = Brush("#69737d"),
                Margin = new Thickness(4)
            });
            return tree;
        }

        var rootName = string.IsNullOrWhiteSpace(state.RootDir)
            ? "Root"
            : Path.GetFileName(state.RootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = state.RootDir;
        }

        var selectedRoot = new ExplorerNode(rootName, true, UnclassifiedRootKey);
        selectedRoot.Children.AddRange(root.Children);
        AddUnclassifiedRows(tree, selectedRoot, 0, assignedGroupNumbers);

        return tree;
    }

    private Dictionary<string, int> AssignedGroupNumbers()
    {
        var assigned = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < state.Groups.Count; i++)
        {
            foreach (var path in state.Groups[i].Before.Concat(state.Groups[i].Processing).Concat(state.Groups[i].After))
            {
                assigned[AppState.NormalizePath(path)] = i + 1;
            }
        }

        return assigned;
    }

    private void AddUnclassifiedRows(StackPanel tree, ExplorerNode node, int depth, IReadOnlyDictionary<string, int> assignedGroupNumbers)
    {
        if (!node.IsDirectory)
        {
            AddUnclassifiedPhotoWrap(tree, [node], depth, assignedGroupNumbers);
            return;
        }

        tree.Children.Add(UnclassifiedFolderRow(node, depth));
        if (!expandedUnclassifiedPaths.Contains(node.Key))
        {
            return;
        }

        var pendingPhotos = new List<ExplorerNode>();
        foreach (var child in OrderedExplorerChildren(node))
        {
            if (!child.IsDirectory)
            {
                pendingPhotos.Add(child);
                continue;
            }

            AddUnclassifiedPhotoWrap(tree, pendingPhotos, depth + 1, assignedGroupNumbers);
            pendingPhotos.Clear();
            AddUnclassifiedRows(tree, child, depth + 1, assignedGroupNumbers);
        }

        AddUnclassifiedPhotoWrap(tree, pendingPhotos, depth + 1, assignedGroupNumbers);
    }

    private void AddUnclassifiedPhotoWrap(StackPanel tree, List<ExplorerNode> photos, int depth, IReadOnlyDictionary<string, int> assignedGroupNumbers)
    {
        if (photos.Count == 0)
        {
            return;
        }

        var wrap = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(Math.Min(depth * 24, 120), 0, 0, 6)
        };
        ConfigureUnclassifiedDropTarget(wrap);
        foreach (var photo in photos)
        {
            assignedGroupNumbers.TryGetValue(AppState.NormalizePath(photo.Key), out var groupNumber);
            wrap.Children.Add(PhotoCard(photo.Key, groupNumber == 0 ? null : groupNumber));
        }

        tree.Children.Add(wrap);
    }

    private Control UnclassifiedFolderRow(ExplorerNode node, int depth)
    {
        var isExpanded = expandedUnclassifiedPaths.Contains(node.Key);
        var row = new Border
        {
            Height = 34,
            Padding = new Thickness(Math.Min(depth * 24, 120), 0, 8, 0),
            Background = Brush("#f7f9fa")
        };

        var content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = isExpanded ? "▾" : "▸",
                    Width = 18,
                    FontSize = 13,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brush("#3f464d"),
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                },
                FolderGlyph(),
                new TextBlock
                {
                    Text = node.Name,
                    FontSize = 14,
                    Foreground = Brush("#202428"),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            }
        };

        row.Child = content;
        row.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(row).Properties.IsLeftButtonPressed)
            {
                return;
            }

            ToggleUnclassifiedFolder(node.Key);
            args.Handled = true;
        };
        ConfigureUnclassifiedDropTarget(row);
        return row;
    }

    private void ToggleUnclassifiedFolder(string key)
    {
        ToggleSharedFolder(key);
        RefreshLeft();
        RefreshCenter();
    }

    private void ToggleSharedFolder(string key)
    {
        var shouldExpand = !expandedExplorerPaths.Contains(key) || !expandedUnclassifiedPaths.Contains(key);
        if (shouldExpand)
        {
            expandedExplorerPaths.Add(key);
            expandedUnclassifiedPaths.Add(key);
            return;
        }

        RemoveExpandedPathWithChildren(expandedExplorerPaths, key);
        RemoveExpandedPathWithChildren(expandedUnclassifiedPaths, key);
    }

    private static void RemoveExpandedPathWithChildren(HashSet<string> paths, string key)
    {
        var prefix = key.TrimEnd('/') + "/";
        paths.RemoveWhere(path =>
            string.Equals(path, key, StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private void RefreshRight()
    {
        rightPanel.Children.Clear();
        rightPanel.Margin = new Thickness(8, 8, 8, 0);
        if (string.IsNullOrWhiteSpace(state.RootDir))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(selectedGroupId) && state.GroupById(selectedGroupId) is null)
        {
            selectedGroupId = state.Groups.FirstOrDefault()?.Id ?? string.Empty;
        }

        if (state.Groups.Count == 0)
        {
            rightPanel.Children.Add(new TextBlock
            {
                Text = "아직 공종이 없습니다. 공종 추가를 눌러 시작하세요.",
                Foreground = Brush("#69737d")
            });
            return;
        }

        for (var i = 0; i < state.Groups.Count; i++)
        {
            rightPanel.Children.Add(GroupRow(state.Groups[i], i + 1));
        }
    }

    private Control GroupRow(PhotoGroup group, int number)
    {
        var root = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush("#d0d6dc"),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(0),
            Margin = new Thickness(0)
        };

        var stack = new StackPanel { Spacing = 0 };
        var header = new Grid
        {
            Height = 54,
            Background = Brushes.White,
            ColumnDefinitions = new ColumnDefinitions("52,*,Auto,34")
        };

        AddToGrid(header, new TextBlock
        {
            Text = number.ToString(),
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = Brush("#161a1d"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        }, 0, 0);

        var titleHost = new ContentControl();
        void CommitTitle(TextBox titleBox)
        {
            if (!ReferenceEquals(titleHost.Content, titleBox))
            {
                return;
            }

            var nextTitle = titleBox.Text?.Trim() ?? string.Empty;
            var changed = !string.Equals(group.Title, nextTitle, StringComparison.Ordinal);
            group.Title = nextTitle;
            if (changed)
            {
                SaveToMetadata("구역 이름을 저장했습니다", refreshAfterSave: false);
            }
            titleHost.Content = CreateGroupTitleDisplay(group, BeginTitleEdit);
        }

        void BeginTitleEdit()
        {
            SelectGroupForPreview(group.Id);
            var titleBox = new TextBox
            {
                Text = group.Title,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                SelectionBrush = Brushes.Transparent,
                SelectionForegroundBrush = Brush("#1f252a"),
                CaretBrush = Brush("#1f252a"),
                FontSize = 13,
                Foreground = Brush("#1f252a"),
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(2, 0),
                TextAlignment = TextAlignment.Left,
                TextWrapping = TextWrapping.NoWrap,
                Watermark = "공종 이름을 입력해주세요"
            };
            titleBox.Resources["TextControlBackground"] = Brushes.Transparent;
            titleBox.Resources["TextControlBackgroundPointerOver"] = Brushes.Transparent;
            titleBox.Resources["TextControlBackgroundFocused"] = Brushes.Transparent;
            titleBox.Resources["TextControlBorderBrush"] = Brushes.Transparent;
            titleBox.Resources["TextControlBorderBrushPointerOver"] = Brushes.Transparent;
            titleBox.Resources["TextControlBorderBrushFocused"] = Brushes.Transparent;
            titleBox.Resources["TextControlForeground"] = Brush("#1f252a");
            titleBox.Resources["TextControlForegroundFocused"] = Brush("#1f252a");
            titleBox.Resources["TextControlForegroundPointerOver"] = Brush("#1f252a");
            titleBox.AttachedToVisualTree += (_, _) =>
            {
                titleBox.Focus();
                titleBox.CaretIndex = titleBox.Text?.Length ?? 0;
            };
            titleBox.LostFocus += (_, _) =>
            {
                if (ReferenceEquals(titleHost.Content, titleBox))
                {
                    titleHost.Content = CreateGroupTitleDisplay(group, BeginTitleEdit);
                }
            };
            titleBox.KeyDown += (_, args) =>
            {
                if (args.Key != Key.Enter)
                {
                    return;
                }

                CommitTitle(titleBox);
                args.Handled = true;
            };
            titleHost.Content = titleBox;
        }

        titleHost.Content = CreateGroupTitleDisplay(group, BeginTitleEdit);
        AddToGrid(header, titleHost, 1, 0);

        Point? rowSelectStart = null;
        root.PointerPressed += (_, args) =>
        {
            if (args.Source is TextBox or Button or ComboBox)
            {
                return;
            }

            var point = args.GetCurrentPoint(root);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            rowSelectStart = point.Position;
        };
        root.PointerReleased += (_, args) =>
        {
            if (rowSelectStart is null || args.Source is TextBox or Button or ComboBox)
            {
                rowSelectStart = null;
                return;
            }

            var delta = args.GetPosition(root) - rowSelectStart.Value;
            rowSelectStart = null;
            if (Math.Abs(delta.X) >= 6 || Math.Abs(delta.Y) >= 6)
            {
                return;
            }

            if (selectedGroupId != group.Id)
            {
                selectedGroupId = group.Id;
                RefreshRight();
            }

            RefreshDetail();
        };

        var remove = new Button
        {
            Content = "×",
            Width = 40,
            Height = 34,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = Brush("#d92d20"),
            BorderThickness = new Thickness(0),
            FontSize = 18,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        remove.Resources["ButtonForeground"] = Brush("#d92d20");
        remove.Resources["ButtonForegroundPointerOver"] = Brush("#d92d20");
        remove.Resources["ButtonForegroundPressed"] = Brush("#b42318");
        remove.Resources["ButtonBackground"] = Brushes.Transparent;
        remove.Resources["ButtonBackgroundPointerOver"] = Brush("#fff1f0");
        remove.Resources["ButtonBackgroundPressed"] = Brush("#ffe4e2");
        remove.Resources["ButtonBorderBrush"] = Brushes.Transparent;
        remove.Resources["ButtonBorderBrushPointerOver"] = Brushes.Transparent;
        remove.Resources["ButtonBorderBrushPressed"] = Brushes.Transparent;
        remove.Click += (_, _) =>
        {
            state.RemoveGroup(group.Id);
            RefreshAll();
        };
        AddToGrid(header, remove, 3, 0);

        var cntPerPage = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };
        cntPerPage.Children.Add(new TextBlock
        {
            Text = "한 페이지당 사진 수",
            Foreground = Brush("#1f252a"),
            VerticalAlignment = VerticalAlignment.Center
        });
        var cntCombo = new ComboBox
        {
            ItemsSource = new[] { 3, 4 },
            SelectedItem = group.CntPerPage == 4 ? 4 : 3,
            Width = 58,
            Height = 26,
            Padding = new Thickness(6, 0),
            FontSize = 13,
            Foreground = Brush("#1f252a"),
            Background = Brushes.White,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center
        };
        cntCombo.Resources["ComboBoxForeground"] = Brush("#1f252a");
        cntCombo.Resources["ComboBoxForegroundPointerOver"] = Brush("#1f252a");
        cntCombo.Resources["ComboBoxForegroundPressed"] = Brush("#1f252a");
        cntCombo.Resources["ComboBoxBackground"] = Brushes.White;
        cntCombo.Resources["ComboBoxBackgroundPointerOver"] = Brush("#f1f3f5");
        cntCombo.Resources["ComboBoxBackgroundPressed"] = Brush("#e9ecef");
        cntCombo.Resources["ComboBoxBorderBrush"] = Brushes.Transparent;
        cntCombo.Resources["ComboBoxBorderBrushPointerOver"] = Brushes.Transparent;
        cntCombo.Resources["ComboBoxBorderBrushFocused"] = Brushes.Transparent;
        cntCombo.Resources["ComboBoxBorderBrushPressed"] = Brushes.Transparent;
        cntCombo.SelectionChanged += (_, _) =>
        {
            if (cntCombo.SelectedItem is int count && group.CntPerPage != count)
            {
                group.CntPerPage = count;
                SaveToMetadata("페이지당 사진 수를 저장했습니다", refreshAfterSave: false);
                RefreshAll();
            }
        };
        cntPerPage.Children.Add(cntCombo);
        AddToGrid(header, cntPerPage, 2, 0);
        stack.Children.Add(header);

        var phases = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0 };
        foreach (var phase in new[] { Phase.Before, Phase.Processing, Phase.After })
        {
            phases.Children.Add(PhaseBox(group, phase));
        }
        stack.Children.Add(new ScrollViewer
        {
            Content = phases,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        });
        root.Child = stack;
        ConfigureGroupReorder(root, group);
        return root;
    }

    private Control CreateGroupTitleDisplay(PhotoGroup group, Action beginEdit)
    {
        var hasTitle = !string.IsNullOrWhiteSpace(group.Title);
        var title = new TextBlock
        {
            Text = hasTitle ? group.Title : "공종 이름을 입력해주세요",
            Foreground = hasTitle ? Brush("#1f252a") : Brush("#9aa3ab"),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        title.PointerPressed += (_, args) =>
        {
            if (args.GetCurrentPoint(title).Properties.IsLeftButtonPressed)
            {
                beginEdit();
                args.Handled = true;
            }
        };
        return title;
    }

    private void MoveGroup(string groupId, int delta)
    {
        var index = state.Groups.FindIndex(group => group.Id == groupId);
        var next = index + delta;
        if (index < 0 || next < 0 || next >= state.Groups.Count)
        {
            return;
        }

        var group = state.Groups[index];
        state.Groups.RemoveAt(index);
        state.Groups.Insert(next, group);
        RefreshAll();
    }

    private void MoveGroupTo(string sourceGroupId, string targetGroupId)
    {
        if (sourceGroupId == targetGroupId)
        {
            return;
        }

        var sourceIndex = state.Groups.FindIndex(group => group.Id == sourceGroupId);
        var targetIndex = state.Groups.FindIndex(group => group.Id == targetGroupId);
        if (sourceIndex < 0 || targetIndex < 0)
        {
            return;
        }

        var group = state.Groups[sourceIndex];
        state.Groups.RemoveAt(sourceIndex);
        state.Groups.Insert(targetIndex, group);
        selectedGroupId = sourceGroupId;
        SaveToMetadata("구역 순서를 저장했습니다", refreshAfterSave: false);
        RefreshAll();
    }

    private Control PhaseBox(PhotoGroup group, Phase phase)
    {
        group.NormalizeLabels();
        var photos = group.Photos(phase);
        var slotWidth = ScaledPhotoSize(150, classifiedPhotoScaleLevel);
        var slotCount = Math.Max(1, photos.Count);
        var contentWidth = slotCount * slotWidth;
        var box = new Border
        {
            Width = contentWidth,
            MinHeight = ScaledPhotoSize(196, classifiedPhotoScaleLevel),
            CornerRadius = new CornerRadius(0),
            Background = Brushes.White,
            BorderBrush = Brush("#e3e8ed"),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(0, 10, 0, 14)
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0 };
        if (photos.Count == 0)
        {
            row.Children.Add(AssignedPhotoCard(group, phase, string.Empty, group.LabelAt(phase, 0), 0, slotWidth));
        }
        else
        {
            for (var index = 0; index < photos.Count; index++)
            {
                row.Children.Add(AssignedPhotoCard(group, phase, photos[index], group.LabelAt(phase, index), index, slotWidth));
            }
        }

        box.Child = row;
        ConfigureDropTarget(box, group, phase);
        return box;
    }

    private void RefreshDetail()
    {
        detailPanel.Children.Clear();
        detailPanel.Margin = new Thickness(0);

        var group = state.GroupById(selectedGroupId);
        if (group is null)
        {
            detailPanel.Margin = new Thickness(12);
            detailPanel.Children.Add(new TextBlock
            {
                Text = "공종을 선택해주세요.",
                Foreground = Brush("#69737d"),
                TextAlignment = TextAlignment.Left,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0)
            });
            return;
        }

        var root = new DockPanel
        {
            Background = Brushes.White
        };

        var content = new StackPanel
        {
            Margin = new Thickness(0),
            Spacing = 0
        };

        foreach (var phase in new[] { Phase.Before, Phase.Processing, Phase.After })
        {
            content.Children.Add(DetailPhaseSection(group, phase, phase != Phase.After));
        }

        root.Children.Add(new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        });

        detailPanel.Children.Add(root);
    }

    private Control DetailPhaseSection(PhotoGroup group, Phase phase, bool showBottomBorder)
    {
        var section = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush("#d0d6dc"),
            BorderThickness = showBottomBorder ? new Thickness(0, 0, 0, 1) : new Thickness(0),
            CornerRadius = new CornerRadius(0),
            Padding = new Thickness(0),
            MinHeight = 220,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var stack = new StackPanel { Spacing = 0 };
        group.NormalizeLabels();
        var photos = group.Photos(phase);

        if (photos.Count == 0)
        {
            stack.Children.Add(DetailPhotoRow(phase, group.LabelAt(phase, 0), string.Empty));
        }
        else
        {
            for (var index = 0; index < photos.Count; index++)
            {
                stack.Children.Add(DetailPhotoRow(phase, group.LabelAt(phase, index), photos[index]));
            }
        }

        section.Child = stack;
        ConfigureDropTarget(section, group, phase);
        return section;
    }

    private Control DetailPhotoRow(Phase phase, string label, string relativePath)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("46,*"),
            MinHeight = 320,
            Background = Brushes.White
        };

        AddToGrid(row, new Border
        {
            BorderBrush = Brush("#d0d6dc"),
            BorderThickness = new Thickness(0, 0, 1, 0),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.Black,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        }, 0, 0);

        if (!string.IsNullOrEmpty(relativePath))
        {
            AddToGrid(row, DetailPhotoCard(phase, relativePath), 1, 0);
        }

        return row;
    }

    private Control DetailPhotoCard(Phase phase, string relativePath)
    {
        var absolutePath = FileScanner.ToAbsolutePath(state.RootDir, relativePath);
        var image = new Image
        {
            MinHeight = 260,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Source = File.Exists(absolutePath) ? new Bitmap(absolutePath) : null
        };

        var card = new Border
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 18),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Child = new DockPanel
            {
                Children =
                {
                    EditableFileNameHost(relativePath, 12, Brush("#69737d"), new Thickness(0, 10, 0, 0), 260),
                    image
                }
            }
        };
        if (card.Child is DockPanel panel && panel.Children[0] is Control name)
        {
            DockPanel.SetDock(name, Dock.Bottom);
        }
        return card;
    }

    private Task ShowImagePreviewAsync(string relativePath)
    {
        var absolutePath = FileScanner.ToAbsolutePath(state.RootDir, relativePath);
        if (!File.Exists(absolutePath))
        {
            status.Text = "사진 파일을 찾을 수 없습니다: " + relativePath;
            return Task.CompletedTask;
        }

        var bitmap = new Bitmap(absolutePath);
        var image = new Image
        {
            Source = bitmap,
            Stretch = Stretch.Fill
        };
        var zoomLabel = new TextBlock
        {
            Width = 70,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right
        };
        var slider = new Slider
        {
            Minimum = 0.1,
            Maximum = 4,
            TickFrequency = 0.1,
            Width = 260,
            VerticalAlignment = VerticalAlignment.Center
        };

        void ApplyZoom(double zoom)
        {
            image.Width = Math.Max(1, bitmap.PixelSize.Width * zoom);
            image.Height = Math.Max(1, bitmap.PixelSize.Height * zoom);
            zoomLabel.Text = $"{zoom:P0}";
        }

        var fitZoom = Math.Min(1.0, Math.Min(900.0 / Math.Max(1, bitmap.PixelSize.Width), 580.0 / Math.Max(1, bitmap.PixelSize.Height)));
        slider.Value = Math.Clamp(fitZoom, 0.1, 4.0);
        ApplyZoom(slider.Value);
        slider.PropertyChanged += (_, args) =>
        {
            if (args.Property == RangeBase.ValueProperty)
            {
                ApplyZoom(slider.Value);
            }
        };

        var scrollViewer = new ScrollViewer
        {
            Content = image,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brush("#1f252a")
        };
        scrollViewer.GestureRecognizers.Add(new PinchGestureRecognizer());
        void ApplyPointerZoom(Point pointerPosition, double nextZoom)
        {
            var previousZoom = Math.Max(0.001, slider.Value);
            nextZoom = Math.Clamp(nextZoom, slider.Minimum, slider.Maximum);
            if (Math.Abs(nextZoom - previousZoom) < 0.001)
            {
                return;
            }

            var imagePointX = (scrollViewer.Offset.X + pointerPosition.X) / previousZoom;
            var imagePointY = (scrollViewer.Offset.Y + pointerPosition.Y) / previousZoom;
            slider.Value = nextZoom;

            Dispatcher.UIThread.Post(() =>
            {
                var maxX = Math.Max(0, image.Bounds.Width - scrollViewer.Viewport.Width);
                var maxY = Math.Max(0, image.Bounds.Height - scrollViewer.Viewport.Height);
                scrollViewer.Offset = new Vector(
                    Math.Clamp(imagePointX * nextZoom - pointerPosition.X, 0, maxX),
                    Math.Clamp(imagePointY * nextZoom - pointerPosition.Y, 0, maxY));
            });
        }

        var isPanning = false;
        var panStart = new Point();
        var panStartOffset = new Vector();
        double? pinchStartZoom = null;
        scrollViewer.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(scrollViewer).Properties.IsLeftButtonPressed)
            {
                return;
            }

            isPanning = true;
            panStart = args.GetPosition(scrollViewer);
            panStartOffset = scrollViewer.Offset;
            args.Pointer.Capture(scrollViewer);
            args.Handled = true;
        };
        scrollViewer.PointerMoved += (_, args) =>
        {
            var current = args.GetPosition(scrollViewer);
            if (!isPanning)
            {
                return;
            }

            var delta = current - panStart;
            scrollViewer.Offset = new Vector(
                Math.Max(0, panStartOffset.X - delta.X),
                Math.Max(0, panStartOffset.Y - delta.Y));
            args.Handled = true;
        };
        scrollViewer.PointerReleased += (_, args) =>
        {
            if (!isPanning)
            {
                return;
            }

            isPanning = false;
            args.Pointer.Capture(null);
            args.Handled = true;
        };
        scrollViewer.PointerCaptureLost += (_, _) => isPanning = false;
        scrollViewer.PointerWheelChanged += (_, args) =>
        {
            var delta = args.Delta.Y;
            if (Math.Abs(delta) < 0.001)
            {
                return;
            }

            var zoomFactor = Math.Pow(1.12, delta);
            ApplyPointerZoom(args.GetPosition(scrollViewer), slider.Value * zoomFactor);
            args.Handled = true;
        };
        Gestures.AddPointerTouchPadGestureMagnifyHandler(scrollViewer, (_, args) =>
        {
            var delta = Math.Abs(args.Delta.Y) > 0.001 ? args.Delta.Y : args.Delta.X;
            if (Math.Abs(delta) < 0.001)
            {
                return;
            }

            var zoomFactor = Math.Clamp(1.0 + delta, 0.85, 1.15);
            ApplyPointerZoom(args.GetPosition(scrollViewer), slider.Value * zoomFactor);
            args.Handled = true;
        });
        Gestures.AddPinchHandler(scrollViewer, (_, args) =>
        {
            pinchStartZoom ??= slider.Value;
            ApplyPointerZoom(args.ScaleOrigin, pinchStartZoom.Value * args.Scale);
            args.Handled = true;
        });
        Gestures.AddPinchEndedHandler(scrollViewer, (_, args) =>
        {
            pinchStartZoom = null;
            args.Handled = true;
        });

        var zoomControls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = EditableName(relativePath),
                    FontWeight = FontWeight.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                },
                slider,
                zoomLabel
            }
        };
        var toolbar = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(12),
            Children =
            {
                new TextBlock
                {
                    Text = absolutePath,
                    FontSize = 12,
                    Foreground = Brush("#69737d"),
                    TextWrapping = TextWrapping.Wrap
                },
                zoomControls
            }
        };

        var dialog = new Window
        {
            Title = EditableName(relativePath),
            Width = 1000,
            Height = 760,
            MinWidth = 520,
            MinHeight = 420,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new DockPanel
            {
                Children =
                {
                    toolbar,
                    scrollViewer
                }
            }
        };
        DockPanel.SetDock(toolbar, Dock.Top);
        dialog.Deactivated += (_, _) =>
        {
            if (dialog.IsVisible)
            {
                dialog.Close();
            }
        };
        dialog.Closed += (_, _) => bitmap.Dispose();
        dialog.Show(this);
        return Task.CompletedTask;
    }

    private void Save()
    {
        _ = SaveToMetadata("저장했습니다", refreshAfterSave: true);
    }

    private bool SaveToMetadata(string successMessage, bool refreshAfterSave)
    {
        if (string.IsNullOrWhiteSpace(state.RootDir))
        {
            status.Text = "먼저 기준 폴더를 선택하세요.";
            return false;
        }

        try
        {
            metadataStore.Save(state.RootDir, state);
            if (refreshAfterSave)
            {
                scanResult = scanner.Scan(state.RootDir);
                RefreshAll();
            }

            var path = MetadataStore.MetadataPath(state.RootDir);
            status.Text = successMessage + ": " + path;
            ShowAutoSaveFeedback();
            return true;
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync(ex);
            return false;
        }
    }

    private async void ShowAutoSaveFeedback()
    {
        var version = ++autoSaveFeedbackVersion;
        isAutoSaveFeedbackVisible = true;
        RefreshTopBar();
        await Task.Delay(500);
        if (version != autoSaveFeedbackVersion)
        {
            return;
        }

        isAutoSaveFeedbackVisible = false;
        RefreshTopBar();
    }

    private void ClearThumbnailCacheOnExit()
    {
        try
        {
            thumbnailService.ClearCache();
        }
        catch
        {
            // Shutdown should not be interrupted by cache cleanup failures.
        }
    }

    private async Task ExportOneAsync(ExportFormat format)
    {
        if (string.IsNullOrWhiteSpace(state.RootDir))
        {
            status.Text = "먼저 기준 폴더를 선택하세요.";
            return;
        }

        try
        {
            var extension = format == ExportFormat.Hwpx ? "hwpx" : "docx";
            var fileType = new FilePickerFileType(format == ExportFormat.Hwpx ? "HWPX 문서" : "Word 문서")
            {
                Patterns = [$"*.{extension}"],
                AppleUniformTypeIdentifiers = format == ExportFormat.Hwpx
                    ? ["public.zip-archive"]
                    : ["org.openxmlformats.wordprocessingml.document"],
                MimeTypes = format == ExportFormat.Hwpx
                    ? ["application/haansofthwpml"]
                    : ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]
            };
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = format == ExportFormat.Hwpx ? "HWPX로 내보내기" : "DOCX로 내보내기",
                SuggestedFileName = $"sitesnap-{DateTime.Now:yyMMddHHmmss}.{extension}",
                DefaultExtension = extension,
                FileTypeChoices = [fileType]
            });
            var outputPath = file?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                status.Text = "내보내기를 취소했습니다.";
                return;
            }

            if (!outputPath.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))
            {
                outputPath += "." + extension;
            }

            if (format == ExportFormat.Hwpx)
            {
                documentExporter.ExportHwpxTo(state.RootDir, outputPath, state);
            }
            else
            {
                documentExporter.ExportDocxTo(state.RootDir, outputPath, state);
            }

            status.Text = "내보내기 완료: " + outputPath;
            await ShowInfoAsync("내보내기 완료", "내보내기가 완료되었습니다.\n" + outputPath);
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync(ex);
        }
    }

    private enum ExportFormat
    {
        Hwpx,
        Docx
    }

    private sealed class ExplorerNode(string name, bool isDirectory, string key)
    {
        public string Name { get; } = name;
        public bool IsDirectory { get; } = isDirectory;
        public string Key { get; } = key;
        public List<ExplorerNode> Children { get; } = [];
    }

    private sealed record MarginInputSet(TextBox Top, TextBox Bottom, TextBox Left, TextBox Right);

    private sealed record CellMarginInputSet(TextBox Vertical, TextBox Horizontal);

    private sealed record WorkCellInputSet(TextBox Height, TextBox Width);

    private sealed record PaperTemplateInputSet(
        TextBox Title,
        TextBox TitleFontPt,
        TextBox Subtitle,
        TextBox SubtitleFontPt,
        TextBox Company,
        TextBox CompanyFontPt,
        TextBox BodyTitle,
        TextBox BodyTitleFontPt,
        TextBox BodySubtitle,
        TextBox BodySubtitleFontPt,
        TextBox LineSpacingPercent,
        TextBox FontFamily,
        CheckBox ShowPageNumber,
        Slider ImageDpi,
        TextBlock ImageDpiValue,
        Slider JpegQuality,
        TextBlock JpegQualityValue)
    {
        public IEnumerable<TextBox> TextBoxes
        {
            get
            {
                yield return Title;
                yield return TitleFontPt;
                yield return Subtitle;
                yield return SubtitleFontPt;
                yield return Company;
                yield return CompanyFontPt;
                yield return BodyTitle;
                yield return BodyTitleFontPt;
                yield return BodySubtitle;
                yield return BodySubtitleFontPt;
                yield return LineSpacingPercent;
                yield return FontFamily;
            }
        }
    }

    private sealed record SettingsPageInputSet(
        MarginInputSet HwpxPaper,
        CellMarginInputSet HwpxCell,
        CellMarginInputSet HwpxPhoto,
        WorkCellInputSet HwpxWorkCell,
        MarginInputSet DocxPaper,
        CellMarginInputSet DocxCell,
        CellMarginInputSet DocxPhoto,
        WorkCellInputSet DocxWorkCell);

    private async Task ShowInfoAsync(string title, string message)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 520,
            Height = 220,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    new Button
                    {
                        Content = "확인",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        MinWidth = 100
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is Button close)
        {
            close.Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(this);
    }

    private async Task ShowVersionAsync()
    {
        var dialog = new Window
        {
            Title = ProgramName + " 정보",
            Width = 520,
            Height = 325,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 14,
                Children =
                {
                    VersionInfoTable(),
                    new Button
                    {
                        Content = "확인",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        MinWidth = 86
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is Button close)
        {
            close.Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(this);
    }

    private Control VersionInfoTable()
    {
        var table = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("110,*"),
            RowDefinitions = new RowDefinitions("36,36,126")
        };

        AddInfoCell(table, "프로그램", 0, 0, header: true);
        AddInfoCell(table, ProgramName, 1, 0);
        AddInfoCell(table, "버전", 0, 1, header: true);
        AddInfoCell(table, ProgramVersion, 1, 1);
        AddInfoCell(table, "캐시 위치", 0, 2, header: true);
        AddInfoCell(table, CacheInfoContent(thumbnailService.CacheRoot), 1, 2);

        return table;
    }

    private static void AddInfoCell(Grid table, string text, int column, int row, bool header = false)
    {
        AddInfoCell(table, InfoText(text, header), column, row, header);
    }

    private static void AddInfoCell(Grid table, Control content, int column, int row, bool header = false)
    {
        var cell = new Border
        {
            Background = header ? Brush("#f3f5f7") : Brushes.White,
            BorderBrush = Brush("#d8dde2"),
            BorderThickness = new Thickness(0.7),
            Padding = new Thickness(10, 6),
            Child = content
        };
        AddToGrid(table, cell, column, row);
    }

    private static SelectableTextBlock InfoText(string text, bool header = false, IBrush? foreground = null)
    {
        return new SelectableTextBlock
        {
            Text = text,
            FontSize = 13,
            FontWeight = header ? FontWeight.Bold : FontWeight.Normal,
            Foreground = foreground ?? (header ? Brush("#333") : Brush("#222")),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private Control CacheInfoContent(string cacheRoot)
    {
        var clearCache = new Button
        {
            Content = "캐시 클리어",
            MinWidth = 92,
            Height = 28,
            Padding = new Thickness(12, 0),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        clearCache.Click += (_, _) =>
        {
            try
            {
                thumbnailService.ClearCache();
                RefreshAll();
                status.Text = "캐시를 삭제했습니다: " + cacheRoot;
                clearCache.Content = "삭제 완료";
            }
            catch (Exception ex)
            {
                _ = ShowErrorAsync(ex);
            }
        };

        return new StackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                InfoText(cacheRoot),
                InfoText("프로그램 종료 시 항상 캐시 내용은 자동으로 삭제됩니다.", foreground: Brush("#8a939b")),
                clearCache
            }
        };
    }

    private async Task ShowErrorAsync(Exception ex)
    {
        status.Text = "오류: " + ex.Message;
        var dialog = new Window
        {
            Title = "오류",
            Width = 460,
            Height = 220,
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = ex.Message, TextWrapping = TextWrapping.Wrap },
                    new Button
                    {
                        Content = "닫기",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        MinWidth = 100
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is Button close)
        {
            close.Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(this);
    }

    private static GridSplitter Splitter(GridResizeDirection direction)
    {
        return new GridSplitter
        {
            ResizeDirection = direction,
            Background = Brush("#d0d6dc"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
    }

    private static void AddToGrid(Grid grid, Control child, int column, int row)
    {
        Grid.SetColumn(child, column);
        Grid.SetRow(child, row);
        grid.Children.Add(child);
    }

    private static string PhaseItemLabel(Phase phase, int index, int count)
    {
        return count <= 1 ? phase.Label() : $"{phase.Label()}{index + 1}";
    }

    private static IBrush PhaseBackground(Phase phase)
    {
        return phase switch
        {
            Phase.Before => Brush("#fff8f4"),
            Phase.Processing => Brush("#906a5c"),
            Phase.After => Brush("#30373d"),
            _ => Brushes.White
        };
    }

    private static IBrush PhaseForeground(Phase phase)
    {
        return phase == Phase.Before ? Brush("#1f252a") : Brushes.White;
    }

    private static IBrush Brush(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }
}
