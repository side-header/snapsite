using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using NewGreen.Domain;
using NewGreen.Infrastructure.FileSystem;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Avalonia.VisualTree;

namespace NewGreen.UI;

public sealed partial class MainWindow
{
    private const string UnclassifiedPhotoCardClass = "unclassified-photo-card";
    private static readonly Regex Rule1PhotoNamePattern = new(
        "^(?<number>[0-9]+)(?<phase>전|중|후)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Control AssignedPhotoCard(
        PhotoGroup group,
        bool omit,
        PhotoCell cell,
        int cellIndex,
        double width,
        Border insertionIndicator,
        double collectionWidth)
    {
        var relativePath = cell.Image;
        var hasPhoto = !string.IsNullOrWhiteSpace(relativePath);
        const double fileNameHeight = 24;
        const double cardContentSpacing = 4;
        const double cardPadding = 8;
        const double cardBorderThickness = 1;
        var imageSize = ScaledPhotoSize(112, classifiedPhotoScaleLevel);
        var cardWidth = ScaledPhotoSize(132, classifiedPhotoScaleLevel);
        var cardHeight = Math.Max(
            ScaledPhotoSize(146, classifiedPhotoScaleLevel),
            imageSize + fileNameHeight + cardContentSpacing + (cardPadding + cardBorderThickness) * 2);
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
                HoverOutline(
                    CellLabelEditor(group, cell, omit),
                    HorizontalAlignment.Center,
                    VerticalAlignment.Center,
                    new Thickness(0),
                    new Thickness(0))
            }
        };

        if (!hasPhoto)
        {
            var emptyCard = new Border
            {
                Width = cardWidth,
                Height = cardHeight,
                MinHeight = cardHeight,
                Margin = new Thickness(0),
                Padding = new Thickness(cardPadding),
                Background = Brushes.White,
                BorderBrush = Brush("#d8dde2"),
                BorderThickness = new Thickness(cardBorderThickness),
                CornerRadius = new CornerRadius(6),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            slot.Children.Add(ClassifiedCardWithControls(emptyCard, group, omit, cellIndex, cardWidth, cardHeight));

            var emptySlot = new Border
            {
                Width = width,
                MinHeight = slotMinHeight,
                Padding = new Thickness(10, 0),
                Background = Brushes.Transparent,
                Child = cellContent
            };
            cellContent.Children.Add(slot);
            ConfigureClassifiedCellDragSource(emptySlot, group, omit, cellIndex, cell.Image);
            ConfigurePhotoSlotDropTarget(
                emptySlot,
                group,
                omit,
                cellIndex,
                width,
                collectionWidth,
                insertionIndicator);
            return emptySlot;
        }

        var image = new Image
        {
            Width = imageSize,
            Height = imageSize,
            Stretch = Stretch.Uniform,
            Source = thumbnailService.TryGetCached(FileScanner.ToAbsolutePath(state.RootDir, relativePath))
        };
        ConfigureImagePreviewTrigger(image, relativePath, _ =>
        {
            SelectGroupForPreview(group.Id);
            ShowPhotoPathInStatus(relativePath);
            RevealPhotosInUnclassifiedArea([relativePath]);
            return false;
        });
        _ = LoadThumbnailIntoAsync(image, relativePath);

        var name = PermanentFileNameTextBox(
            relativePath,
            11,
            Brush("#1f252a"),
            nameWidth,
            onBeginEdit: () =>
            {
                SelectGroupForPreview(group.Id);
            });

        var cardContent = new StackPanel
        {
            Spacing = cardContentSpacing,
            Children =
            {
                image,
                name
            }
        };

        var photoCard = new Border
        {
            Width = cardWidth,
            Height = cardHeight,
            MinHeight = cardHeight,
            Margin = new Thickness(0),
            Padding = new Thickness(cardPadding),
            Background = Brushes.White,
            BorderBrush = Brush("#d8dde2"),
            BorderThickness = new Thickness(cardBorderThickness),
            CornerRadius = new CornerRadius(6),
            HorizontalAlignment = HorizontalAlignment.Center,
            Child = cardContent
        };
        classifiedPhotoCardViews.TryAdd(AppState.NormalizePath(relativePath), photoCard);
        Point? navigationStart = null;
        photoCard.PointerPressed += (_, args) =>
        {
            var point = args.GetCurrentPoint(photoCard);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            navigationStart = point.Position;
        };
        photoCard.PointerReleased += (_, args) =>
        {
            if (navigationStart is null)
            {
                return;
            }

            if (args.Source is Image)
            {
                navigationStart = null;
                return;
            }

            var delta = args.GetPosition(photoCard) - navigationStart.Value;
            navigationStart = null;
            if (Math.Abs(delta.X) >= 6 || Math.Abs(delta.Y) >= 6)
            {
                return;
            }

            SelectGroupForPreview(group.Id);
            ShowPhotoPathInStatus(relativePath);
            RevealPhotosInUnclassifiedArea([relativePath]);
        };
        slot.Children.Add(ClassifiedCardWithControls(photoCard, group, omit, cellIndex, cardWidth, cardHeight));
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
        ConfigureClassifiedCellDragSource(card, group, omit, cellIndex, relativePath);
        ConfigurePhotoSlotDropTarget(
            card,
            group,
            omit,
            cellIndex,
            width,
            collectionWidth,
            insertionIndicator);
        return card;
    }

    private static Border ClassifiedCellInsertionIndicator()
    {
        return new Border
        {
            Width = 3,
            Margin = new Thickness(0),
            Background = Brush("#2f80ed"),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false,
            IsVisible = false
        };
    }

    private static void ShowClassifiedCellInsertionIndicator(
        Border indicator,
        double boundaryX,
        double collectionWidth)
    {
        var lineWidth = indicator.Width;
        var maximumLeft = Math.Max(0, collectionWidth - lineWidth);
        var left = Math.Clamp(boundaryX - lineWidth / 2, 0, maximumLeft);
        indicator.HorizontalAlignment = HorizontalAlignment.Left;
        indicator.Margin = new Thickness(left, 0, 0, 0);
        indicator.IsVisible = true;
    }

    private void ShowPhotoPathInStatus(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(state.RootDir) || string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        status.Text = FileScanner.ToAbsolutePath(state.RootDir, relativePath);
    }

    private Control ClassifiedCardWithControls(
        Border cardSurface,
        PhotoGroup group,
        bool omit,
        int cellIndex,
        double cardWidth,
        double cardHeight)
    {
        var remove = CellControlButton("−", "분류 해제 / 셀 삭제");
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            Children =
            {
                remove
            }
        };
        if (!omit)
        {
            var insertLeft = CellControlButton("←", "왼쪽에 빈 셀 추가");
            var insertRight = CellControlButton("→", "오른쪽에 빈 셀 추가");
            insertLeft.Click += (_, _) => InsertClassifiedTargetCell(group, cellIndex);
            insertRight.Click += (_, _) => InsertClassifiedTargetCell(group, cellIndex + 1);
            buttons.Children.Add(insertLeft);
            buttons.Children.Add(insertRight);
        }

        var overlay = new Border
        {
            IsVisible = false,
            Padding = new Thickness(2),
            Margin = new Thickness(0, 5, 0, 0),
            Background = Brush("#ffffff"),
            BorderBrush = Brush("#c8d1da"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(5),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            Child = buttons
        };
        var root = new Grid
        {
            Width = cardWidth,
            Height = cardHeight,
            MinHeight = cardHeight,
            Children =
            {
                cardSurface,
                overlay
            }
        };
        root.PropertyChanged += (_, args) =>
        {
            if (args.Property == InputElement.IsPointerOverProperty)
            {
                overlay.IsVisible = root.IsPointerOver;
                cardSurface.BorderBrush = root.IsPointerOver ? Brush("#2f80ed") : Brush("#d8dde2");
            }
        };
        remove.Click += (_, _) => RemoveClassifiedCell(group, omit, cellIndex);
        return root;
    }

    private Button CellControlButton(string text, string tooltip)
    {
        var size = Math.Max(18, ScaledPhotoSize(25, classifiedPhotoScaleLevel));
        var button = new Button
        {
            Content = text,
            Width = size,
            Height = size,
            Padding = new Thickness(0),
            FontSize = Math.Max(11, ScaledPhotoSize(14, classifiedPhotoScaleLevel)),
            FontWeight = FontWeight.Bold,
            Foreground = Brush("#1f252a"),
            Background = Brush("#f8f9fa"),
            BorderBrush = Brush("#d8dde2"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Focusable = false
        };
        button.Resources["ButtonBackground"] = Brush("#f8f9fa");
        button.Resources["ButtonBackgroundPointerOver"] = Brush("#f7f8f9");
        button.Resources["ButtonBackgroundPressed"] = Brush("#dfe4e8");
        button.Resources["ButtonForeground"] = Brush("#1f252a");
        button.Resources["ButtonForegroundPointerOver"] = Brush("#1f252a");
        button.Resources["ButtonForegroundPressed"] = Brush("#1f252a");
        button.Resources["ButtonBorderBrush"] = Brush("#d8dde2");
        button.Resources["ButtonBorderBrushPointerOver"] = Brush("#d8dde2");
        button.Resources["ButtonBorderBrushPressed"] = Brush("#d8dde2");
        ToolTip.SetTip(button, tooltip);
        button.PointerPressed += (_, args) => args.Handled = true;
        return button;
    }

    private void RemoveClassifiedCell(PhotoGroup group, bool omit, int cellIndex)
    {
        var refreshSnapshot = CaptureClassifiedGroupRefreshSnapshot([group.Id]);
        var savedUnclassifiedOffset = unclassifiedTreeScrollViewer?.Offset;
        if (!state.RemoveCell(group.Id, omit, cellIndex, out var removedPhoto))
        {
            return;
        }

        status.Text = string.IsNullOrWhiteSpace(removedPhoto)
            ? $"{group.Title}의 빈 셀을 삭제했습니다."
            : $"분류에서 제거했습니다: {removedPhoto}";
        RefreshPanelHeaders();
        RefreshClassifiedGroups(refreshSnapshot);
        if (selectedGroupId == group.Id)
        {
            RefreshDetail();
        }

        if (!string.IsNullOrWhiteSpace(removedPhoto))
        {
            if (ReplaceRenderedAssignedPhotoCardWithUnclassified(removedPhoto))
            {
                UpdateUnclassifiedPhotoSelectionVisuals();
            }
            else
            {
                RefreshCenter();
                RestoreUnclassifiedScrollOffset(savedUnclassifiedOffset);
            }
        }
        QueueAutoSave("셀을 삭제했습니다");
    }

    private void InsertClassifiedTargetCell(PhotoGroup group, int insertIndex)
    {
        var refreshSnapshot = CaptureClassifiedGroupRefreshSnapshot([group.Id]);
        if (!state.InsertEmptyCell(group.Id, omit: false, insertIndex))
        {
            return;
        }

        status.Text = $"{group.Title}에 빈 셀을 추가했습니다.";
        RefreshClassifiedGroups(refreshSnapshot);
        if (selectedGroupId == group.Id)
        {
            RefreshDetail();
        }
        QueueAutoSave("빈 셀을 추가했습니다");
    }

    private TextBox CellLabelEditor(PhotoGroup group, PhotoCell cell, bool omit)
    {
        var input = new TextBox
        {
            Text = cell.Label,
            Watermark = string.Empty,
            Width = 126,
            Height = 28,
            Padding = new Thickness(2, 0),
            FontSize = 14,
            LineHeight = 20,
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
        input.GotFocus += (_, _) => SelectGroupForPreview(group.Id);
        input.PointerPressed += (_, args) =>
        {
            if (args.GetCurrentPoint(input).Properties.IsLeftButtonPressed)
            {
                SelectGroupForPreview(group.Id);
            }
        };
        input.LostFocus += (_, _) =>
        {
            input.Text = cell.Label;
        };
        input.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter)
            {
                return;
            }

            var nextLabel = input.Text ?? string.Empty;
            var changed = !string.Equals(cell.Label, nextLabel, StringComparison.Ordinal);
            cell.Label = nextLabel;
            if (changed)
            {
                QueueAutoSave("사진 라벨을 저장했습니다");
            }
            args.Handled = true;
        };
        return input;
    }

    private TextBox PermanentFileNameTextBox(
        string relativePath,
        double fontSize,
        IBrush foreground,
        double width,
        bool isReadOnly = false,
        Action? onBeginEdit = null,
        Func<KeyModifiers, bool>? onPointerPressed = null)
    {
        var input = FileNameEditor(
            relativePath,
            fontSize,
            foreground,
            width,
            TextAlignment.Center,
            showFullName: false,
            singleLineLayout: true,
            cancelEdit: box => box.Text = EditableName(relativePath, showFullName: false),
            focusOnAttach: false);
        input.IsReadOnly = isReadOnly;
        input.BorderThickness = new Thickness(1);
        input.CornerRadius = new CornerRadius(4);
        input.BorderBrush = Brushes.Transparent;
        input.Resources["TextControlBorderBrushPointerOver"] = Brush("#2f80ed");
        input.PointerEntered += (_, _) => input.BorderBrush = Brush("#2f80ed");
        input.PointerExited += (_, _) => input.BorderBrush = Brushes.Transparent;
        if (onBeginEdit is not null)
        {
            input.GotFocus += (_, _) => onBeginEdit();
        }

        if (onPointerPressed is not null)
        {
            input.AddHandler(
                InputElement.PointerPressedEvent,
                (_, args) =>
                {
                    if (args.GetCurrentPoint(input).Properties.IsLeftButtonPressed && HasSelectionModifier(args.KeyModifiers))
                    {
                        onPointerPressed(args.KeyModifiers);
                        args.Handled = true;
                    }
                },
                RoutingStrategies.Tunnel);
            input.AddHandler(
                InputElement.PointerPressedEvent,
                (_, args) =>
                {
                    if (args.GetCurrentPoint(input).Properties.IsLeftButtonPressed && !HasSelectionModifier(args.KeyModifiers))
                    {
                        onPointerPressed(args.KeyModifiers);
                        args.Handled = true;
                    }
                },
                RoutingStrategies.Bubble,
                handledEventsToo: true);
        }
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
        ConfigureImagePreviewTrigger(image, relativePath, modifiers =>
        {
            if (!assigned)
            {
                SelectUnclassifiedPhoto(relativePath, modifiers);
                return HasSelectionModifier(modifiers);
            }

            return false;
        });
        _ = LoadThumbnailIntoAsync(image, relativePath);

        var name = PermanentFileNameTextBox(
            relativePath,
            11,
            assigned ? Brush("#b7b9ba") : Brush("#1f252a"),
            nameWidth,
            isReadOnly: assigned,
            onPointerPressed: assigned
                ? null
                : modifiers =>
                {
                    SelectUnclassifiedPhoto(relativePath, modifiers);
                    return HasSelectionModifier(modifiers);
                });
        var content = new StackPanel { Spacing = 4 };
        image.Opacity = assigned ? 0.32 : 1.0;
        content.Children.Add(image);
        content.Children.Add(name);

        var cardBody = new Grid();
        cardBody.Children.Add(content);
        if (!assigned)
        {
            visibleUnclassifiedPhotos.Add(relativePath);
        }
        var selectedIndex = assigned ? -1 : SelectedPhotoIndex(relativePath);
        var selectionBadgeText = new TextBlock
        {
            FontSize = 12,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        var selectionBadge = new Border
        {
            MinWidth = 22,
            Height = 22,
            Padding = new Thickness(6, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brush("#2f80ed"),
            CornerRadius = new CornerRadius(11),
            IsVisible = !assigned && !isRule1Selection && selectedPhotos.Count >= 2 && selectedIndex >= 0,
            Child = selectionBadgeText
        };
        if (selectionBadge.IsVisible)
        {
            selectionBadgeText.Text = (selectedIndex + 1).ToString();
        }
        if (!assigned)
        {
            cardBody.Children.Add(selectionBadge);
        }
        if (assigned)
        {
            var assignedGroupNumberText = new TextBlock
            {
                Text = assignedGroupNumber!.Value.ToString(),
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                Foreground = Brush("#111"),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            unclassifiedAssignedGroupNumberTexts[relativePath] = assignedGroupNumberText;
            cardBody.Children.Add(new Border
            {
                MinWidth = 22,
                Height = 22,
                Padding = new Thickness(6, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                Child = assignedGroupNumberText
            });
        }

        var isSelected = selectedIndex >= 0;
        IBrush RestingBorderBrush()
        {
            if (assigned)
            {
                return Brush("#c8d1da");
            }

            return SelectedPhotoIndex(relativePath) >= 0
                ? Brush("#2f80ed")
                : Brush("#d8dde2");
        }

        var card = new Border
        {
            Width = cardWidth,
            MinHeight = cardMinHeight,
            Margin = new Thickness(5),
            Padding = new Thickness(8),
            Background = isSelected
                ? Brush("#eaf4ff")
                : assigned ? Brush("#f8f9fa") : Brushes.White,
            BorderBrush = RestingBorderBrush(),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = cardBody
        };
        if (assigned)
        {
            ConfigurePhotoPathStatusClick(card, relativePath);
        }
        renderedUnclassifiedPhotoCards.Add((relativePath, card));
        card.PointerEntered += (_, _) => card.BorderBrush = Brush("#2f80ed");
        card.PointerExited += (_, _) => card.BorderBrush = RestingBorderBrush();
        card.Classes.Add(UnclassifiedPhotoCardClass);
        if (!assigned)
        {
            unclassifiedPhotoCardViews[relativePath] = (card, selectionBadge, selectionBadgeText);

            Point? selectionStart = null;
            var selectionModifiers = KeyModifiers.None;
            card.PointerPressed += (_, args) =>
            {
                var point = args.GetCurrentPoint(card);
                if (point.Properties.IsLeftButtonPressed)
                {
                    selectionStart = point.Position;
                    selectionModifiers = args.KeyModifiers;
                }
            };
            card.PointerReleased += (_, args) =>
            {
                if (selectionStart is null)
                {
                    return;
                }

                var delta = args.GetPosition(card) - selectionStart.Value;
                selectionStart = null;
                if (Math.Abs(delta.X) < 6 && Math.Abs(delta.Y) < 6)
                {
                    SelectUnclassifiedPhoto(relativePath, selectionModifiers);
                }
            };
            ConfigureDragSource(card, relativePath);
        }
        return card;
    }

    private void ConfigurePhotoPathStatusClick(Control control, string relativePath)
    {
        Point? clickStart = null;
        control.AddHandler(
            InputElement.PointerPressedEvent,
            (_, args) =>
            {
                var point = args.GetCurrentPoint(control);
                if (point.Properties.IsLeftButtonPressed)
                {
                    clickStart = point.Position;
                }
            },
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        control.AddHandler(
            InputElement.PointerReleasedEvent,
            (_, args) =>
            {
                if (clickStart is null)
                {
                    return;
                }

                var delta = args.GetPosition(control) - clickStart.Value;
                clickStart = null;
                if (Math.Abs(delta.X) < 6 && Math.Abs(delta.Y) < 6)
                {
                    ShowPhotoPathInStatus(relativePath);
                    RevealPhotoInClassifiedArea(relativePath);
                }
            },
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private void RevealPhotoInClassifiedArea(string relativePath)
    {
        var normalizedPath = AppState.NormalizePath(relativePath);
        if (!classifiedPhotoCardViews.TryGetValue(normalizedPath, out var targetCard))
        {
            return;
        }

        ScrollClassifiedPhotoIntoPreferredPosition(targetCard);
        HighlightClassifiedPhotoCard(targetCard);
    }

    private void ScrollClassifiedPhotoIntoPreferredPosition(Border targetCard)
    {
        var horizontalScroller = targetCard
            .GetVisualAncestors()
            .OfType<ScrollViewer>()
            .FirstOrDefault(scrollViewer => !ReferenceEquals(scrollViewer, classifiedScrollViewer));
        if (horizontalScroller?.Content is Control horizontalContent && horizontalScroller.Viewport.Width > 0)
        {
            var targetCenter = targetCard.TranslatePoint(
                new Point(targetCard.Bounds.Width / 2, 0),
                horizontalContent);
            if (targetCenter is not null)
            {
                var maximumOffset = Math.Max(0, horizontalScroller.Extent.Width - horizontalScroller.Viewport.Width);
                var targetOffset = Math.Clamp(
                    targetCenter.Value.X - horizontalScroller.Viewport.Width / 2,
                    0,
                    maximumOffset);
                horizontalScroller.Offset = new Vector(targetOffset, horizontalScroller.Offset.Y);
            }
        }
        else
        {
            targetCard.BringIntoView();
        }

        if (classifiedScrollViewer is not { } outerScroller || outerScroller.Viewport.Height <= 0)
        {
            targetCard.BringIntoView();
            return;
        }

        var verticalCenter = targetCard.TranslatePoint(
            new Point(0, targetCard.Bounds.Height / 2),
            rightPanel);
        if (verticalCenter is null)
        {
            targetCard.BringIntoView();
            return;
        }

        var maximumVerticalOffset = Math.Max(0, outerScroller.Extent.Height - outerScroller.Viewport.Height);
        var verticalOffset = Math.Clamp(
            verticalCenter.Value.Y - outerScroller.Viewport.Height * 0.4,
            0,
            maximumVerticalOffset);
        outerScroller.Offset = new Vector(outerScroller.Offset.X, verticalOffset);
    }

    private void HighlightClassifiedPhotoCard(Border card)
    {
        var version = ++classifiedPhotoHighlightVersion;
        classifiedPhotoHighlightVersions[card] = version;
        card.Transitions = null;
        card.Background = Brush("#eaf4ff");
        card.BorderBrush = Brush("#2f80ed");
        card.Transitions = new Transitions
        {
            new BrushTransition
            {
                Property = Border.BackgroundProperty,
                Duration = TimeSpan.FromMilliseconds(620)
            },
            new BrushTransition
            {
                Property = Border.BorderBrushProperty,
                Duration = TimeSpan.FromMilliseconds(620)
            }
        };

        DispatcherTimer.RunOnce(() =>
        {
            if (!IsCurrentClassifiedPhotoHighlight(card, version))
            {
                return;
            }

            card.Background = Brushes.White;
            if (!card.IsPointerOver)
            {
                card.BorderBrush = Brush("#d8dde2");
            }
        }, TimeSpan.FromMilliseconds(60), DispatcherPriority.Render);

        DispatcherTimer.RunOnce(() =>
        {
            if (!IsCurrentClassifiedPhotoHighlight(card, version))
            {
                return;
            }

            classifiedPhotoHighlightVersions.Remove(card);
            card.Transitions = null;
            card.Background = Brushes.White;
            card.BorderBrush = card.IsPointerOver ? Brush("#2f80ed") : Brush("#d8dde2");
        }, TimeSpan.FromMilliseconds(720), DispatcherPriority.Render);
    }

    private bool IsCurrentClassifiedPhotoHighlight(Border card, int version)
    {
        return classifiedPhotoHighlightVersions.TryGetValue(card, out var currentVersion) &&
            currentVersion == version;
    }

    private static bool IsShiftPressed(KeyModifiers modifiers)
    {
        return (modifiers & KeyModifiers.Shift) != 0;
    }

    private static bool IsToggleSelectionPressed(KeyModifiers modifiers)
    {
        return (modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0;
    }

    private static bool HasSelectionModifier(KeyModifiers modifiers)
    {
        return IsShiftPressed(modifiers) || IsToggleSelectionPressed(modifiers);
    }

    private int SelectedPhotoIndex(string relativePath)
    {
        return selectedPhotos.FindIndex(path => AppState.SamePath(path, relativePath));
    }

    private void SelectUnclassifiedPhoto(string relativePath, KeyModifiers modifiers)
    {
        ShowPhotoPathInStatus(relativePath);

        if (isRule1Selection)
        {
            selectedPhotos.Clear();
            unclassifiedSelectionAnchor = string.Empty;
            isRule1Selection = false;
        }

        var selectRange = IsShiftPressed(modifiers);
        var toggleSelection = IsToggleSelectionPressed(modifiers);
        var result = ApplyUnclassifiedPhotoSelection(
            selectedPhotos,
            visibleUnclassifiedPhotos,
            unclassifiedSelectionAnchor,
            relativePath,
            selectRange,
            toggleSelection);
        selectedPhotos.Clear();
        selectedPhotos.AddRange(result.Selected);
        unclassifiedSelectionAnchor = result.Anchor;
        UpdateUnclassifiedPhotoSelectionVisuals();
    }

    private static (List<string> Selected, string Anchor) ApplyUnclassifiedPhotoSelection(
        IReadOnlyList<string> currentSelection,
        IReadOnlyList<string> visiblePhotos,
        string anchorPath,
        string relativePath,
        bool selectRange,
        bool toggleSelection)
    {
        var nextSelection = currentSelection.ToList();
        if (selectRange)
        {
            var range = VisibleUnclassifiedPhotoRange(visiblePhotos, anchorPath, relativePath);
            if (range.Count == 0)
            {
                return ([relativePath], relativePath);
            }

            if (!toggleSelection)
            {
                nextSelection.Clear();
            }

            foreach (var path in range)
            {
                if (!nextSelection.Any(selected => AppState.SamePath(selected, path)))
                {
                    nextSelection.Add(path);
                }
            }

            return (nextSelection, anchorPath);
        }

        if (toggleSelection)
        {
            var selectedIndex = nextSelection.FindIndex(selected => AppState.SamePath(selected, relativePath));
            if (selectedIndex >= 0)
            {
                nextSelection.RemoveAt(selectedIndex);
            }
            else
            {
                nextSelection.Add(relativePath);
            }

            return (nextSelection, relativePath);
        }

        if (currentSelection.Count == 1 && AppState.SamePath(currentSelection[0], relativePath))
        {
            return ([], string.Empty);
        }

        return ([relativePath], relativePath);
    }

    private void ConfigureUnclassifiedSelectionDismissTarget(Control control)
    {
        control.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(control).Properties.IsLeftButtonPressed ||
                IsUnclassifiedSelectionDismissExcluded(args.Source))
            {
                return;
            }

            ClearUnclassifiedPhotoSelection();
            UpdateUnclassifiedPhotoSelectionVisuals();
        };
    }

    private static bool IsUnclassifiedSelectionDismissExcluded(object? source)
    {
        var visual = source as Visual;
        while (visual is not null)
        {
            if (visual is Control control &&
                (control.Classes.Contains(UnclassifiedPhotoCardClass) || control is Button or ScrollBar))
            {
                return true;
            }

            visual = visual.GetVisualParent();
        }

        return false;
    }

    private static List<string> VisibleUnclassifiedPhotoRange(
        IReadOnlyList<string> visiblePhotos,
        string anchorPath,
        string targetPath)
    {
        var anchorIndex = FindPhotoIndex(visiblePhotos, anchorPath);
        var targetIndex = FindPhotoIndex(visiblePhotos, targetPath);
        if (anchorIndex < 0 || targetIndex < 0)
        {
            return [];
        }

        var step = anchorIndex <= targetIndex ? 1 : -1;
        var range = new List<string>();
        for (var index = anchorIndex; ; index += step)
        {
            range.Add(visiblePhotos[index]);
            if (index == targetIndex)
            {
                break;
            }
        }

        return range;
    }

    private static int FindPhotoIndex(IReadOnlyList<string> photos, string relativePath)
    {
        for (var index = 0; index < photos.Count; index++)
        {
            if (AppState.SamePath(photos[index], relativePath))
            {
                return index;
            }
        }

        return -1;
    }

    private void ReconcileVisibleUnclassifiedSelection()
    {
        if (isRule1Selection)
        {
            var assigned = state.AssignedSet();
            var scannedPhotos = scanResult.Photos
                .Select(photo => AppState.NormalizePath(photo.RelativePath))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            selectedPhotos.RemoveAll(selected =>
                !scannedPhotos.Contains(AppState.NormalizePath(selected)) ||
                assigned.Contains(AppState.NormalizePath(selected)) ||
                TryParseRule1Photo(selected, OpenedRootName()) is null);
            if (selectedPhotos.Count == 0)
            {
                isRule1Selection = false;
            }

            unclassifiedSelectionAnchor = string.Empty;
            return;
        }

        selectedPhotos.RemoveAll(selected =>
            !visibleUnclassifiedPhotos.Any(visible => AppState.SamePath(visible, selected)));
        if (!visibleUnclassifiedPhotos.Any(visible => AppState.SamePath(visible, unclassifiedSelectionAnchor)))
        {
            unclassifiedSelectionAnchor = string.Empty;
        }
    }

    private void ClearUnclassifiedPhotoSelection()
    {
        selectedPhotos.Clear();
        unclassifiedSelectionAnchor = string.Empty;
        isRule1Selection = false;
    }

    private void UpdateUnclassifiedPhotoSelectionVisuals()
    {
        var showOrderBadges = !isRule1Selection && selectedPhotos.Count >= 2;
        foreach (var (relativePath, view) in unclassifiedPhotoCardViews)
        {
            var selectedIndex = SelectedPhotoIndex(relativePath);
            var isSelected = selectedIndex >= 0;
            view.Card.Background = isSelected
                ? Brush("#eaf4ff")
                : Brushes.White;
            view.Card.BorderBrush = view.Card.IsPointerOver
                ? Brush("#2f80ed")
                : isSelected
                    ? Brush("#2f80ed")
                    : Brush("#d8dde2");
            view.Badge.IsVisible = showOrderBadges && isSelected;
            view.BadgeText.Text = view.Badge.IsVisible ? (selectedIndex + 1).ToString() : string.Empty;
        }

        UpdateUnclassifiedFolderStatusCounts();

        if (unclassifiedSelectionAction is not null && unclassifiedSelectionActionSummary is not null)
        {
            UpdateUnclassifiedSelectionAction(unclassifiedSelectionAction, unclassifiedSelectionActionSummary);
        }
    }

    private void UpdateUnclassifiedFolderStatusCounts()
    {
        var selectedCounts = PhotoCountsByFolder(selectedPhotos);
        var assigned = state.AssignedSet();
        var classifiedCounts = PhotoCountsByFolder(scanResult.Photos
            .Select(photo => AppState.NormalizePath(photo.RelativePath))
            .Where(assigned.Contains));

        foreach (var (folderKey, label) in unclassifiedFolderStatusLabels)
        {
            selectedCounts.TryGetValue(folderKey, out var selectedCount);
            classifiedCounts.TryGetValue(folderKey, out var classifiedCount);
            label.Text = FormatUnclassifiedFolderStatus(selectedCount, classifiedCount);
            label.IsVisible = label.Text.Length > 0;
        }
    }

    private static string FormatUnclassifiedFolderStatus(int selectedCount, int classifiedCount)
    {
        var parts = new List<string>(2);
        if (selectedCount > 0)
        {
            parts.Add($"{selectedCount}개 선택됨");
        }

        if (classifiedCount > 0)
        {
            parts.Add($"{classifiedCount}개 분류됨");
        }

        return string.Join(" · ", parts);
    }

    private static Dictionary<string, int> PhotoCountsByFolder(IEnumerable<string> relativePaths)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var relativePath in relativePaths)
        {
            var parts = SplitRelativePath(relativePath);
            if (parts.Length == 0)
            {
                continue;
            }

            for (var folderDepth = parts.Length - 1; folderDepth >= 1; folderDepth--)
            {
                IncrementFolderPhotoCount(counts, string.Join('/', parts[..folderDepth]));
            }

            IncrementFolderPhotoCount(counts, UnclassifiedRootKey);
        }

        return counts;
    }

    private static void IncrementFolderPhotoCount(Dictionary<string, int> counts, string folderKey)
    {
        counts.TryGetValue(folderKey, out var count);
        counts[folderKey] = count + 1;
    }

    private void UpdateUnclassifiedSelectionAction(Border action, TextBlock summary)
    {
        var selectedCount = selectedPhotos.Count;
        action.IsVisible = selectedCount > 0;
        if (selectedCount == 0)
        {
            isUnclassifiedSelectionActionExpanded = false;
            summary.Text = string.Empty;
            if (unclassifiedSelectionActionDetails is not null)
            {
                unclassifiedSelectionActionDetails.Text = string.Empty;
            }
            UpdateUnclassifiedSelectionActionExpansion();
            return;
        }

        if (isRule1Selection)
        {
            var groupCount = BuildRule1GroupPlans(selectedPhotos, OpenedRootName()).Count;
            summary.Text = $"사진 {selectedCount}장이 선택되었습니다 (규칙1 기준 선택 적용)";
            if (unclassifiedSelectionActionDetails is not null)
            {
                unclassifiedSelectionActionDetails.Text =
                    "• 규칙1: 사진 파일 이름이 {숫자} + \"전\" | \"중\" | \"후\"인 경우\n" +
                    $"• 공종 페이지 {groupCount}개 추가\n" +
                    "• 공종 제목: \"{폴더 이름} {숫자}구역\"";
            }
            ApplyUnclassifiedSelectionActionPalette(action, "#eaf4ff", "#b9d8ff", "#2f80ed", "#246fd1", "#1d5db3");
            UpdateUnclassifiedSelectionActionExpansion();
            return;
        }

        var suggestedTitle = SuggestedGroupTitle(selectedPhotos, OpenedRootName());
        summary.Text = $"사진 {selectedCount}장이 선택되었습니다";
        if (unclassifiedSelectionActionDetails is not null)
        {
            unclassifiedSelectionActionDetails.Text = string.IsNullOrWhiteSpace(suggestedTitle)
                ? "선택 순서대로 전/중/후/나머지 영역에 자동 배치됩니다."
                : $"선택 순서대로 전/중/후/나머지 영역에 자동 배치됩니다.\n공종 제목은 첫 번째 선택 사진의 폴더 이름인 \"{suggestedTitle}\"로 자동 입력됩니다.";
        }
        ApplyUnclassifiedSelectionActionPalette(action, "#eaf4ff", "#b9d8ff", "#2f80ed", "#246fd1", "#1d5db3");
        UpdateUnclassifiedSelectionActionExpansion();
    }

    private void UpdateUnclassifiedSelectionActionExpansion()
    {
        if (unclassifiedSelectionActionDetails is null || unclassifiedSelectionActionToggleIcon is null)
        {
            return;
        }

        unclassifiedSelectionActionDetails.IsVisible = isUnclassifiedSelectionActionExpanded;
        unclassifiedSelectionActionToggleIcon.Text = isUnclassifiedSelectionActionExpanded ? "▾" : "▸";
    }

    private void ApplyUnclassifiedSelectionActionPalette(
        Border action,
        string actionBackground,
        string actionBorder,
        string buttonBackground,
        string buttonHover,
        string buttonPressed)
    {
        action.Background = Brush(actionBackground);
        action.BorderBrush = Brush(actionBorder);
        if (unclassifiedSelectionActionButton is null)
        {
            return;
        }

        unclassifiedSelectionActionButton.Background = Brush(buttonBackground);
        unclassifiedSelectionActionButton.Resources["ButtonBackground"] = Brush(buttonBackground);
        unclassifiedSelectionActionButton.Resources["ButtonBackgroundPointerOver"] = Brush(buttonHover);
        unclassifiedSelectionActionButton.Resources["ButtonBackgroundPressed"] = Brush(buttonPressed);
    }

    private void SelectPhotosByRule1()
    {
        var assigned = state.AssignedSet();
        var scannedPaths = scanResult.Photos
            .Select(photo => photo.RelativePath)
            .ToList();
        var totalMatchCount = BuildRule1GroupPlans(scannedPaths, OpenedRootName())
            .Sum(plan => plan.Photos.Count);
        var candidates = scannedPaths
            .Where(path => !assigned.Contains(AppState.NormalizePath(path)))
            .ToList();
        var plans = BuildRule1GroupPlans(candidates, OpenedRootName());

        selectedPhotos.Clear();
        selectedPhotos.AddRange(plans.SelectMany(plan => plan.Photos
            .OrderBy(photo => photo.PhaseIndex)
            .ThenBy(photo => photo.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Select(photo => photo.RelativePath)));
        unclassifiedSelectionAnchor = string.Empty;
        isRule1Selection = selectedPhotos.Count > 0;
        status.Text = Rule1SelectionStatus(selectedPhotos.Count, plans.Count, totalMatchCount);
        UpdateUnclassifiedPhotoSelectionVisuals();
    }

    private static string Rule1SelectionStatus(int selectedCount, int plannedGroupCount, int totalMatchCount)
    {
        if (selectedCount > 0)
        {
            return $"규칙1 사진 {selectedCount}장과 생성 예정 공종 {plannedGroupCount}개를 선택했습니다.";
        }

        return totalMatchCount > 0
            ? $"규칙1에 해당하는 사진이 {totalMatchCount}개가 있으나, 모두 분류된 상태입니다."
            : "규칙1에 해당하는 사진이 없습니다.";
    }

    private void AddPhotoGroupsFromRule1Selection()
    {
        var plans = BuildRule1GroupPlans(selectedPhotos, OpenedRootName());
        if (plans.Count == 0)
        {
            ClearUnclassifiedPhotoSelection();
            UpdateUnclassifiedPhotoSelectionVisuals();
            status.Text = "추가할 규칙1 사진이 없습니다.";
            return;
        }

        var createdGroupIds = new List<string>();
        var createdGroups = new List<PhotoGroup>();
        var assignedPaths = new List<string>();
        var assignedPhotoCount = 0;
        foreach (var plan in plans)
        {
            var group = state.AddGroup();
            group.Title = $"{plan.FolderName} {plan.AreaNumber}구역";
            createdGroupIds.Add(group.Id);
            createdGroups.Add(group);

            var duplicatePhasePhotos = new List<string>();
            for (var phaseIndex = 0; phaseIndex < 3; phaseIndex++)
            {
                var phasePhotos = plan.Photos
                    .Where(photo => photo.PhaseIndex == phaseIndex)
                    .OrderBy(photo => photo.RelativePath, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (phasePhotos.Count == 0)
                {
                    continue;
                }

                if (state.PlacePhotoAt(group.Id, omit: false, phaseIndex, phasePhotos[0].RelativePath, out _))
                {
                    assignedPhotoCount++;
                    assignedPaths.Add(phasePhotos[0].RelativePath);
                }
                duplicatePhasePhotos.AddRange(phasePhotos.Skip(1).Select(photo => photo.RelativePath));
            }

            if (duplicatePhasePhotos.Count > 0)
            {
                assignedPhotoCount += state.PlacePhotosInCollection(group.Id, omit: true, duplicatePhasePhotos);
                assignedPaths.AddRange(duplicatePhasePhotos);
            }
        }

        selectedGroupId = createdGroupIds[0];
        var createdGroupCount = plans.Count;
        ClearUnclassifiedPhotoSelection();
        AppendClassifiedGroups(createdGroups);
        RefreshUnclassifiedAssignmentCards(assignedPaths);
        RefreshPanelHeaders();
        RefreshDetail();
        ScheduleHighlightClassifiedPhotos(assignedPaths);
        ScheduleRevealCreatedClassifiedGroups(createdGroupIds);
        QueueAutoSave($"규칙1 사진 {assignedPhotoCount}장으로 공종 {createdGroupCount}개를 추가했습니다");
    }

    private static List<Rule1GroupPlan> BuildRule1GroupPlans(
        IEnumerable<string> relativePaths,
        string openedRootName)
    {
        var plans = new Dictionary<string, Rule1GroupPlan>(StringComparer.OrdinalIgnoreCase);
        foreach (var relativePath in relativePaths)
        {
            var photo = TryParseRule1Photo(relativePath, openedRootName);
            if (photo is null)
            {
                continue;
            }

            var key = photo.FolderKey + "\u001f" + photo.AreaNumber;
            if (!plans.TryGetValue(key, out var plan))
            {
                plan = new Rule1GroupPlan(photo.FolderKey, photo.FolderName, photo.AreaNumber);
                plans[key] = plan;
            }
            plan.Photos.Add(photo);
        }

        return plans.Values
            .OrderBy(plan => plan.FolderKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(plan => plan.AreaNumber)
            .ToList();
    }

    private static Rule1PhotoMatch? TryParseRule1Photo(string relativePath, string openedRootName)
    {
        var fileName = Path.GetFileNameWithoutExtension(relativePath)
            .Normalize(NormalizationForm.FormC);
        var match = Rule1PhotoNamePattern.Match(fileName);
        if (!match.Success || !long.TryParse(match.Groups["number"].Value, out var areaNumber))
        {
            return null;
        }

        var phaseIndex = match.Groups["phase"].Value switch
        {
            "전" => 0,
            "중" => 1,
            "후" => 2,
            _ => -1
        };
        if (phaseIndex < 0)
        {
            return null;
        }

        var parts = SplitRelativePath(relativePath);
        if (parts.Length == 0)
        {
            return null;
        }

        var folderKey = parts.Length >= 2
            ? string.Join('/', parts[..^1])
            : string.Empty;
        var folderName = parts.Length >= 2
            ? parts[^2]
            : openedRootName.Trim();
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return null;
        }

        return new Rule1PhotoMatch(
            relativePath,
            folderKey,
            folderName,
            areaNumber,
            phaseIndex);
    }

    private sealed record Rule1PhotoMatch(
        string RelativePath,
        string FolderKey,
        string FolderName,
        long AreaNumber,
        int PhaseIndex);

    private sealed class Rule1GroupPlan(string folderKey, string folderName, long areaNumber)
    {
        public string FolderKey { get; } = folderKey;
        public string FolderName { get; } = folderName;
        public long AreaNumber { get; } = areaNumber;
        public List<Rule1PhotoMatch> Photos { get; } = [];
    }

    private void ConfigureImagePreviewTrigger(Image image, string relativePath, Func<KeyModifiers, bool>? beforePreview = null)
    {
        Point? previewStart = null;
        var previewModifiers = KeyModifiers.None;
        image.PointerPressed += (_, args) =>
        {
            var point = args.GetCurrentPoint(image);
            if (!point.Properties.IsLeftButtonPressed)
            {
                return;
            }

            previewStart = point.Position;
            previewModifiers = args.KeyModifiers;
        };
        image.PointerReleased += async (_, args) =>
        {
            if (previewStart is null)
            {
                return;
            }

            var delta = args.GetPosition(image) - previewStart.Value;
            previewStart = null;
            if (Math.Abs(delta.X) >= 6 || Math.Abs(delta.Y) >= 6)
            {
                return;
            }

            var suppressPreview = beforePreview?.Invoke(previewModifiers) ?? false;
            if (!suppressPreview)
            {
                await ShowImagePreviewAsync(relativePath);
            }
            args.Handled = true;
        };
    }

    private void ConfigureDragSource(Control control, string relativePath)
    {
        control.PointerPressed += (_, e) =>
        {
            if (e.Source is TextBox or Button)
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
            IReadOnlyList<string> draggingPhotos = selectedPhotos.Count > 1 && SelectedPhotoIndex(draggingPhoto) >= 0
                ? selectedPhotos.ToList()
                : [draggingPhoto];
            pendingDragPhoto = string.Empty;
            dragStartPoint = null;

            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(SerializePhotoDrag(draggingPhotos)));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        };

        control.PointerReleased += (_, _) =>
        {
            pendingDragPhoto = string.Empty;
            dragStartPoint = null;
        };
    }

    private void ConfigureClassifiedCellDragSource(
        Control control,
        PhotoGroup group,
        bool omit,
        int cellIndex,
        string relativePath)
    {
        Point? cellDragStart = null;
        control.PointerPressed += (_, e) =>
        {
            if (e.Source is TextBox or Button)
            {
                return;
            }

            var point = e.GetCurrentPoint(control);
            if (point.Properties.IsLeftButtonPressed)
            {
                cellDragStart = point.Position;
            }
        };

        control.PointerMoved += async (_, e) =>
        {
            if (cellDragStart is null)
            {
                return;
            }

            var point = e.GetCurrentPoint(control);
            if (!point.Properties.IsLeftButtonPressed)
            {
                cellDragStart = null;
                return;
            }

            var delta = point.Position - cellDragStart.Value;
            if (Math.Abs(delta.X) < 6 && Math.Abs(delta.Y) < 6)
            {
                return;
            }

            cellDragStart = null;
            var payload = new ClassifiedCellDragData(
                group.Id,
                omit,
                cellIndex,
                AppState.NormalizePath(relativePath));
            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(
                ClassifiedCellDragPrefix + JsonSerializer.Serialize(payload)));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        };

        control.PointerReleased += (_, _) => cellDragStart = null;
    }

    private void ConfigurePhotoSlotDropTarget(
        Control control,
        PhotoGroup targetGroup,
        bool omit,
        int cellIndex,
        double slotWidth,
        double collectionWidth,
        Border insertionIndicator)
    {
        DragDrop.SetAllowDrop(control, true);
        DragDrop.AddDragOverHandler(control, (_, e) =>
        {
            var classifiedCell = GetDroppedClassifiedCell(e);
            if (classifiedCell is not null)
            {
                var insertAfter = e.GetPosition(control).X >= control.Bounds.Width / 2;
                var insertionIndex = cellIndex + (insertAfter ? 1 : 0);
                ShowClassifiedCellInsertionIndicator(
                    insertionIndicator,
                    insertionIndex * slotWidth,
                    collectionWidth);
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
                return;
            }

            insertionIndicator.IsVisible = false;
            if (!CanDropPhoto(e))
            {
                return;
            }

            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
        control.AddHandler(
            DragDrop.DragLeaveEvent,
            (_, _) => insertionIndicator.IsVisible = false,
            RoutingStrategies.Bubble);
        DragDrop.AddDropHandler(control, (_, e) =>
        {
            var classifiedCell = GetDroppedClassifiedCell(e);
            if (classifiedCell is not null)
            {
                var insertAfter = e.GetPosition(control).X >= control.Bounds.Width / 2;
                insertionIndicator.IsVisible = false;
                MoveDroppedClassifiedCell(
                    targetGroup,
                    omit,
                    cellIndex + (insertAfter ? 1 : 0),
                    classifiedCell);
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
                return;
            }

            insertionIndicator.IsVisible = false;
            var relativePaths = GetDroppedPhotos(e);
            if (relativePaths.Count == 0)
            {
                return;
            }

            if (relativePaths.Count > 1)
            {
                AssignDroppedPhotosBesideCell(targetGroup, omit, cellIndex, relativePaths);
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
                return;
            }

            var relativePath = relativePaths[0];
            var filledExistingCell = false;
            if (!state.PlacePhotoAt(targetGroup.Id, omit, cellIndex, relativePath, out filledExistingCell))
            {
                return;
            }
            ClearUnclassifiedPhotoSelection();
            selectedGroupId = targetGroup.Id;
            status.Text = filledExistingCell
                ? $"{targetGroup.Title} > {(omit ? "나머지" : "대상")} 빈 셀에 이동했습니다: {relativePath}"
                : $"{targetGroup.Title} > {(omit ? "나머지" : "대상")} 셀 오른쪽으로 이동했습니다: {relativePath}";
            RefreshAfterAssigningPhotos(targetGroup, [relativePath]);
            QueueAutoSave("사진 위치를 저장했습니다");
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
            var relativePaths = GetDroppedPhotos(e);
            if (relativePaths.Count == 0)
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var affectedGroupIds = ClassifiedGroupIdsContainingPhotos(relativePaths);
            var refreshSnapshot = CaptureClassifiedGroupRefreshSnapshot(affectedGroupIds);
            foreach (var relativePath in relativePaths)
            {
                state.RemovePhoto(relativePath);
            }
            ClearUnclassifiedPhotoSelection();
            pendingDragPhoto = string.Empty;
            dragStartPoint = null;
            status.Text = relativePaths.Count == 1
                ? "분류에서 제거했습니다: " + relativePaths[0]
                : $"사진 {relativePaths.Count}장을 분류에서 제거했습니다.";
            RefreshPanelHeaders();
            RefreshClassifiedGroups(refreshSnapshot);
            RefreshUnclassifiedAssignmentCards(relativePaths);
            if (affectedGroupIds.Contains(selectedGroupId, StringComparer.OrdinalIgnoreCase))
            {
                RefreshDetail();
            }
            QueueAutoSave("분류에서 제거했습니다");
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
    }

    private void AssignDroppedPhotosToCollection(
        PhotoGroup targetGroup,
        bool omit,
        IReadOnlyList<string> relativePaths,
        string destinationName)
    {
        var assignedCount = state.PlacePhotosInCollection(targetGroup.Id, omit, relativePaths);
        if (assignedCount == 0)
        {
            return;
        }

        ClearUnclassifiedPhotoSelection();
        pendingDragPhoto = string.Empty;
        dragStartPoint = null;
        selectedGroupId = targetGroup.Id;
        status.Text = $"{targetGroup.Title}의 {destinationName} 영역에 사진 {assignedCount}장을 선택 순서대로 이동했습니다.";
        RefreshAfterAssigningPhotos(targetGroup, relativePaths);
        QueueAutoSave("사진 위치를 저장했습니다");
    }

    private void MoveDroppedClassifiedCell(
        PhotoGroup targetGroup,
        bool targetOmit,
        int targetInsertionIndex,
        ClassifiedCellDragData source)
    {
        var refreshSnapshot = CaptureClassifiedGroupRefreshSnapshot([source.GroupId, targetGroup.Id]);
        if (!state.MoveAssignedPhotoCellToInsertionIndex(
                targetGroup.Id,
                targetOmit,
                targetInsertionIndex,
                source.GroupId,
                source.Omit,
                source.CellIndex))
        {
            return;
        }

        selectedGroupId = targetGroup.Id;
        status.Text = $"{targetGroup.Title} > {(targetOmit ? "나머지" : "대상")} 영역으로 셀을 이동했습니다.";
        RefreshPanelHeaders();
        RefreshClassifiedGroups(refreshSnapshot);
        RefreshDetail();
        if (!string.IsNullOrWhiteSpace(source.RelativePath))
        {
            ScheduleHighlightClassifiedPhotos([source.RelativePath]);
        }
        QueueAutoSave("사진 셀 위치를 저장했습니다");
    }

    private void AssignDroppedPhotosBesideCell(
        PhotoGroup targetGroup,
        bool omit,
        int cellIndex,
        IReadOnlyList<string> relativePaths)
    {
        var assignedCount = state.PlacePhotosBesideCell(targetGroup.Id, omit, cellIndex, relativePaths);
        if (assignedCount == 0)
        {
            return;
        }

        ClearUnclassifiedPhotoSelection();
        pendingDragPhoto = string.Empty;
        dragStartPoint = null;
        selectedGroupId = targetGroup.Id;
        status.Text = $"{targetGroup.Title}에 사진 {assignedCount}장을 드롭한 셀 옆으로 이동했습니다.";
        RefreshAfterAssigningPhotos(targetGroup, relativePaths);
        QueueAutoSave("사진 위치를 저장했습니다");
    }

    private void RefreshAfterAssigningPhotos(PhotoGroup targetGroup, IReadOnlyList<string> relativePaths)
    {
        var refreshSnapshot = CaptureClassifiedGroupRefreshSnapshot([targetGroup.Id]);
        var savedUnclassifiedOffset = unclassifiedTreeScrollViewer?.Offset;
        var assignedGroupNumber = state.Groups.FindIndex(group => group.Id == targetGroup.Id) + 1;
        var assignedPaths = relativePaths
            .Select(AppState.NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var replacedAll = assignedGroupNumber > 0 && assignedPaths.Count > 0;
        foreach (var relativePath in assignedPaths)
        {
            replacedAll &= ReplaceRenderedUnclassifiedPhotoCard(relativePath, assignedGroupNumber);
        }

        if (replacedAll)
        {
            UpdateUnclassifiedPhotoSelectionVisuals();
        }
        else
        {
            RefreshCenter();
            RestoreUnclassifiedScrollOffset(savedUnclassifiedOffset);
        }

        RefreshPanelHeaders();
        RefreshClassifiedGroups(refreshSnapshot);
        RefreshDetail();
        ScheduleHighlightClassifiedPhotos(assignedPaths);
    }

    private List<string> ClassifiedGroupIdsContainingPhotos(IEnumerable<string> relativePaths)
    {
        var paths = relativePaths
            .Select(AppState.NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return state.Groups
            .Where(group => group.AllCells().Any(cell =>
                paths.Contains(AppState.NormalizePath(cell.Image))))
            .Select(group => group.Id)
            .ToList();
    }

    private List<string> ClassifiedGroupIdsContainingPath(string relativePath, bool includeChildren)
    {
        relativePath = AppState.NormalizePath(relativePath);
        var prefix = relativePath.TrimEnd('/') + "/";
        return state.Groups
            .Where(group => group.AllCells().Any(cell =>
            {
                var imagePath = AppState.NormalizePath(cell.Image);
                return AppState.SamePath(imagePath, relativePath) ||
                    includeChildren && imagePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
            }))
            .Select(group => group.Id)
            .ToList();
    }

    private void RefreshUnclassifiedAssignmentCards(IEnumerable<string> relativePaths)
    {
        var savedOffset = unclassifiedTreeScrollViewer?.Offset;
        var assignedGroupNumbers = AssignedGroupNumbers();
        var needsCenterFallback = false;
        foreach (var relativePath in relativePaths
                     .Select(AppState.NormalizePath)
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var isRendered = renderedUnclassifiedPhotoCards.Any(candidate =>
                AppState.SamePath(candidate.RelativePath, relativePath));
            if (!isRendered)
            {
                continue;
            }

            var replaced = assignedGroupNumbers.TryGetValue(relativePath, out var groupNumber)
                ? ReplaceRenderedUnclassifiedPhotoCard(relativePath, groupNumber)
                : ReplaceRenderedAssignedPhotoCardWithUnclassified(relativePath);
            needsCenterFallback |= !replaced;
        }

        if (needsCenterFallback)
        {
            RefreshCenter();
            RestoreUnclassifiedScrollOffset(savedOffset);
            return;
        }

        UpdateUnclassifiedPhotoSelectionVisuals();
    }

    private void ScheduleHighlightClassifiedPhotos(IReadOnlyList<string> relativePaths)
    {
        var normalizedPaths = relativePaths
            .Select(AppState.NormalizePath)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        DispatcherTimer.RunOnce(() =>
        {
            foreach (var relativePath in normalizedPaths)
            {
                if (classifiedPhotoCardViews.TryGetValue(relativePath, out var photoCard))
                {
                    HighlightClassifiedPhotoCard(photoCard);
                }
            }
        }, TimeSpan.FromMilliseconds(16), DispatcherPriority.Render);
    }

    private bool ReplaceRenderedUnclassifiedPhotoCard(string relativePath, int assignedGroupNumber)
    {
        var renderedIndex = renderedUnclassifiedPhotoCards.FindIndex(candidate =>
            AppState.SamePath(candidate.RelativePath, relativePath));
        if (renderedIndex < 0)
        {
            return false;
        }

        var renderedCard = renderedUnclassifiedPhotoCards[renderedIndex];
        if (renderedCard.Card.GetVisualParent() is not Panel parent)
        {
            return false;
        }

        var childIndex = parent.Children.IndexOf(renderedCard.Card);
        if (childIndex < 0)
        {
            return false;
        }

        renderedUnclassifiedPhotoCards.RemoveAt(renderedIndex);
        unclassifiedPhotoCardViews.Remove(relativePath);
        unclassifiedAssignedGroupNumberTexts.Remove(relativePath);
        visibleUnclassifiedPhotos.RemoveAll(path => AppState.SamePath(path, relativePath));

        var replacement = PhotoCard(relativePath, assignedGroupNumber);
        var appendedRenderedIndex = renderedUnclassifiedPhotoCards.FindLastIndex(candidate =>
            ReferenceEquals(candidate.Card, replacement));
        if (appendedRenderedIndex >= 0)
        {
            var replacementEntry = renderedUnclassifiedPhotoCards[appendedRenderedIndex];
            renderedUnclassifiedPhotoCards.RemoveAt(appendedRenderedIndex);
            renderedUnclassifiedPhotoCards.Insert(
                Math.Clamp(renderedIndex, 0, renderedUnclassifiedPhotoCards.Count),
                replacementEntry);
        }
        parent.Children.RemoveAt(childIndex);
        parent.Children.Insert(childIndex, replacement);
        return true;
    }

    private bool ReplaceRenderedAssignedPhotoCardWithUnclassified(string relativePath)
    {
        var renderedIndex = renderedUnclassifiedPhotoCards.FindIndex(candidate =>
            AppState.SamePath(candidate.RelativePath, relativePath));
        if (renderedIndex < 0)
        {
            return false;
        }

        var renderedCard = renderedUnclassifiedPhotoCards[renderedIndex];
        if (renderedCard.Card.GetVisualParent() is not Panel parent)
        {
            return false;
        }

        var childIndex = parent.Children.IndexOf(renderedCard.Card);
        if (childIndex < 0)
        {
            return false;
        }

        var visibleInsertIndex = renderedUnclassifiedPhotoCards
            .Take(renderedIndex)
            .Count(candidate => visibleUnclassifiedPhotos.Any(path =>
                AppState.SamePath(path, candidate.RelativePath)));
        renderedUnclassifiedPhotoCards.RemoveAt(renderedIndex);
        unclassifiedPhotoCardViews.Remove(relativePath);
        unclassifiedAssignedGroupNumberTexts.Remove(relativePath);
        visibleUnclassifiedPhotos.RemoveAll(path => AppState.SamePath(path, relativePath));

        var replacement = PhotoCard(relativePath, assignedGroupNumber: null);
        var appendedRenderedIndex = renderedUnclassifiedPhotoCards.FindLastIndex(candidate =>
            ReferenceEquals(candidate.Card, replacement));
        if (appendedRenderedIndex >= 0)
        {
            var replacementEntry = renderedUnclassifiedPhotoCards[appendedRenderedIndex];
            renderedUnclassifiedPhotoCards.RemoveAt(appendedRenderedIndex);
            renderedUnclassifiedPhotoCards.Insert(
                Math.Clamp(renderedIndex, 0, renderedUnclassifiedPhotoCards.Count),
                replacementEntry);
        }
        visibleUnclassifiedPhotos.RemoveAll(path => AppState.SamePath(path, relativePath));
        visibleUnclassifiedPhotos.Insert(
            Math.Clamp(visibleInsertIndex, 0, visibleUnclassifiedPhotos.Count),
            relativePath);
        parent.Children.RemoveAt(childIndex);
        parent.Children.Insert(childIndex, replacement);
        return true;
    }

    private void RestoreUnclassifiedScrollOffset(Vector? savedOffset)
    {
        if (savedOffset is null)
        {
            return;
        }

        DispatcherTimer.RunOnce(() =>
        {
            if (unclassifiedTreeScrollViewer is not { } scrollViewer)
            {
                return;
            }

            var maximumX = Math.Max(0, scrollViewer.Extent.Width - scrollViewer.Viewport.Width);
            var maximumY = Math.Max(0, scrollViewer.Extent.Height - scrollViewer.Viewport.Height);
            scrollViewer.Offset = new Vector(
                Math.Clamp(savedOffset.Value.X, 0, maximumX),
                Math.Clamp(savedOffset.Value.Y, 0, maximumY));
        }, TimeSpan.FromMilliseconds(16), DispatcherPriority.Render);
    }

    private void ConfigureGroupReorder(
        Control control,
        PhotoGroup group,
        double dragHandleHeight = 54,
        Action? onDragStarted = null)
    {
        control.AddHandler(
            InputElement.PointerPressedEvent,
            (_, e) =>
            {
                if (e.Source is Button or ComboBox or Image)
                {
                    return;
                }

                var point = e.GetCurrentPoint(control);
                if (!point.Properties.IsLeftButtonPressed || point.Position.Y > dragHandleHeight)
                {
                    return;
                }

                pendingDragGroupId = group.Id;
                groupDragStartPoint = point.Position;
            },
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

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
            onDragStarted?.Invoke();

            var data = new DataTransfer();
            data.Add(DataTransferItem.CreateText(GroupDragPrefix + draggingGroupId));
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
        };

        control.AddHandler(
            InputElement.PointerReleasedEvent,
            (_, _) =>
            {
                pendingDragGroupId = string.Empty;
                groupDragStartPoint = null;
            },
            RoutingStrategies.Tunnel,
            handledEventsToo: true);

        DragDrop.SetAllowDrop(control, true);
        DragDrop.AddDragOverHandler(control, (_, e) =>
        {
            var relativePaths = GetDroppedPhotos(e);
            if (relativePaths.Count > 0)
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            var sourceGroupId = GetDroppedGroup(e);
            e.DragEffects = !string.IsNullOrWhiteSpace(sourceGroupId) && sourceGroupId != group.Id
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        });
        DragDrop.AddDropHandler(control, (_, e) =>
        {
            var relativePaths = GetDroppedPhotos(e);
            if (relativePaths.Count > 0)
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

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

    private void ConfigureDropTarget(
        Control control,
        PhotoGroup group,
        bool omit,
        Border? emptyCollectionInsertionIndicator = null)
    {
        DragDrop.SetAllowDrop(control, true);
        DragDrop.AddDragOverHandler(control, (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(GetDroppedGroup(e)))
            {
                return;
            }

            var classifiedCell = GetDroppedClassifiedCell(e);
            if (emptyCollectionInsertionIndicator is not null)
            {
                if (classifiedCell is not null)
                {
                    ShowClassifiedCellInsertionIndicator(
                        emptyCollectionInsertionIndicator,
                        control.Bounds.Width / 2,
                        control.Bounds.Width);
                }
                else
                {
                    emptyCollectionInsertionIndicator.IsVisible = false;
                }
            }
            e.DragEffects = classifiedCell is not null || CanDropPhoto(e)
                ? DragDropEffects.Move
                : DragDropEffects.None;
            e.Handled = true;
        });
        control.AddHandler(
            DragDrop.DragLeaveEvent,
            (_, _) =>
            {
                if (emptyCollectionInsertionIndicator is not null)
                {
                    emptyCollectionInsertionIndicator.IsVisible = false;
                }
            },
            RoutingStrategies.Bubble);
        DragDrop.AddDropHandler(control, (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(GetDroppedGroup(e)))
            {
                return;
            }

            var classifiedCell = GetDroppedClassifiedCell(e);
            if (classifiedCell is not null)
            {
                if (emptyCollectionInsertionIndicator is not null)
                {
                    emptyCollectionInsertionIndicator.IsVisible = false;
                }
                var targetCells = omit ? group.Omit : group.Target;
                var classifiedInsertionIndex = targetCells.Count == 0
                    ? 0
                    : targetCells.Count;
                MoveDroppedClassifiedCell(group, omit, classifiedInsertionIndex, classifiedCell);
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
                return;
            }

            if (emptyCollectionInsertionIndicator is not null)
            {
                emptyCollectionInsertionIndicator.IsVisible = false;
            }
            var relativePaths = GetDroppedPhotos(e);
            if (relativePaths.Count == 0)
            {
                e.DragEffects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            if (relativePaths.Count > 1)
            {
                AssignDroppedPhotosToCollection(group, omit, relativePaths, omit ? "나머지" : "대상");
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
                return;
            }

            var relativePath = relativePaths[0];
            var cells = omit ? group.Omit : group.Target;
            var targetIndex = cells.FindIndex(cell => string.IsNullOrWhiteSpace(cell.Image));
            if (targetIndex < 0)
            {
                targetIndex = cells.Count - 1;
            }
            var filledExistingCell = false;
            if (!state.PlacePhotoAt(group.Id, omit, targetIndex, relativePath, out filledExistingCell))
            {
                return;
            }
            ClearUnclassifiedPhotoSelection();
            status.Text = filledExistingCell
                ? $"{group.Title} > {(omit ? "나머지" : "대상")} 빈 셀에 이동했습니다: {relativePath}"
                : $"{group.Title} > {(omit ? "나머지" : "대상")} 마지막 셀로 이동했습니다: {relativePath}";
            RefreshAfterAssigningPhotos(group, [relativePath]);
            QueueAutoSave("사진 위치를 저장했습니다");
            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        });
    }

    private static bool CanDropPhoto(DragEventArgs e)
    {
        return GetDroppedPhotos(e).Count > 0;
    }

    private static ClassifiedCellDragData? GetDroppedClassifiedCell(DragEventArgs e)
    {
        var text = e.DataTransfer.TryGetText() ?? string.Empty;
        if (!text.StartsWith(ClassifiedCellDragPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ClassifiedCellDragData>(
                text[ClassifiedCellDragPrefix.Length..]);
            if (payload is null || string.IsNullOrWhiteSpace(payload.GroupId) || payload.CellIndex < 0)
            {
                return null;
            }

            return payload with
            {
                RelativePath = AppState.NormalizePath(payload.RelativePath ?? string.Empty)
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> GetDroppedPhotos(DragEventArgs e)
    {
        var text = e.DataTransfer.TryGetText() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) || text.StartsWith(GroupDragPrefix, StringComparison.Ordinal))
        {
            return [];
        }

        if (text.StartsWith(ClassifiedCellDragPrefix, StringComparison.Ordinal))
        {
            var classifiedCell = GetDroppedClassifiedCell(e);
            return classifiedCell is not null && !string.IsNullOrWhiteSpace(classifiedCell.RelativePath)
                ? [classifiedCell.RelativePath]
                : [];
        }

        if (!text.StartsWith(MultiPhotoDragPrefix, StringComparison.Ordinal))
        {
            return [text];
        }

        try
        {
            var paths = JsonSerializer.Deserialize<List<string>>(text[MultiPhotoDragPrefix.Length..]) ?? [];
            var uniquePaths = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in paths)
            {
                var normalized = AppState.NormalizePath(path);
                if (!string.IsNullOrWhiteSpace(normalized) && seen.Add(normalized))
                {
                    uniquePaths.Add(normalized);
                }
            }
            return uniquePaths;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializePhotoDrag(IReadOnlyList<string> relativePaths)
    {
        return relativePaths.Count <= 1
            ? relativePaths.FirstOrDefault() ?? string.Empty
            : MultiPhotoDragPrefix + JsonSerializer.Serialize(relativePaths);
    }

    private static string GetDroppedGroup(DragEventArgs e)
    {
        var text = e.DataTransfer.TryGetText() ?? string.Empty;
        return text.StartsWith(GroupDragPrefix, StringComparison.Ordinal)
            ? text[GroupDragPrefix.Length..]
            : string.Empty;
    }

    private sealed record ClassifiedCellDragData(
        string GroupId,
        bool Omit,
        int CellIndex,
        string RelativePath);

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

    private ContentControl EditableFileNameHost(
        string relativePath,
        double fontSize,
        IBrush foreground,
        Thickness margin,
        double width,
        TextAlignment textAlignment = TextAlignment.Center,
        bool showFullName = false,
        Action? onBeginEdit = null,
        Func<KeyModifiers, bool>? onPointerPressed = null,
        bool singleLineLayout = false)
    {
        var host = new ContentControl
        {
            Width = width,
            Height = singleLineLayout ? 24 : double.NaN,
            Margin = margin,
            HorizontalContentAlignment = textAlignment == TextAlignment.Left ? HorizontalAlignment.Left : HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        void ShowDisplay()
        {
            host.Content = FileNameDisplay(relativePath, fontSize, foreground, width, textAlignment, showFullName, singleLineLayout, modifiers =>
            {
                if (onPointerPressed?.Invoke(modifiers) == true)
                {
                    return;
                }

                onBeginEdit?.Invoke();
                ShowEditor();
            });
        }

        void ShowEditor()
        {
            host.Content = FileNameEditor(relativePath, fontSize, foreground, width, textAlignment, showFullName, singleLineLayout, _ => ShowDisplay());
        }

        ShowDisplay();
        return host;
    }

    private static TextBlock FileNameDisplay(
        string relativePath,
        double fontSize,
        IBrush foreground,
        double width,
        TextAlignment textAlignment,
        bool showFullName,
        bool singleLineLayout,
        Action<KeyModifiers> beginEdit)
    {
        var block = new TextBlock
        {
            Text = EditableName(relativePath, showFullName),
            Width = width,
            Height = singleLineLayout ? 24 : double.NaN,
            MaxHeight = singleLineLayout ? 24 : 38,
            FontSize = fontSize,
            Foreground = foreground,
            TextAlignment = textAlignment,
            TextWrapping = singleLineLayout ? TextWrapping.NoWrap : TextWrapping.Wrap,
            TextTrimming = singleLineLayout ? TextTrimming.CharacterEllipsis : TextTrimming.None,
            VerticalAlignment = VerticalAlignment.Center
        };
        block.PointerPressed += (_, args) =>
        {
            if (!args.GetCurrentPoint(block).Properties.IsLeftButtonPressed)
            {
                return;
            }

            beginEdit(args.KeyModifiers);
            args.Handled = true;
        };
        return block;
    }

    private TextBox FileNameEditor(
        string relativePath,
        double fontSize,
        IBrush foreground,
        double width,
        TextAlignment textAlignment,
        bool showFullName,
        bool singleLineLayout,
        Action<TextBox> cancelEdit,
        bool focusOnAttach = true)
    {
        var box = new TextBox
        {
            Text = EditableName(relativePath, showFullName),
            Width = width,
            Height = 24,
            Padding = singleLineLayout ? new Thickness(0) : new Thickness(2, 0),
            FontSize = fontSize,
            Foreground = foreground,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            SelectionBrush = Brush("#d8ecff"),
            SelectionForegroundBrush = foreground,
            CaretBrush = foreground,
            TextAlignment = textAlignment,
            TextWrapping = TextWrapping.NoWrap,
            HorizontalContentAlignment = textAlignment == TextAlignment.Left ? HorizontalAlignment.Left : HorizontalAlignment.Center,
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
        if (focusOnAttach)
        {
            box.AttachedToVisualTree += (_, _) =>
            {
                box.Focus();
                box.CaretIndex = box.Text?.Length ?? 0;
            };
        }
        box.LostFocus += (_, _) => cancelEdit(box);
        box.KeyDown += (_, args) =>
        {
            if (args.Key != Key.Enter || box.IsReadOnly)
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

        ClassifiedGroupRefreshSnapshot? refreshSnapshot = null;
        var affectedGroupIds = new List<string>();
        var movedPath = false;
        try
        {
            relativePath = AppState.NormalizePath(relativePath);
            var absolutePath = FileScanner.ToAbsolutePath(state.RootDir, relativePath);
            var isDirectory = Directory.Exists(absolutePath);
            if (!isDirectory && !File.Exists(absolutePath))
            {
                status.Text = "파일을 찾을 수 없습니다: " + relativePath;
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
                return;
            }

            affectedGroupIds = ClassifiedGroupIdsContainingPath(relativePath, isDirectory);
            refreshSnapshot = CaptureClassifiedGroupRefreshSnapshot(affectedGroupIds);

            if (isDirectory)
            {
                Directory.Move(absolutePath, targetPath);
            }
            else
            {
                File.Move(absolutePath, targetPath);
            }
            movedPath = true;

            state.ReplaceAssignedPath(relativePath, newRelativePath, isDirectory);
            ReplaceSelectedPhotoPaths(relativePath, newRelativePath, isDirectory);
            pendingDragPhoto = ReplaceRelativePath(pendingDragPhoto, relativePath, newRelativePath, isDirectory);
            ReplaceExpandedExplorerPaths(relativePath, newRelativePath, isDirectory);

            scanResult = scanner.Scan(state.RootDir);
            RefreshPanelHeaders();
            RefreshLeft();
            RefreshCenter();
            RefreshClassifiedGroups(refreshSnapshot);
            if (affectedGroupIds.Contains(selectedGroupId, StringComparer.OrdinalIgnoreCase))
            {
                RefreshDetail();
            }
            status.Text = "이름을 변경했습니다: " + newRelativePath;
            QueueAutoSave("이름을 변경했습니다");
        }
        catch (Exception ex)
        {
            _ = ShowErrorAsync(ex);
            if (movedPath)
            {
                scanResult = scanner.Scan(state.RootDir);
                RefreshPanelHeaders();
                RefreshLeft();
                RefreshCenter();
                if (refreshSnapshot is not null)
                {
                    RefreshClassifiedGroups(refreshSnapshot);
                }
                if (affectedGroupIds.Contains(selectedGroupId, StringComparer.OrdinalIgnoreCase))
                {
                    RefreshDetail();
                }
            }
        }
    }

    private void ReplaceSelectedPhotoPaths(string oldPath, string newPath, bool includeChildren)
    {
        var updatedPaths = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var selectedPhoto in selectedPhotos)
        {
            var updatedPath = ReplaceRelativePath(selectedPhoto, oldPath, newPath, includeChildren);
            if (seen.Add(updatedPath))
            {
                updatedPaths.Add(updatedPath);
            }
        }

        selectedPhotos.Clear();
        selectedPhotos.AddRange(updatedPaths);
        if (!string.IsNullOrWhiteSpace(unclassifiedSelectionAnchor))
        {
            unclassifiedSelectionAnchor = ReplaceRelativePath(
                unclassifiedSelectionAnchor,
                oldPath,
                newPath,
                includeChildren);
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
