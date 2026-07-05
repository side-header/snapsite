using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NewGreen.Domain;
using NewGreen.Infrastructure.FileSystem;
using System.IO;

namespace NewGreen.UI;

public sealed partial class MainWindow
{
    private Control AssignedPhotoCard(PhotoGroup group, Phase phase, string relativePath, string label, int targetIndex, double width)
    {
        var hasPhoto = !string.IsNullOrWhiteSpace(relativePath);
        var imageSize = ScaledPhotoSize(112, classifiedPhotoScaleLevel);
        var cardWidth = ScaledPhotoSize(132, classifiedPhotoScaleLevel);
        var cardMinHeight = ScaledPhotoSize(146, classifiedPhotoScaleLevel);
        var slotMinHeight = ScaledPhotoSize(170, classifiedPhotoScaleLevel);
        var nameWidth = ScaledPhotoSize(116, classifiedPhotoScaleLevel);

        var cellContent = new Grid
        {
            Width = width,
            MinHeight = slotMinHeight
        };
        var slot = new StackPanel
        {
            Width = width,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                PhaseLabelEditor(group, phase, targetIndex, label)
            }
        };

        if (!hasPhoto)
        {
            slot.Children.Add(new TextBlock
            {
                Text = "×",
                Width = ScaledPhotoSize(124, classifiedPhotoScaleLevel),
                Height = ScaledPhotoSize(132, classifiedPhotoScaleLevel),
                FontSize = ScaledPhotoSize(56, classifiedPhotoScaleLevel),
                FontWeight = FontWeight.Thin,
                Foreground = Brush("#e2e8ee"),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });

            var emptySlot = new Border
            {
                Width = width,
                MinHeight = slotMinHeight,
                Padding = new Thickness(10, 0),
                Background = Brushes.Transparent,
                Child = cellContent
            };
            cellContent.Children.Add(slot);
            ConfigurePhotoSlotDropTarget(emptySlot, group, phase, targetIndex);
            return emptySlot;
        }

        var image = new Image
        {
            Width = imageSize,
            Height = imageSize,
            Stretch = Stretch.Uniform,
            Source = thumbnailService.TryGetCached(FileScanner.ToAbsolutePath(state.RootDir, relativePath))
        };
        _ = LoadThumbnailIntoAsync(image, relativePath);

        var name = EditableFileNameHost(relativePath, 11, Brush("#1f252a"), new Thickness(0), nameWidth);
        SetPhotoPathToolTip(name, relativePath);

        var cardContent = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                image,
                name
            }
        };

        var photoCard = new Border
        {
            Width = cardWidth,
            MinHeight = cardMinHeight,
            Margin = new Thickness(0),
            Padding = new Thickness(8),
            Background = Brushes.White,
            BorderBrush = Brush("#d8dde2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = cardContent
        };
        slot.Children.Add(photoCard);
        cellContent.Children.Add(slot);

        var card = new Border
        {
            Width = width,
            MinHeight = slotMinHeight,
            Margin = new Thickness(0),
            Padding = new Thickness(10, 0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            Child = cellContent
        };
        Point? previewStart = null;
        card.PointerPressed += (_, args) =>
        {
            if (args.Source is Button or TextBox)
            {
                return;
            }

            var point = args.GetCurrentPoint(card);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            previewStart = point.Position;
        };
        card.PointerReleased += async (_, args) =>
        {
            if (previewStart is null || args.Source is Button or TextBox)
            {
                previewStart = null;
                return;
            }

            var delta = args.GetPosition(card) - previewStart.Value;
            previewStart = null;
            if (Math.Abs(delta.X) >= 6 || Math.Abs(delta.Y) >= 6)
            {
                return;
            }

            selectedGroupId = group.Id;
            RefreshDetail();
            await ShowImagePreviewAsync(relativePath);
            args.Handled = true;
        };
        ConfigureDragSource(card, relativePath);
        ConfigurePhotoSlotDropTarget(card, group, phase, targetIndex);
        return card;
    }

    private TextBox PhaseLabelEditor(PhotoGroup group, Phase phase, int index, string label)
    {
        var input = new TextBox
        {
            Text = label,
            Watermark = "X",
            Width = 126,
            Height = 28,
            Padding = new Thickness(2, 0),
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = Brush("#1f252a"),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            SelectionBrush = Brush("#d8ecff"),
            CaretBrush = Brush("#1f252a"),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        input.Classes.Add("phase-label-box");
        input.Resources["TextControlBackground"] = Brushes.Transparent;
        input.Resources["TextControlBackgroundPointerOver"] = Brushes.Transparent;
        input.Resources["TextControlBackgroundFocused"] = Brushes.Transparent;
        input.Resources["TextControlBorderBrush"] = Brushes.Transparent;
        input.Resources["TextControlBorderBrushPointerOver"] = Brushes.Transparent;
        input.Resources["TextControlBorderBrushFocused"] = Brushes.Transparent;
        input.Resources["TextControlForeground"] = Brush("#1f252a");
        input.Resources["TextControlPlaceholderForeground"] = Brush("#b8c0c8");
        input.TextChanged += (_, _) => group.SetLabel(phase, index, input.Text ?? string.Empty);
        input.LostFocus += (_, _) => SaveToMetadata("사진 라벨을 저장했습니다", refreshAfterSave: false);
        input.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter)
            {
                return;
            }

            group.SetLabel(phase, index, input.Text ?? string.Empty);
            SaveToMetadata("사진 라벨을 저장했습니다", refreshAfterSave: false);
            args.Handled = true;
        };
        return input;
    }

    private Control PhotoCard(string relativePath, int? assignedGroupNumber)
    {
        var assigned = assignedGroupNumber.HasValue;
        var imageSize = ScaledPhotoSize(112, unclassifiedPhotoScaleLevel);
        var cardWidth = ScaledPhotoSize(132, unclassifiedPhotoScaleLevel);
        var cardMinHeight = ScaledPhotoSize(146, unclassifiedPhotoScaleLevel);
        var nameWidth = ScaledPhotoSize(116, unclassifiedPhotoScaleLevel);
        var image = new Image
        {
            Width = imageSize,
            Height = imageSize,
            Stretch = Stretch.Uniform,
            Source = thumbnailService.TryGetCached(FileScanner.ToAbsolutePath(state.RootDir, relativePath))
        };
        _ = LoadThumbnailIntoAsync(image, relativePath);

        var name = EditableFileNameHost(relativePath, 11, Brush("#1f252a"), new Thickness(0), nameWidth);
        SetPhotoPathToolTip(name, relativePath);

        var content = new StackPanel { Spacing = 4 };
        content.Children.Add(image);
        content.Children.Add(name);
        content.Opacity = assigned ? 0.32 : 1.0;

        var cardBody = new Grid();
        cardBody.Children.Add(content);
        if (assigned)
        {
            cardBody.Children.Add(new Border
            {
                MinWidth = 22,
                Height = 22,
                Padding = new Thickness(6, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                Child = new TextBlock
                {
                    Text = assignedGroupNumber!.Value.ToString(),
                    FontSize = 13,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brush("#111"),
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }

        var card = new Border
        {
            Width = cardWidth,
            MinHeight = cardMinHeight,
            Margin = new Thickness(5),
            Padding = new Thickness(8),
            Background = selectedPhoto == relativePath && !assigned ? Brush("#dff2e4") : assigned ? Brush("#f8f9fa") : Brushes.White,
            BorderBrush = selectedPhoto == relativePath && !assigned ? Brush("#35a04b") : assigned ? Brush("#c8d1da") : Brush("#d8dde2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = cardBody
        };
        Point? previewStart = null;
        card.PointerPressed += (_, args) =>
        {
            if (args.Source is Button or TextBox)
            {
                return;
            }

            var point = args.GetCurrentPoint(card);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            previewStart = point.Position;
        };
        card.PointerReleased += async (_, args) =>
        {
            if (previewStart is null || args.Source is Button or TextBox)
            {
                previewStart = null;
                return;
            }

            var delta = args.GetPosition(card) - previewStart.Value;
            previewStart = null;
            if (Math.Abs(delta.X) >= 6 || Math.Abs(delta.Y) >= 6)
            {
                return;
            }

            await ShowImagePreviewAsync(relativePath);
            args.Handled = true;
        };
        if (!assigned)
        {
            ConfigureDragSource(card, relativePath);
        }
        return card;
    }

    private void SetPhotoPathToolTip(Control control, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(state.RootDir) || string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        AttachPathTreePopup(control, state.RootDir, relativePath);
    }

    private void AttachPathTreePopup(Control control, string rootDir, string relativePath)
    {
        var popupContent = PathTreePopupContent(rootDir, relativePath);
        var popup = new Popup
        {
            PlacementTarget = control,
            Placement = PlacementMode.Bottom,
            VerticalOffset = 1,
            HorizontalOffset = 0,
            IsLightDismissEnabled = false,
            Child = popupContent
        };
        var pointerOverTarget = false;
        var pointerOverPopup = false;

        async void CloseWhenPointerLeaves()
        {
            await Task.Delay(160);
            if (!pointerOverTarget && !pointerOverPopup && ReferenceEquals(activePathTreePopup, popup))
            {
                CloseActivePathTreePopup();
            }
        }

        control.PointerEntered += (_, _) =>
        {
            pointerOverTarget = true;
            if (!ReferenceEquals(activePathTreePopup, popup))
            {
                CloseActivePathTreePopup();
                activePathTreePopup = popup;
            }
            popup.IsOpen = true;
        };
        control.PointerExited += (_, _) =>
        {
            pointerOverTarget = false;
            CloseWhenPointerLeaves();
        };
        popupContent.PointerEntered += (_, _) =>
        {
            pointerOverPopup = true;
        };
        popupContent.PointerExited += (_, _) =>
        {
            pointerOverPopup = false;
            CloseWhenPointerLeaves();
        };
        popup.Closed += (_, _) =>
        {
            if (ReferenceEquals(activePathTreePopup, popup))
            {
                activePathTreePopup = null;
            }
        };
    }

    private void CloseActivePathTreePopup()
    {
        if (activePathTreePopup is null)
        {
            return;
        }

        var popup = activePathTreePopup;
        activePathTreePopup = null;
        popup.IsOpen = false;
    }

    private static Control PathTreePopupContent(string rootDir, string relativePath)
    {
        var rootName = Path.GetFileName(rootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(rootName))
        {
            rootName = rootDir;
        }

        var segments = relativePath
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToList();
        var lines = new List<string> { rootName };
        for (var i = 0; i < segments.Count; i++)
        {
            lines.Add(PathTreeText(segments[i], i + 1, isLast: i == segments.Count - 1));
        }

        var treeText = new SelectableTextBlock
        {
            Text = string.Join(Environment.NewLine, lines),
            FontSize = 12,
            FontFamily = FontFamily.Parse("Menlo, Consolas, monospace"),
            Foreground = Brush("#1f252a"),
            TextWrapping = TextWrapping.NoWrap,
            SelectionBrush = Brush("#cfe8ff"),
            SelectionForegroundBrush = Brushes.Black
        };

        var scroll = new ScrollViewer
        {
            Content = treeText,
            MinWidth = 260,
            MaxWidth = 640,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        return new Border
        {
            Background = Brush("#f3f5f7"),
            BorderBrush = Brush("#d8dde2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(10, 8),
            Child = scroll
        };
    }

    private static string PathTreeText(string text, int depth, bool isLast)
    {
        return new string(' ', Math.Max(0, depth - 1) * 3) + (isLast ? "└─ " : "├─ ") + text;
    }

    private void ConfigureDragSource(Control control, string relativePath)
    {
        control.PointerPressed += (_, e) =>
        {
            if (e.Source is TextBox)
            {
                return;
            }

            var point = e.GetCurrentPoint(control);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            pendingDragPhoto = relativePath;
            dragStartPoint = point.Position;
        };

        control.PointerMoved += async (_, e) =>
        {
            if (dragStartPoint is null || string.IsNullOrWhiteSpace(pendingDragPhoto))
            {
                return;
            }

            var point = e.GetCurrentPoint(control);
            if (!point.Properties.IsLeftButtonPressed)
            {
                pendingDragPhoto = string.Empty;
                dragStartPoint = null;
                return;
            }

            var delta = point.Position - dragStartPoint.Value;
            if (Math.Abs(delta.X) < 6 && Math.Abs(delta.Y) < 6)
            {
                return;
            }

            var draggingPhoto = pendingDragPhoto;
            pendingDragPhoto = string.Empty;
            dragStartPoint = null;

            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(draggingPhoto));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        };

        control.PointerReleased += (_, _) =>
        {
            pendingDragPhoto = string.Empty;
            dragStartPoint = null;
        };
    }

    private void ConfigurePhotoSlotDropTarget(Control control, PhotoGroup targetGroup, Phase targetPhase, int targetIndex)
    {
        DragDrop.SetAllowDrop(control, true);
        DragDrop.AddDragOverHandler(control, (_, e) =>
        {
            if (!CanDropPhoto(e))
            {
                return;
            }

            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
        DragDrop.AddDropHandler(control, (_, e) =>
        {
            var relativePath = GetDroppedPhoto(e);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            AssignPhotoAt(targetGroup, targetPhase, relativePath, targetIndex);
            selectedPhoto = string.Empty;
            selectedGroupId = targetGroup.Id;
            status.Text = $"{targetGroup.Title} > {targetPhase.Label()} {targetIndex + 1} 위치로 이동했습니다: {relativePath}";
            SaveToMetadata("사진 위치를 저장했습니다", refreshAfterSave: false);
            RefreshAll();
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
    }

    private void ConfigureUnclassifiedDropTarget(Control control)
    {
        DragDrop.SetAllowDrop(control, true);
        DragDrop.AddDragOverHandler(control, (_, e) =>
        {
            if (!CanDropPhoto(e))
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
        DragDrop.AddDropHandler(control, (_, e) =>
        {
            var relativePath = GetDroppedPhoto(e);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            state.RemovePhoto(relativePath);
            selectedPhoto = string.Empty;
            pendingDragPhoto = string.Empty;
            dragStartPoint = null;
            status.Text = "분류에서 제거했습니다: " + relativePath;
            SaveToMetadata("분류에서 제거했습니다", refreshAfterSave: false);
            RefreshAll();
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
    }

    private void AssignPhotoAt(PhotoGroup targetGroup, Phase targetPhase, string relativePath, int targetIndex)
    {
        relativePath = AppState.NormalizePath(relativePath);
        var movedLabel = state.RemovePhotoWithLabel(relativePath);
        targetGroup.InsertPhoto(targetPhase, relativePath, targetIndex, movedLabel);
    }

    private void ConfigureGroupReorder(Control control, PhotoGroup group)
    {
        control.PointerPressed += (_, e) =>
        {
            if (e.Source is Button or ComboBox or Image)
            {
                return;
            }

            var point = e.GetCurrentPoint(control);
            if (!point.Properties.IsLeftButtonPressed || point.Position.Y > 54)
            {
                return;
            }

            pendingDragGroupId = group.Id;
            groupDragStartPoint = point.Position;
        };

        control.PointerMoved += async (_, e) =>
        {
            if (groupDragStartPoint is null || string.IsNullOrWhiteSpace(pendingDragGroupId))
            {
                return;
            }

            var point = e.GetCurrentPoint(control);
            if (!point.Properties.IsLeftButtonPressed)
            {
                pendingDragGroupId = string.Empty;
                groupDragStartPoint = null;
                return;
            }

            var delta = point.Position - groupDragStartPoint.Value;
            if (Math.Abs(delta.X) < 6 && Math.Abs(delta.Y) < 6)
            {
                return;
            }

            var draggingGroupId = pendingDragGroupId;
            pendingDragGroupId = string.Empty;
            groupDragStartPoint = null;

            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(GroupDragPrefix + draggingGroupId));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        };

        control.PointerReleased += (_, _) =>
        {
            pendingDragGroupId = string.Empty;
            groupDragStartPoint = null;
        };

        DragDrop.SetAllowDrop(control, true);
        DragDrop.AddDragOverHandler(control, (_, e) =>
        {
            var sourceGroupId = GetDroppedGroup(e);
            e.DragEffects = !string.IsNullOrWhiteSpace(sourceGroupId) && sourceGroupId != group.Id
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        });
        DragDrop.AddDropHandler(control, (_, e) =>
        {
            var sourceGroupId = GetDroppedGroup(e);
            if (string.IsNullOrWhiteSpace(sourceGroupId) || sourceGroupId == group.Id)
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            MoveGroupTo(sourceGroupId, group.Id);
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
    }

    private void ConfigureDropTarget(Control control, PhotoGroup group, Phase phase)
    {
        DragDrop.SetAllowDrop(control, true);
        DragDrop.AddDragOverHandler(control, (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(GetDroppedGroup(e)))
            {
                return;
            }

            e.DragEffects = CanDropPhoto(e) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        });
        DragDrop.AddDropHandler(control, (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(GetDroppedGroup(e)))
            {
                return;
            }

            var relativePath = GetDroppedPhoto(e);
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            state.AssignPhoto(group.Id, phase, relativePath);
            selectedPhoto = string.Empty;
            status.Text = $"{group.Title} > {phase.Label()}에 드롭했습니다: {relativePath}";
            SaveToMetadata("사진 위치를 저장했습니다", refreshAfterSave: false);
            RefreshAll();
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
    }

    private static bool CanDropPhoto(DragEventArgs e)
    {
        var text = e.DataTransfer.TryGetText();
        return !string.IsNullOrWhiteSpace(text) && !text.StartsWith(GroupDragPrefix, StringComparison.Ordinal);
    }

    private static string GetDroppedPhoto(DragEventArgs e)
    {
        var text = e.DataTransfer.TryGetText() ?? string.Empty;
        return text.StartsWith(GroupDragPrefix, StringComparison.Ordinal) ? string.Empty : text;
    }

    private static string GetDroppedGroup(DragEventArgs e)
    {
        var text = e.DataTransfer.TryGetText() ?? string.Empty;
        return text.StartsWith(GroupDragPrefix, StringComparison.Ordinal)
            ? text[GroupDragPrefix.Length..]
            : string.Empty;
    }

    private async Task LoadThumbnailIntoAsync(Image image, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(state.RootDir))
        {
            return;
        }

        var bitmap = await thumbnailService.LoadAsync(state.RootDir, relativePath);
        if (bitmap is null)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() => image.Source = bitmap);
    }

    private ContentControl EditableFileNameHost(string relativePath, double fontSize, IBrush foreground, Thickness margin, double width, TextAlignment textAlignment = TextAlignment.Center, bool showFullName = false)
    {
        var host = new ContentControl
        {
            Width = width,
            Margin = margin,
            HorizontalContentAlignment = textAlignment == TextAlignment.Left ? HorizontalAlignment.Left : HorizontalAlignment.Center
        };

        void ShowDisplay()
        {
            host.Content = FileNameDisplay(relativePath, fontSize, foreground, width, textAlignment, showFullName, () => ShowEditor());
        }

        void ShowEditor()
        {
            host.Content = FileNameEditor(relativePath, fontSize, foreground, width, showFullName, ShowDisplay);
        }

        ShowDisplay();
        return host;
    }

    private static TextBlock FileNameDisplay(string relativePath, double fontSize, IBrush foreground, double width, TextAlignment textAlignment, bool showFullName, Action beginEdit)
    {
        var block = new TextBlock
        {
            Text = EditableName(relativePath, showFullName),
            Width = width,
            MaxHeight = 38,
            FontSize = fontSize,
            Foreground = foreground,
            TextAlignment = textAlignment,
            TextWrapping = TextWrapping.Wrap
        };
        block.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(block).Properties.IsLeftButtonPressed)
            {
                return;
            }

            beginEdit();
            args.Handled = true;
        };
        return block;
    }

    private TextBox FileNameEditor(string relativePath, double fontSize, IBrush foreground, double width, bool showFullName, Action cancelEdit)
    {
        var box = new TextBox
        {
            Text = EditableName(relativePath, showFullName),
            Width = width,
            Height = 24,
            Padding = new Thickness(2, 0),
            FontSize = fontSize,
            Foreground = foreground,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            SelectionBrush = Brushes.Transparent,
            SelectionForegroundBrush = foreground,
            CaretBrush = foreground,
            TextAlignment = TextAlignment.Left,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        box.Classes.Add("file-name-box");
        box.Resources["TextControlBackground"] = Brushes.Transparent;
        box.Resources["TextControlBackgroundPointerOver"] = Brushes.Transparent;
        box.Resources["TextControlBackgroundFocused"] = Brushes.Transparent;
        box.Resources["TextControlBorderBrush"] = Brushes.Transparent;
        box.Resources["TextControlBorderBrushPointerOver"] = Brushes.Transparent;
        box.Resources["TextControlBorderBrushFocused"] = Brushes.Transparent;
        box.Resources["TextControlForeground"] = foreground;
        box.Resources["TextControlForegroundFocused"] = foreground;
        box.Resources["TextControlForegroundPointerOver"] = foreground;
        box.AttachedToVisualTree += (_, _) =>
        {
            box.Focus();
            box.CaretIndex = box.Text?.Length ?? 0;
        };
        box.LostFocus += (_, _) => cancelEdit();
        box.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter)
            {
                return;
            }

            args.Handled = true;
            RenamePathFromTextBox(relativePath, box, showFullName);
        };
        return box;
    }

    private void RenamePathFromTextBox(string relativePath, TextBox box, bool allowDotInName)
    {
        var newName = box.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(newName))
        {
            box.Text = EditableName(relativePath, allowDotInName);
            status.Text = "파일 이름을 입력하세요.";
            return;
        }

        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            newName.Contains('/') ||
            newName.Contains('\\') ||
            (!allowDotInName && newName.Contains('.')))
        {
            box.Text = EditableName(relativePath, allowDotInName);
            status.Text = allowDotInName
                ? "이름에는 사용할 수 없는 문자를 입력할 수 없습니다."
                : "파일 이름에는 확장자나 사용할 수 없는 문자를 입력할 수 없습니다.";
            return;
        }

        RenamePath(relativePath, newName);
    }

    private static string EditableName(string relativePath, bool showFullName = false)
    {
        var fileName = Path.GetFileName(relativePath);
        if (showFullName)
        {
            return fileName;
        }

        return string.IsNullOrEmpty(Path.GetExtension(fileName))
            ? fileName
            : Path.GetFileNameWithoutExtension(fileName);
    }

    private void RenamePath(string relativePath, string newName)
    {
        if (string.IsNullOrWhiteSpace(state.RootDir))
        {
            status.Text = "먼저 기준 폴더를 선택하세요.";
            return;
        }

        try
        {
            relativePath = AppState.NormalizePath(relativePath);
            var absolutePath = FileScanner.ToAbsolutePath(state.RootDir, relativePath);
            var isDirectory = Directory.Exists(absolutePath);
            if (!isDirectory && !File.Exists(absolutePath))
            {
                status.Text = "파일을 찾을 수 없습니다: " + relativePath;
                RefreshAll();
                return;
            }

            var parentRelativePath = AppState.NormalizePath(Path.GetDirectoryName(relativePath) ?? string.Empty);
            if (parentRelativePath == ".")
            {
                parentRelativePath = string.Empty;
            }

            if (!isDirectory)
            {
                newName += Path.GetExtension(relativePath);
            }

            var newRelativePath = string.IsNullOrEmpty(parentRelativePath)
                ? newName
                : parentRelativePath + "/" + newName;
            newRelativePath = AppState.NormalizePath(newRelativePath);
            if (string.Equals(relativePath, newRelativePath, StringComparison.Ordinal))
            {
                return;
            }

            var targetPath = FileScanner.ToAbsolutePath(state.RootDir, newRelativePath);
            if (File.Exists(targetPath) || Directory.Exists(targetPath))
            {
                status.Text = "같은 이름의 파일 또는 폴더가 이미 있습니다: " + newRelativePath;
                RefreshAll();
                return;
            }

            if (isDirectory)
            {
                Directory.Move(absolutePath, targetPath);
            }
            else
            {
                File.Move(absolutePath, targetPath);
            }

            state.ReplaceAssignedPath(relativePath, newRelativePath, isDirectory);
            selectedPhoto = ReplaceRelativePath(selectedPhoto, relativePath, newRelativePath, isDirectory);
            pendingDragPhoto = ReplaceRelativePath(pendingDragPhoto, relativePath, newRelativePath, isDirectory);
            ReplaceExpandedExplorerPaths(relativePath, newRelativePath, isDirectory);

            metadataStore.Save(state.RootDir, state);
            scanResult = scanner.Scan(state.RootDir);
            RefreshAll();
            status.Text = "이름을 변경했습니다: " + newRelativePath;
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync(ex);
            RefreshAll();
        }
    }

    private static string ReplaceRelativePath(string currentPath, string oldPath, string newPath, bool includeChildren)
    {
        if (string.IsNullOrWhiteSpace(currentPath))
        {
            return currentPath;
        }

        currentPath = AppState.NormalizePath(currentPath);
        oldPath = AppState.NormalizePath(oldPath).TrimEnd('/');
        newPath = AppState.NormalizePath(newPath).TrimEnd('/');
        if (string.Equals(currentPath, oldPath, StringComparison.OrdinalIgnoreCase))
        {
            return newPath;
        }

        var oldPrefix = oldPath + "/";
        return includeChildren && currentPath.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase)
            ? newPath + currentPath[oldPath.Length..]
            : currentPath;
    }

    private void ReplaceExpandedExplorerPaths(string oldPath, string newPath, bool includeChildren)
    {
        oldPath = AppState.NormalizePath(oldPath).TrimEnd('/');
        newPath = AppState.NormalizePath(newPath).TrimEnd('/');
        ReplaceExpandedPaths(expandedExplorerPaths, oldPath, newPath, includeChildren);
        ReplaceExpandedPaths(expandedUnclassifiedPaths, oldPath, newPath, includeChildren);
    }

    private static void ReplaceExpandedPaths(HashSet<string> paths, string oldPath, string newPath, bool includeChildren)
    {
        var updated = paths
            .Select(path => ReplaceRelativePath(path, oldPath, newPath, includeChildren))
            .ToList();
        paths.Clear();
        foreach (var path in updated)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                paths.Add(path);
            }
        }
    }
}
