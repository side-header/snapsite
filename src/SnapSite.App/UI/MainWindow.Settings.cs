using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using NewGreen.Domain;
using NewGreen.Infrastructure.Persistence;

namespace NewGreen.UI;

public sealed partial class MainWindow
{
    private const double SettingsComparisonTableWidth = 565;

    private enum PaperTemplateFormat
    {
        Hwpx,
        Docx
    }

    private static PaperTemplateInputSet PaperTemplateInputsFor(PaperTemplateSettings settings, int imageDpi, int jpegQuality)
    {
        var imageDpiValue = new TextBlock
        {
            Text = $"{ExportSettings.NormalizeImageDpi(imageDpi)}",
            Width = 36,
            Foreground = Brush("#333"),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right
        };
        var imageDpiSlider = PaperTemplateImageDpiSlider(imageDpi, imageDpiValue);
        var jpegQualityValue = new TextBlock
        {
            Text = $"{ExportSettings.NormalizeJpegQuality(jpegQuality)}",
            Width = 36,
            Foreground = Brush("#333"),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right
        };
        var jpegQualitySlider = PaperTemplateQualitySlider(jpegQuality, jpegQualityValue);
        return new PaperTemplateInputSet(
            PaperTemplateTextBox(settings.Title),
            PaperTemplateNumberBox(settings.TitleFontPt),
            PaperTemplateTextBox(settings.Subtitle),
            PaperTemplateNumberBox(settings.SubtitleFontPt),
            PaperTemplateTextBox(settings.Company),
            PaperTemplateNumberBox(settings.CompanyFontPt),
            PaperTemplateTextBox(settings.BodyTitle),
            PaperTemplateNumberBox(settings.BodyTitleFontPt),
            PaperTemplateTextBox(settings.BodySubtitle),
            PaperTemplateNumberBox(settings.BodySubtitleFontPt),
            PaperTemplateNumberBox(settings.LineSpacingPercent),
            PaperTemplateTextBox(settings.FontFamily),
            PaperTemplateCheckBox(settings.ShowPageNumber),
            imageDpiSlider,
            imageDpiValue,
            jpegQualitySlider,
            jpegQualityValue);
    }

    private static Grid BuildPaperTemplateBody(PaperTemplateSettings settings, PaperTemplateInputSet inputs, int lineSpacingFallback = 160)
    {
        var body = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("900,*"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var previewHost = new ContentControl
        {
            Content = PaperTemplatePreview(settings)
        };
        void RefreshPreview()
        {
            previewHost.Content = PaperTemplatePreview(PaperTemplateFromInputs(inputs, lineSpacingFallback));
        }

        foreach (var input in inputs.TextBoxes)
        {
            input.TextChanged += (_, _) => RefreshPreview();
        }

        AddToGrid(body, previewHost, 0, 0);
        AddToGrid(body, PaperTemplateForm(inputs), 1, 0);
        return body;
    }

    private static PaperTemplateSettings PaperTemplateFromInputs(PaperTemplateInputSet inputs, int lineSpacingFallback = 160)
    {
        var settings = new PaperTemplateSettings();
        ReadPaperTemplateInputs(inputs, settings, lineSpacingFallback);
        settings.Normalize();
        return settings;
    }

    private static void ReadPaperTemplateInputs(PaperTemplateInputSet inputs, PaperTemplateSettings settings, int lineSpacingFallback = 160)
    {
        settings.Title = inputs.Title.Text ?? string.Empty;
        settings.TitleFontPt = ReadPaperTemplateDouble(inputs.TitleFontPt, 37);
        settings.Subtitle = inputs.Subtitle.Text ?? string.Empty;
        settings.SubtitleFontPt = ReadPaperTemplateDouble(inputs.SubtitleFontPt, 17);
        settings.Company = inputs.Company.Text ?? string.Empty;
        settings.CompanyFontPt = ReadPaperTemplateDouble(inputs.CompanyFontPt, 22);
        settings.BodyTitle = inputs.BodyTitle.Text ?? string.Empty;
        settings.BodyTitleFontPt = ReadPaperTemplateDouble(inputs.BodyTitleFontPt, 23);
        settings.BodySubtitle = inputs.BodySubtitle.Text ?? string.Empty;
        settings.BodySubtitleFontPt = ReadPaperTemplateDouble(inputs.BodySubtitleFontPt, 14);
        settings.LineSpacingPercent = ReadPaperTemplateNumber(inputs.LineSpacingPercent, lineSpacingFallback);
        settings.FontFamily = inputs.FontFamily.Text ?? string.Empty;
        settings.ShowPageNumber = inputs.ShowPageNumber.IsChecked == true;
    }

    private static Control PaperTemplateForm(PaperTemplateInputSet inputs)
    {
        var form = new StackPanel
        {
            Spacing = 0,
            Width = 520,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        form.Children.Add(PaperTemplateSectionHeader("공통"));
        form.Children.Add(PaperTemplateInputRow("문단 모양 > 줄 간격", inputs.LineSpacingPercent, "%"));
        form.Children.Add(PaperTemplateInputRow("글씨체", inputs.FontFamily));
        form.Children.Add(PaperTemplatePageNumberRow(inputs.ShowPageNumber));
        form.Children.Add(PaperTemplateFixedValueRow("사진 사이즈 조절", "삽입 크기에 맞게"));
        form.Children.Add(PaperTemplateImageDpiRow(inputs.ImageDpi, inputs.ImageDpiValue));
        form.Children.Add(PaperTemplateQualityRow(inputs.JpegQuality, inputs.JpegQualityValue));
        form.Children.Add(PaperTemplateSectionHeader("1 페이지"));
        form.Children.Add(PaperTemplateInputRow("제목", inputs.Title));
        form.Children.Add(PaperTemplateInputRow("제목 글자크기", inputs.TitleFontPt, "pt"));
        form.Children.Add(PaperTemplateInputRow("소제목", inputs.Subtitle));
        form.Children.Add(PaperTemplateInputRow("소제목 글자크기", inputs.SubtitleFontPt, "pt"));
        form.Children.Add(PaperTemplateInputRow("회사명", inputs.Company));
        form.Children.Add(PaperTemplateInputRow("회사명 글자크기", inputs.CompanyFontPt, "pt"));
        form.Children.Add(PaperTemplateSectionHeader("2 페이지부터"));
        form.Children.Add(PaperTemplateInputRow("제목", inputs.BodyTitle));
        form.Children.Add(PaperTemplateInputRow("제목 글자크기", inputs.BodyTitleFontPt, "pt"));
        form.Children.Add(PaperTemplateInputRow("소제목", inputs.BodySubtitle));
        form.Children.Add(PaperTemplateInputRow("소제목 글자크기", inputs.BodySubtitleFontPt, "pt"));
        return form;
    }

    private static Control PaperTemplateSectionHeader(string text)
    {
        return new Border
        {
            Height = 34,
            Margin = new Thickness(0, 8, 0, 6),
            Background = Brush("#e9ecef"),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brush("#222"),
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0)
            }
        };
    }

    private static Control PaperTemplateInputRow(string label, TextBox input, string suffix = "")
    {
        var row = new Grid
        {
            ColumnDefinitions = string.IsNullOrEmpty(suffix) ? new ColumnDefinitions("130,*") : new ColumnDefinitions("130,70,32"),
            Height = 34,
            Margin = new Thickness(0, 1, 0, 1)
        };
        AddToGrid(row, new TextBlock
        {
            Text = label,
            Foreground = Brush("#333"),
            VerticalAlignment = VerticalAlignment.Center
        }, 0, 0);
        AddToGrid(row, input, 1, 0);
        if (!string.IsNullOrEmpty(suffix))
        {
            AddToGrid(row, new TextBlock
            {
                Text = suffix,
                Foreground = Brush("#333"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            }, 2, 0);
        }
        return row;
    }

    private static Control PaperTemplatePageNumberRow(CheckBox input)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("130,*"),
            Height = 34,
            Margin = new Thickness(0, 1, 0, 1)
        };
        AddToGrid(row, new TextBlock
        {
            Text = "페이지 번호",
            Foreground = Brush("#333"),
            VerticalAlignment = VerticalAlignment.Center
        }, 0, 0);
        AddToGrid(row, new Border
        {
            Width = 24,
            Height = 24,
            BorderBrush = Brush("#222"),
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            Child = input
        }, 1, 0);
        return row;
    }

    private static Control PaperTemplateFixedValueRow(string label, string value)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("130,*"),
            Height = 34,
            Margin = new Thickness(0, 1, 0, 1)
        };
        AddToGrid(row, new TextBlock
        {
            Text = label,
            Foreground = Brush("#333"),
            VerticalAlignment = VerticalAlignment.Center
        }, 0, 0);
        AddToGrid(row, new TextBlock
        {
            Text = value,
            Foreground = Brush("#333"),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        }, 1, 0);
        return row;
    }

    private static Control PaperTemplateImageDpiRow(Slider input, TextBlock value)
    {
        return PaperTemplateSliderRow("사진 DPI", input, value, "저", "고");
    }

    private static Control PaperTemplateQualityRow(Slider input, TextBlock value)
    {
        return PaperTemplateSliderRow("사진 품질 (JPEG)", input, value, "저", "고");
    }

    private static Control PaperTemplateSliderRow(string label, Slider input, TextBlock value, string lowLabel, string highLabel)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("130,*"),
            Height = 42,
            Margin = new Thickness(0, 1, 0, 1)
        };
        AddToGrid(row, new TextBlock
        {
            Text = label,
            Foreground = Brush("#333"),
            VerticalAlignment = VerticalAlignment.Center
        }, 0, 0);

        var control = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("36,*,36,24"),
            ColumnSpacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        AddToGrid(control, new TextBlock
        {
            Text = lowLabel,
            Foreground = Brush("#555"),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        }, 0, 0);
        AddToGrid(control, input, 1, 0);
        AddToGrid(control, new TextBlock
        {
            Text = highLabel,
            Foreground = Brush("#555"),
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        }, 2, 0);
        AddToGrid(control, value, 3, 0);
        AddToGrid(row, control, 1, 0);
        return row;
    }

    private static CheckBox PaperTemplateCheckBox(bool value)
    {
        var checkBox = new CheckBox
        {
            IsChecked = value,
            Width = 22,
            Height = 22,
            Padding = new Thickness(0),
            Foreground = Brushes.Black,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        checkBox.Resources["CheckBoxForeground"] = Brushes.Black;
        checkBox.Resources["CheckBoxForegroundPointerOver"] = Brushes.Black;
        checkBox.Resources["CheckBoxForegroundPressed"] = Brushes.Black;
        checkBox.Resources["CheckBoxCheckBackground"] = Brushes.White;
        checkBox.Resources["CheckBoxCheckBackgroundUnchecked"] = Brushes.White;
        checkBox.Resources["CheckBoxCheckBorderBrush"] = Brush("#222");
        checkBox.Resources["CheckBoxCheckBorderBrushUnchecked"] = Brush("#222");
        checkBox.Resources["CheckBoxBorderBrush"] = Brush("#222");
        return checkBox;
    }

    private static TextBox PaperTemplateTextBox(string value)
    {
        var input = new TextBox
        {
            Text = value,
            Height = 28,
            Padding = new Thickness(10, 2),
            FontSize = 15,
            Foreground = Brushes.Black,
            Background = Brush("#f7f8f9"),
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        input.Resources["TextControlBackground"] = Brush("#f7f8f9");
        input.Resources["TextControlBackgroundPointerOver"] = Brush("#f7f8f9");
        input.Resources["TextControlBackgroundFocused"] = Brush("#f7f8f9");
        input.Resources["TextControlBorderBrush"] = Brushes.Transparent;
        input.Resources["TextControlBorderBrushPointerOver"] = Brushes.Transparent;
        input.Resources["TextControlBorderBrushFocused"] = Brush("#8fc7ff");
        input.Resources["TextControlForeground"] = Brushes.Black;
        input.Resources["TextControlForegroundPointerOver"] = Brushes.Black;
        input.Resources["TextControlForegroundFocused"] = Brushes.Black;
        input.SelectionBrush = Brush("#d8ecff");
        input.SelectionForegroundBrush = Brushes.Black;
        input.CaretBrush = Brushes.Black;
        return input;
    }

    private static TextBox PaperTemplateNumberBox(double value)
    {
        var input = PaperTemplateTextBox(FormatTemplateNumber(value));
        input.Width = 70;
        input.HorizontalAlignment = HorizontalAlignment.Left;
        return input;
    }

    private static Slider PaperTemplateQualitySlider(int value, TextBlock valueText)
    {
        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = ExportSettings.NormalizeJpegQuality(value),
            Width = 260,
            VerticalAlignment = VerticalAlignment.Center
        };
        slider.PropertyChanged += (_, args) =>
        {
            if (args.Property == RangeBase.ValueProperty)
            {
                valueText.Text = $"{ReadJpegQualityInput(slider)}";
            }
        };
        return slider;
    }

    private static Slider PaperTemplateImageDpiSlider(int value, TextBlock valueText)
    {
        var slider = new Slider
        {
            Minimum = ExportSettings.MinImageDpi,
            Maximum = ExportSettings.MaxImageDpi,
            Value = ExportSettings.NormalizeImageDpi(value),
            Width = 260,
            VerticalAlignment = VerticalAlignment.Center
        };
        slider.PropertyChanged += (_, args) =>
        {
            if (args.Property == RangeBase.ValueProperty)
            {
                valueText.Text = $"{ReadImageDpiInput(slider)}";
            }
        };
        return slider;
    }

    private static int ReadPaperTemplateNumber(TextBox input, int fallback)
    {
        return int.TryParse(input.Text, out var value) ? value : fallback;
    }

    private static double ReadPaperTemplateDouble(TextBox input, double fallback)
    {
        return double.TryParse(input.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static string FormatTemplateNumber(double value)
    {
        return value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Control PaperTemplatePreview(PaperTemplateSettings settings)
    {
        var panel = new StackPanel
        {
            Spacing = 18,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Children.Add(new TextBlock
        {
            Text = "미리보기는 글자 위치를 보여주는 용도입니다. 실제 출력 내용의 글자 크기와 줄나눔은 다를 수 있습니다.",
            Foreground = Brush("#7a8288"),
            FontSize = 13,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Width = 840
        });

        var canvas = new Canvas
        {
            Width = 860,
            Height = 650,
            Background = Brushes.White
        };
        canvas.Children.Add(PaperCoverPreview(settings, 18, 30));
        canvas.Children.Add(PaperPhotoPreview(settings, 442, 30));
        panel.Children.Add(canvas);
        return panel;
    }

    private static Control PaperCoverPreview(PaperTemplateSettings settings, double left, double top)
    {
        var page = new Canvas
        {
            Width = SettingsPreviewPaperWidth,
            Height = SettingsPreviewPaperHeight,
            Background = Brushes.White
        };
        page.Children.Add(new Border
        {
            Width = SettingsPreviewPaperWidth,
            Height = SettingsPreviewPaperHeight,
            BorderBrush = Brush("#6ca7ef"),
            BorderThickness = new Thickness(1)
        });
        page.Children.Add(TemplateText(settings.Title, settings.FontFamily, 30, 82, 360, 90, Math.Max(8, settings.TitleFontPt) * 1.02, FontWeight.Normal));
        page.Children.Add(TemplateText(settings.Subtitle, settings.FontFamily, 42, 212, 336, 92, Math.Max(8, settings.SubtitleFontPt) * 0.94, FontWeight.Bold));
        page.Children.Add(TemplateText(settings.Company, settings.FontFamily, 30, 486, 360, 68, Math.Max(8, settings.CompanyFontPt) * 1.02, FontWeight.Normal));
        Canvas.SetLeft(page, left);
        Canvas.SetTop(page, top);
        return page;
    }

    private static Control PaperPhotoPreview(PaperTemplateSettings settings, double left, double top)
    {
        var page = new Canvas
        {
            Width = SettingsPreviewPaperWidth,
            Height = SettingsPreviewPaperHeight,
            Background = Brushes.White
        };
        page.Children.Add(new Border
        {
            Width = SettingsPreviewPaperWidth,
            Height = SettingsPreviewPaperHeight,
            BorderBrush = Brush("#6ca7ef"),
            BorderThickness = new Thickness(1)
        });
        page.Children.Add(TemplateText(settings.BodyTitle, settings.FontFamily, 40, 36, 340, 40, Math.Max(8, settings.BodyTitleFontPt) * 0.95, FontWeight.Bold));
        page.Children.Add(TemplateText(settings.BodySubtitle, settings.FontFamily, 30, 82, 360, 48, Math.Max(8, settings.BodySubtitleFontPt) * 0.92, FontWeight.Bold));
        page.Children.Add(new Border
        {
            Width = 340,
            Height = 432,
            Background = Brush("#e9eef2"),
            BorderBrush = Brush("#777"),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = "전/중/후 사진",
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                Foreground = Brush("#222"),
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });
        Canvas.SetLeft(page.Children[^1], 40);
        Canvas.SetTop(page.Children[^1], 140);
        Canvas.SetLeft(page, left);
        Canvas.SetTop(page, top);
        return page;
    }

    private static Control TemplateText(string text, string fontFamily, double left, double top, double width, double height, double fontSize, FontWeight fontWeight)
    {
        var block = new TextBlock
        {
            Text = text,
            Width = width,
            Height = height,
            FontSize = fontSize,
            FontFamily = new FontFamily(string.IsNullOrWhiteSpace(fontFamily) ? "함초롬바탕" : fontFamily.Trim()),
            FontWeight = fontWeight,
            Foreground = Brush("#222"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.None
        };
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, top);
        return block;
    }

    private async Task ShowSettingsAsync()
    {
        state.ExportSettings ??= new ExportSettings();
        state.ExportSettings.Normalize();
        state.NormalizePaperTemplates();
        var paperTemplates = state.PaperTemplates!;

        var hwpxPaperTemplate = PaperTemplateInputsFor(paperTemplates.Hwpx!, state.ExportSettings.HwpxImageDpi, state.ExportSettings.HwpxJpegQuality);
        var docxPaperTemplate = PaperTemplateInputsFor(paperTemplates.Docx!, state.ExportSettings.DocxImageDpi, state.ExportSettings.DocxJpegQuality);
        var pageInputs = Enumerable.Range(
                PhotoGroup.MinCntPerPage,
                PhotoGroup.MaxCntPerPage - PhotoGroup.MinCntPerPage + 1)
            .ToDictionary(
                count => count,
                count => SettingsPageInputsFor(state.ExportSettings.SettingsFor(count)));

        var dialog = new Window
        {
            Title = "종이 설정",
            Width = 1420,
            Height = 820,
            MinWidth = 1280,
            MinHeight = 700,
            Background = Brushes.White,
            Content = BuildSettingsDialogContent(paperTemplates, hwpxPaperTemplate, docxPaperTemplate, pageInputs)
        };

        if (dialog.Content is Grid grid &&
            grid.Children.OfType<DockPanel>().LastOrDefault() is { } buttons &&
            buttons.Children.OfType<Button>().FirstOrDefault(button => Equals(button.Tag, "defaults")) is { } defaults &&
            buttons.Children.OfType<StackPanel>().SelectMany(panel => panel.Children.OfType<Button>()).FirstOrDefault(button => Equals(button.Tag, "save")) is { } save &&
            buttons.Children.OfType<StackPanel>().SelectMany(panel => panel.Children.OfType<Button>()).FirstOrDefault(button => Equals(button.Tag, "close")) is { } close)
        {
            defaults.Click += (_, _) => ResetSettingsInputsToDefaults(hwpxPaperTemplate, docxPaperTemplate, pageInputs);
            save.Click += (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(state.RootDir))
                {
                    status.Text = $"먼저 기준 폴더를 선택하세요. 설정은 기준 폴더의 {MetadataStore.MetadataFileName}에 저장됩니다.";
                    return;
                }

                ReadPaperTemplateInputs(hwpxPaperTemplate, paperTemplates.Hwpx!);
                ReadPaperTemplateInputs(docxPaperTemplate, paperTemplates.Docx!, lineSpacingFallback: 80);
                state.NormalizePaperTemplates();
                state.ExportSettings.HwpxImageDpi = ReadImageDpiInput(hwpxPaperTemplate.ImageDpi);
                state.ExportSettings.DocxImageDpi = ReadImageDpiInput(docxPaperTemplate.ImageDpi);
                state.ExportSettings.HwpxJpegQuality = ReadJpegQualityInput(hwpxPaperTemplate.JpegQuality);
                state.ExportSettings.DocxJpegQuality = ReadJpegQualityInput(docxPaperTemplate.JpegQuality);
                foreach (var (count, inputs) in pageInputs)
                {
                    ReadPageInputs(inputs, state.ExportSettings.SettingsFor(count));
                }
                state.ExportSettings.Normalize();

                if (SaveToMetadata("설정을 저장했습니다", refreshAfterSave: false))
                {
                    dialog.Close();
                }
            };
            close.Click += (_, _) => dialog.Close();
        }

        await dialog.ShowDialog(this);
    }

    private static Grid BuildSettingsDialogContent(
        PaperTemplateFormatSettings paperTemplates,
        PaperTemplateInputSet hwpxPaperTemplateInputs,
        PaperTemplateInputSet docxPaperTemplateInputs,
        IReadOnlyDictionary<int, SettingsPageInputSet> pageInputs)
    {
        var grid = new Grid
        {
            Margin = new Thickness(18),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Background = Brushes.White
        };

        paperTemplates.Normalize();
        var selectedPaperFormat = PaperTemplateFormat.Hwpx;
        var hwpxBody = BuildPaperTemplateBody(paperTemplates.Hwpx!, hwpxPaperTemplateInputs);
        var docxBody = BuildPaperTemplateBody(paperTemplates.Docx!, docxPaperTemplateInputs, lineSpacingFallback: 80);
        var commonBodyHost = new ContentControl
        {
            Content = hwpxBody
        };
        var hwpxFormatButton = SettingsFormatButton("한글", selected: true);
        var docxFormatButton = SettingsFormatButton("MS Word", selected: false);
        var commonContent = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            Background = Brushes.White
        };
        var formatTabs = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 170, 16),
            Children =
            {
                hwpxFormatButton,
                docxFormatButton
            }
        };
        AddToGrid(commonContent, formatTabs, 0, 0);
        AddToGrid(commonContent, commonBodyHost, 0, 1);
        var commonView = SettingsPageScroll(commonContent);
        var pagePreviews = pageInputs.Keys.ToDictionary(count => count, _ => new ContentControl());
        PaperTemplateInputSet CurrentPaperTemplateInputs()
        {
            return selectedPaperFormat == PaperTemplateFormat.Hwpx ? hwpxPaperTemplateInputs : docxPaperTemplateInputs;
        }

        void RefreshPhotoPagePreview(int count)
        {
            var template = PaperTemplateFromInputs(CurrentPaperTemplateInputs(), selectedPaperFormat == PaperTemplateFormat.Docx ? 80 : 160);
            pagePreviews[count].Content = SettingsPreview(count, template, WorkCellFromInputs(pageInputs[count].HwpxWorkCell));
        }

        void RefreshPhotoPagePreviews()
        {
            foreach (var count in pageInputs.Keys)
            {
                RefreshPhotoPagePreview(count);
            }
        }

        void SelectPaperFormat(PaperTemplateFormat format)
        {
            selectedPaperFormat = format;
            hwpxFormatButton.Classes.Set("selected", format == PaperTemplateFormat.Hwpx);
            docxFormatButton.Classes.Set("selected", format == PaperTemplateFormat.Docx);
            hwpxFormatButton.Background = format == PaperTemplateFormat.Hwpx ? Brush("#e9eef2") : Brushes.Transparent;
            docxFormatButton.Background = format == PaperTemplateFormat.Docx ? Brush("#e9eef2") : Brushes.Transparent;
            commonBodyHost.Content = format == PaperTemplateFormat.Hwpx ? hwpxBody : docxBody;
            RefreshPhotoPagePreviews();
        }

        RefreshPhotoPagePreviews();

        hwpxFormatButton.Click += (_, _) => SelectPaperFormat(PaperTemplateFormat.Hwpx);
        docxFormatButton.Click += (_, _) => SelectPaperFormat(PaperTemplateFormat.Docx);
        foreach (var paperTemplateInputs in new[] { hwpxPaperTemplateInputs, docxPaperTemplateInputs })
        {
            paperTemplateInputs.BodyTitle.TextChanged += (_, _) => RefreshPhotoPagePreviews();
            paperTemplateInputs.BodyTitleFontPt.TextChanged += (_, _) => RefreshPhotoPagePreviews();
            paperTemplateInputs.BodySubtitle.TextChanged += (_, _) => RefreshPhotoPagePreviews();
            paperTemplateInputs.BodySubtitleFontPt.TextChanged += (_, _) => RefreshPhotoPagePreviews();
            paperTemplateInputs.FontFamily.TextChanged += (_, _) => RefreshPhotoPagePreviews();
        }
        foreach (var (count, inputs) in pageInputs)
        {
            inputs.HwpxWorkCell.Height.TextChanged += (_, _) => RefreshPhotoPagePreview(count);
            inputs.HwpxWorkCell.Width.TextChanged += (_, _) => RefreshPhotoPagePreview(count);
        }

        var pageViews = pageInputs.Keys.ToDictionary(
            count => count,
            count => SettingsPageScroll(SettingsPagePanel(count, pageInputs[count], pagePreviews[count])));
        var contentHost = new ContentControl
        {
            Content = commonView
        };
        var commonTab = SettingsTopTabButton("공통", selected: true);
        var countCombo = new ComboBox
        {
            ItemsSource = Enumerable.Range(
                PhotoGroup.MinCntPerPage,
                PhotoGroup.MaxCntPerPage - PhotoGroup.MinCntPerPage + 1),
            SelectedItem = PhotoGroup.DefaultCntPerPage,
            Width = 64,
            Height = 30,
            Padding = new Thickness(8, 0),
            FontSize = 15,
            Foreground = Brushes.Black,
            Background = Brushes.White,
            BorderBrush = Brush("#b8c0c8"),
            BorderThickness = new Thickness(1),
            VerticalAlignment = VerticalAlignment.Center
        };
        var pageTabContent = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = "한 페이지당",
                    FontSize = 17,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Center
                },
                countCombo,
                new TextBlock
                {
                    Text = "장",
                    FontSize = 17,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.Black,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
        var pageTab = new Border
        {
            Padding = new Thickness(20, 8),
            Background = Brushes.Transparent,
            CornerRadius = new CornerRadius(8),
            Child = pageTabContent
        };
        var selectedPageCount = PhotoGroup.DefaultCntPerPage;

        void SelectCommonTab()
        {
            commonTab.Classes.Set("selected", true);
            commonTab.Background = Brush("#e9eef2");
            pageTab.Background = Brushes.Transparent;
            contentHost.Content = commonView;
        }

        void SelectPageTab()
        {
            commonTab.Classes.Set("selected", false);
            commonTab.Background = Brushes.Transparent;
            pageTab.Background = Brush("#e9eef2");
            contentHost.Content = pageViews[selectedPageCount];
        }

        commonTab.Click += (_, _) => SelectCommonTab();
        pageTab.PointerPressed += (_, args) =>
        {
            if (args.GetCurrentPoint(pageTab).Properties.IsLeftButtonPressed)
            {
                SelectPageTab();
            }
        };
        countCombo.SelectionChanged += (_, _) =>
        {
            if (countCombo.SelectedItem is int count)
            {
                selectedPageCount = PhotoGroup.NormalizeCntPerPage(count);
                RefreshPhotoPagePreview(selectedPageCount);
                SelectPageTab();
            }
        };

        AddToGrid(grid, SettingsTopTabs(commonTab, pageTab), 0, 0);
        AddToGrid(grid, contentHost, 0, 1);

        var buttons = new DockPanel
        {
            LastChildFill = false,
            Margin = new Thickness(0, 14, 0, 0)
        };
        var defaults = SettingsDialogButton("defaults", "기본 값으로 되돌리기", 150);
        DockPanel.SetDock(defaults, Dock.Left);
        buttons.Children.Add(defaults);

        var rightButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        rightButtons.Children.Add(SettingsDialogButton("save", "저장하기", 100));
        rightButtons.Children.Add(SettingsDialogButton("close", "닫기", 80));
        DockPanel.SetDock(rightButtons, Dock.Right);
        buttons.Children.Add(rightButtons);
        AddToGrid(grid, buttons, 0, 2);
        return grid;
    }

    private static Button SettingsDialogButton(string tag, string text, double minWidth)
    {
        var button = new Button
        {
            Tag = tag,
            Content = text,
            MinWidth = minWidth,
            Padding = new Thickness(12, 8),
            Background = Brushes.Transparent,
            Foreground = Brushes.Black,
            BorderBrush = Brush("#9aa3ab"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Resources["ButtonBackground"] = Brushes.Transparent;
        button.Resources["ButtonBackgroundPointerOver"] = Brush("#f1f3f5");
        button.Resources["ButtonBackgroundPressed"] = Brush("#e9ecef");
        button.Resources["ButtonBorderBrush"] = Brush("#9aa3ab");
        button.Resources["ButtonBorderBrushPointerOver"] = Brush("#69737d");
        button.Resources["ButtonBorderBrushPressed"] = Brush("#4f5963");
        button.Resources["ButtonForeground"] = Brushes.Black;
        button.Resources["ButtonForegroundPointerOver"] = Brushes.Black;
        button.Resources["ButtonForegroundPressed"] = Brushes.Black;
        return button;
    }

    private static Control SettingsTopTabs(Button commonTab, Control pageTab)
    {
        var tabs = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            Margin = new Thickness(64, 16, 0, 24),
            Children =
            {
                commonTab,
                SettingsTabDivider(),
                pageTab
            }
        };

        return tabs;
    }

    private static Button SettingsTopTabButton(string text, bool selected)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = text == "공통" ? 70 : 136,
            Padding = new Thickness(20, 12),
            Background = selected ? Brush("#e9eef2") : Brushes.Transparent,
            Foreground = Brushes.Black,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(8),
            FontSize = 17,
            FontWeight = FontWeight.Bold,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        button.Classes.Set("selected", selected);
        button.Resources["ButtonBackground"] = Brushes.Transparent;
        button.Resources["ButtonBackgroundPointerOver"] = Brush("#f1f3f5");
        button.Resources["ButtonBackgroundPressed"] = Brush("#e9eef2");
        button.Resources["ButtonBorderBrush"] = Brushes.Transparent;
        button.Resources["ButtonBorderBrushPointerOver"] = Brushes.Transparent;
        button.Resources["ButtonBorderBrushPressed"] = Brushes.Transparent;
        return button;
    }

    private static Button SettingsFormatButton(string text, bool selected)
    {
        var button = SettingsTopTabButton(text, selected);
        button.MinWidth = text == "한글" ? 64 : 104;
        button.Padding = new Thickness(16, 8);
        button.FontSize = 16;
        button.FontWeight = FontWeight.Normal;
        button.CornerRadius = new CornerRadius(6);
        return button;
    }

    private static Control SettingsTabDivider()
    {
        return new Border
        {
            Width = 1,
            Height = 42,
            Background = Brush("#333"),
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static Control SettingsPageScroll(Control content)
    {
        return new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private static SettingsPageInputSet SettingsPageInputsFor(ExportPageSettings settings)
    {
        settings.Normalize();
        return new SettingsPageInputSet(
            SettingsInputsFor(settings.Hwpx),
            SettingsCellInputsFor(settings.HwpxCell!),
            SettingsCellInputsFor(settings.HwpxPhoto!),
            SettingsWorkCellInputsFor(settings.HwpxWorkCell!),
            SettingsInputsFor(settings.Docx),
            SettingsCellInputsFor(settings.DocxCell!),
            SettingsCellInputsFor(settings.DocxPhoto!),
            SettingsWorkCellInputsFor(settings.DocxWorkCell!));
    }

    private static void ReadPageInputs(SettingsPageInputSet inputs, ExportPageSettings settings)
    {
        ReadMarginInputs(inputs.HwpxPaper, settings.Hwpx);
        settings.HwpxCell!.VerticalMm = 0;
        settings.HwpxCell!.HorizontalMm = 0;
        ReadCellMarginInputs(inputs.HwpxPhoto, settings.HwpxPhoto!);
        ReadMarginInputs(inputs.DocxPaper, settings.Docx);
        settings.DocxCell!.VerticalMm = 0;
        settings.DocxCell!.HorizontalMm = 0;
        ReadCellMarginInputs(inputs.DocxPhoto, settings.DocxPhoto!);
        ReadWorkCellInputs(inputs.HwpxWorkCell, settings.HwpxWorkCell!);
        ReadWorkCellInputs(inputs.DocxWorkCell, settings.DocxWorkCell!);
        settings.Normalize();
    }

    private static void ResetSettingsInputsToDefaults(
        PaperTemplateInputSet hwpxPaperTemplate,
        PaperTemplateInputSet docxPaperTemplate,
        IReadOnlyDictionary<int, SettingsPageInputSet> pageInputs)
    {
        var defaultTemplate = new PaperTemplateSettings();
        defaultTemplate.Normalize();
        var defaultDocxTemplate = PaperTemplateSettings.DocxDefault();
        defaultDocxTemplate.Normalize();
        WritePaperTemplateInputs(hwpxPaperTemplate, defaultTemplate);
        WritePaperTemplateInputs(docxPaperTemplate, defaultDocxTemplate);

        var defaultSettings = new ExportSettings();
        defaultSettings.Normalize();
        WriteImageDpiInput(hwpxPaperTemplate, defaultSettings.HwpxImageDpi);
        WriteImageDpiInput(docxPaperTemplate, defaultSettings.DocxImageDpi);
        WriteJpegQualityInput(hwpxPaperTemplate, defaultSettings.HwpxJpegQuality);
        WriteJpegQualityInput(docxPaperTemplate, defaultSettings.DocxJpegQuality);
        foreach (var (count, inputs) in pageInputs)
        {
            WritePageInputs(inputs, defaultSettings.SettingsFor(count));
        }
    }

    private static void WritePaperTemplateInputs(PaperTemplateInputSet inputs, PaperTemplateSettings settings)
    {
        inputs.Title.Text = settings.Title;
        inputs.TitleFontPt.Text = FormatTemplateNumber(settings.TitleFontPt);
        inputs.Subtitle.Text = settings.Subtitle;
        inputs.SubtitleFontPt.Text = FormatTemplateNumber(settings.SubtitleFontPt);
        inputs.Company.Text = settings.Company;
        inputs.CompanyFontPt.Text = FormatTemplateNumber(settings.CompanyFontPt);
        inputs.BodyTitle.Text = settings.BodyTitle;
        inputs.BodyTitleFontPt.Text = FormatTemplateNumber(settings.BodyTitleFontPt);
        inputs.BodySubtitle.Text = settings.BodySubtitle;
        inputs.BodySubtitleFontPt.Text = FormatTemplateNumber(settings.BodySubtitleFontPt);
        inputs.LineSpacingPercent.Text = settings.LineSpacingPercent.ToString();
        inputs.FontFamily.Text = settings.FontFamily;
        inputs.ShowPageNumber.IsChecked = settings.ShowPageNumber;
    }

    private static int ReadJpegQualityInput(Slider input)
    {
        return ExportSettings.NormalizeJpegQuality((int)Math.Round(input.Value));
    }

    private static int ReadImageDpiInput(Slider input)
    {
        return ExportSettings.NormalizeImageDpi((int)Math.Round(input.Value));
    }

    private static void WriteImageDpiInput(PaperTemplateInputSet inputs, int value)
    {
        var dpi = ExportSettings.NormalizeImageDpi(value);
        inputs.ImageDpi.Value = dpi;
        inputs.ImageDpiValue.Text = $"{dpi}";
    }

    private static void WriteJpegQualityInput(PaperTemplateInputSet inputs, int value)
    {
        var quality = ExportSettings.NormalizeJpegQuality(value);
        inputs.JpegQuality.Value = quality;
        inputs.JpegQualityValue.Text = $"{quality}";
    }

    private static void WritePageInputs(SettingsPageInputSet inputs, ExportPageSettings settings)
    {
        settings.Normalize();
        WriteMarginInputs(inputs.HwpxPaper, settings.Hwpx);
        WriteCellMarginInputs(inputs.HwpxPhoto, settings.HwpxPhoto!);
        WriteWorkCellInputs(inputs.HwpxWorkCell, settings.HwpxWorkCell!);
        WriteMarginInputs(inputs.DocxPaper, settings.Docx);
        WriteCellMarginInputs(inputs.DocxPhoto, settings.DocxPhoto!);
        WriteWorkCellInputs(inputs.DocxWorkCell, settings.DocxWorkCell!);
    }

    private static void WriteMarginInputs(MarginInputSet inputs, DocumentMarginSettings settings)
    {
        inputs.Top.Text = settings.TopMm.ToString();
        inputs.Bottom.Text = settings.BottomMm.ToString();
        inputs.Left.Text = settings.LeftMm.ToString();
        inputs.Right.Text = settings.RightMm.ToString();
    }

    private static void WriteCellMarginInputs(CellMarginInputSet inputs, CellMarginSettings settings)
    {
        inputs.Vertical.Text = settings.VerticalMm.ToString();
        inputs.Horizontal.Text = settings.HorizontalMm.ToString();
    }

    private static void WriteWorkCellInputs(WorkCellInputSet inputs, WorkCellSizeSettings settings)
    {
        inputs.Height.Text = settings.HeightMm.ToString();
        inputs.Width.Text = settings.WidthMm.ToString();
    }

    private static MarginInputSet SettingsInputsFor(DocumentMarginSettings margins)
    {
        return new MarginInputSet(
            SettingsTextBox(margins.TopMm),
            SettingsTextBox(margins.BottomMm),
            SettingsTextBox(margins.LeftMm),
            SettingsTextBox(margins.RightMm));
    }

    private static CellMarginInputSet SettingsCellInputsFor(CellMarginSettings margins)
    {
        return new CellMarginInputSet(
            SettingsTextBox(margins.VerticalMm),
            SettingsTextBox(margins.HorizontalMm));
    }

    private static WorkCellInputSet SettingsWorkCellInputsFor(WorkCellSizeSettings settings)
    {
        return new WorkCellInputSet(
            SettingsTextBox(settings.HeightMm),
            SettingsTextBox(settings.WidthMm));
    }

    private static void ReadMarginInputs(MarginInputSet inputs, DocumentMarginSettings margins)
    {
        margins.TopMm = ParseMarginInput(inputs.Top.Text, margins.TopMm);
        margins.BottomMm = ParseMarginInput(inputs.Bottom.Text, margins.BottomMm);
        margins.LeftMm = ParseMarginInput(inputs.Left.Text, margins.LeftMm);
        margins.RightMm = ParseMarginInput(inputs.Right.Text, margins.RightMm);
    }

    private static void ReadCellMarginInputs(CellMarginInputSet inputs, CellMarginSettings margins)
    {
        margins.VerticalMm = ParseMarginInput(inputs.Vertical.Text, margins.VerticalMm);
        margins.HorizontalMm = ParseMarginInput(inputs.Horizontal.Text, margins.HorizontalMm);
    }

    private static void ReadWorkCellInputs(WorkCellInputSet inputs, WorkCellSizeSettings settings)
    {
        settings.HeightMm = ParseMarginInput(inputs.Height.Text, settings.HeightMm);
        settings.WidthMm = ParseMarginInput(inputs.Width.Text, settings.WidthMm);
    }

    private static WorkCellSizeSettings WorkCellFromInputs(WorkCellInputSet inputs)
    {
        var settings = new WorkCellSizeSettings();
        ReadWorkCellInputs(inputs, settings);
        settings.Normalize();
        return settings;
    }

    private static TextBox SettingsTextBox(int value)
    {
        var input = new TextBox
        {
            Text = value.ToString(),
            Width = 60,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Foreground = Brush("#222"),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(0),
            Padding = new Thickness(4, 0),
            FontSize = 15,
            Watermark = "0"
        };
        input.Resources["TextControlBackground"] = Brushes.Transparent;
        input.Resources["TextControlBackgroundPointerOver"] = Brushes.Transparent;
        input.Resources["TextControlBackgroundFocused"] = Brushes.Transparent;
        input.Resources["TextControlBorderBrush"] = Brushes.Transparent;
        input.Resources["TextControlBorderBrushPointerOver"] = Brushes.Transparent;
        input.Resources["TextControlBorderBrushFocused"] = Brushes.Transparent;
        return input;
    }

    private static int ParseMarginInput(string? text, int fallback)
    {
        return int.TryParse(text, out var value) ? value : fallback;
    }

    private static Control SettingsPagePanel(int cntPerPage, SettingsPageInputSet inputs, Control preview)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 4, 0, 0),
            ColumnDefinitions = new ColumnDefinitions("690,620"),
            HorizontalAlignment = HorizontalAlignment.Center,
            ColumnSpacing = 35
        };

        AddToGrid(grid, preview, 0, 0);
        AddToGrid(grid, SettingsComparisonPanel(inputs), 1, 0);
        return grid;
    }

    private static Control SettingsComparisonPanel(SettingsPageInputSet inputs)
    {
        var panel = new StackPanel
        {
            Width = SettingsComparisonTableWidth,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 52
        };
        panel.Children.Add(SettingsMarginTable(inputs));
        panel.Children.Add(SettingsWorkCellTable(inputs));
        return panel;
    }

    private static Control SettingsMarginTable(SettingsPageInputSet inputs)
    {
        var panel = new StackPanel
        {
            Width = SettingsComparisonTableWidth,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Children.Add(new TextBlock
        {
            Text = "[종이/표/사진] 여백 조절",
            FontSize = 17,
            FontWeight = FontWeight.Normal,
            Foreground = Brushes.Black,
            Margin = new Thickness(0, 0, 0, 16)
        });

        var table = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("155,205,205"),
            RowDefinitions = new RowDefinitions("36,34,34,34,34,34,34,34,34,34,34,34,34,34,34,34,34"),
            Background = Brushes.White
        };

        AddSettingsCell(table, string.Empty, 0, 0, header: false);
        AddSettingsCell(table, "한글", 1, 0, header: false, large: true);
        AddSettingsCell(table, "MS Word", 2, 0, header: false, large: true);

        AddSettingsSectionRow(table, "용지 (A4)", 1);
        AddSettingsInputRow(table, "위쪽", "1", inputs.HwpxPaper.Top, inputs.DocxPaper.Top, 2);
        AddSettingsInputRow(table, "아래쪽", "2", inputs.HwpxPaper.Bottom, inputs.DocxPaper.Bottom, 3);
        AddSettingsInputRow(table, "왼쪽", "3", inputs.HwpxPaper.Left, inputs.DocxPaper.Left, 4);
        AddSettingsInputRow(table, "오른쪽", "4", inputs.HwpxPaper.Right, inputs.DocxPaper.Right, 5);
        AddSettingsInputRow(table, "머릿말", string.Empty, SettingsFixedTextBox(FixedHeaderFooterMarginMm), SettingsFixedTextBox(FixedHeaderFooterMarginMm), 6);
        AddSettingsInputRow(table, "꼬릿말", string.Empty, SettingsFixedTextBox(FixedHeaderFooterMarginMm), SettingsFixedTextBox(FixedHeaderFooterMarginMm), 7);
        AddSettingsSectionRow(table, "표 바깥", 8);
        AddSettingsInputRow(table, "위/아래", string.Empty, SettingsFixedTextBox(15), SettingsFixedTextBox(15), 9);
        AddSettingsInputRow(table, "좌/우", string.Empty, SettingsFixedTextBox(15), SettingsFixedTextBox(15), 10);
        AddSettingsSectionRow(table, "표 안쪽", 11);
        AddSettingsInputRow(table, "위/아래", string.Empty, SettingsFixedTextBox(0), SettingsFixedTextBox(0), 12);
        AddSettingsInputRow(table, "좌/우", string.Empty, SettingsFixedTextBox(0), SettingsFixedTextBox(0), 13);
        AddSettingsSectionRow(table, "사진 바깥", 14);
        AddSettingsInputRow(table, "위/아래", "5", inputs.HwpxPhoto.Vertical, inputs.DocxPhoto.Vertical, 15);
        AddSettingsInputRow(table, "좌/우", "6", inputs.HwpxPhoto.Horizontal, inputs.DocxPhoto.Horizontal, 16);

        panel.Children.Add(new Border
        {
            BorderBrush = Brush("#222"),
            BorderThickness = new Thickness(1),
            Child = table
        });
        return panel;
    }

    private static Control SettingsWorkCellTable(SettingsPageInputSet inputs)
    {
        var panel = new StackPanel
        {
            Width = SettingsComparisonTableWidth,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Children.Add(new TextBlock
        {
            Text = "[표] 셀 크기 조절",
            FontSize = 17,
            FontWeight = FontWeight.Normal,
            Foreground = Brushes.Black,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var table = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("155,205,205"),
            RowDefinitions = new RowDefinitions("36,34,34,34,34,34,34"),
            Background = Brushes.White
        };

        AddSettingsCell(table, string.Empty, 0, 0, header: false);
        AddSettingsCell(table, "한글", 1, 0, header: false, large: true);
        AddSettingsCell(table, "MS Word", 2, 0, header: false, large: true);
        AddSettingsSectionRow(table, "표 크기", 1);
        AddSettingsInputRow(table, "세로", string.Empty, SettingsFixedTextBox(0, "자동 계산"), SettingsFixedTextBox(0, "자동 계산"), 2, showUnit: false);
        AddSettingsInputRow(table, "가로", string.Empty, SettingsFixedTextBox(0, "자동 계산"), SettingsFixedTextBox(0, "자동 계산"), 3, showUnit: false);
        AddSettingsSectionRow(table, "\"공종\" 셀", 4);
        AddSettingsInputRow(table, "행 크기 (높이)", "1", inputs.HwpxWorkCell.Height, inputs.DocxWorkCell.Height, 5, badgeColor: "#f08a00");
        AddSettingsInputRow(table, "열 크기 (너비)", "2", inputs.HwpxWorkCell.Width, inputs.DocxWorkCell.Width, 6, badgeColor: "#f08a00");

        panel.Children.Add(new Border
        {
            BorderBrush = Brush("#222"),
            BorderThickness = new Thickness(1),
            Child = table
        });
        return panel;
    }

    private static void AddSettingsSectionRow(Grid table, string text, int row)
    {
        AddSettingsCell(table, text, 0, row, header: true);
        AddSettingsCell(table, string.Empty, 1, row, header: true);
        AddSettingsCell(table, string.Empty, 2, row, header: true);
    }

    private static void AddSettingsInputRow(Grid table, string label, string badge, TextBox hwpx, TextBox docx, int row, bool showUnit = true, string badgeColor = "#0b76d1")
    {
        AddSettingsCell(table, SettingsLabelWithBadge(label, badge, badgeColor), 0, row);
        AddSettingsCell(table, SettingsValueEditor(hwpx, showUnit), 1, row);
        AddSettingsCell(table, SettingsValueEditor(docx, showUnit), 2, row);
    }

    private static Control SettingsLabelWithBadge(string label, string badge, string badgeColor = "#0b76d1")
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (!string.IsNullOrEmpty(badge))
        {
            row.Children.Add(SettingsNumberBadge(badge, badgeColor, FontWeight.Normal, 1));
        }
        else
        {
            row.Children.Add(new Border { Width = 24 });
        }
        row.Children.Add(new TextBlock
        {
            Text = label,
            Width = 70,
            FontSize = 14,
            Foreground = Brush("#222"),
            VerticalAlignment = VerticalAlignment.Center
        });
        return row;
    }

    private static Control SettingsNumberBadge(string text, string color = "#0b76d1", FontWeight? fontWeight = null, double borderThickness = 1.5)
    {
        return new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(11),
            BorderBrush = Brush(color),
            BorderThickness = new Thickness(borderThickness),
            Background = Brushes.White,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = fontWeight ?? FontWeight.Bold,
                Foreground = Brush(color),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static Control SettingsValueEditor(TextBox input, bool showUnit = true)
    {
        var disabled = input.IsReadOnly || !input.IsHitTestVisible || !input.Focusable;
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Children.Add(input);
        if (showUnit)
        {
            row.Children.Add(new TextBlock
            {
                Text = "mm",
                FontSize = 14,
                Foreground = disabled ? Brush("#c7ced6") : Brush("#222"),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        return row;
    }

    private static void AddSettingsCell(Grid table, string text, int column, int row, bool header = false, bool large = false)
    {
        AddSettingsCell(table, new TextBlock
        {
            Text = text,
            FontSize = large ? 17 : 14,
            FontWeight = FontWeight.Normal,
            Foreground = Brush("#222"),
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        }, column, row, header);
    }

    private static void AddSettingsCell(Grid table, Control content, int column, int row, bool header = false)
    {
        var border = new Border
        {
            Background = header ? Brush("#e9eef2") : Brushes.White,
            BorderBrush = Brush("#333"),
            BorderThickness = new Thickness(0.7),
            Child = content
        };
        AddToGrid(table, border, column, row);
    }

    private static TextBlock SettingsSectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.Bold,
            Foreground = Brush("#444"),
            Margin = new Thickness(0, 2, 0, 0)
        };
    }

    private static Control SettingsInputRow(string label, TextBox input)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Children =
            {
                new TextBlock { Text = "• " + label, Width = 72, VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Black },
                input,
                new TextBlock { Text = "mm", VerticalAlignment = VerticalAlignment.Center, Foreground = Brushes.Black }
            }
        };
    }

    private static TextBox SettingsFixedTextBox(int value, string? text = null)
    {
        var textBox = SettingsTextBox(value);
        if (text is not null)
        {
            textBox.Text = text;
            textBox.Width = 92;
        }
        textBox.IsReadOnly = true;
        textBox.Focusable = false;
        textBox.IsHitTestVisible = false;
        textBox.Background = Brushes.Transparent;
        textBox.Foreground = Brush("#c7ced6");
        textBox.BorderBrush = Brushes.Transparent;
        return textBox;
    }

    private static T DockRight<T>(T control) where T : Control
    {
        DockPanel.SetDock(control, Dock.Right);
        return control;
    }

    private static Control SettingsPreview(int cntPerPage, PaperTemplateSettings template, WorkCellSizeSettings workCell)
    {
        cntPerPage = PhotoGroup.NormalizeCntPerPage(cntPerPage);
        template.Normalize();
        workCell.Normalize();
        var root = new StackPanel
        {
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var page = new Border
        {
            Width = SettingsPreviewPaperWidth,
            Height = SettingsPreviewPaperHeight,
            BorderBrush = Brush("#6ca7ef"),
            BorderThickness = new Thickness(1),
            Background = Brush("#e7f5ff"),
            Child = SettingsPreviewPage(cntPerPage, template, workCell)
        };
        root.Children.Add(page);
        return root;
    }

    private static Control SettingsPreviewPage(int cntPerPage, PaperTemplateSettings template, WorkCellSizeSettings workCell)
    {
        cntPerPage = PhotoGroup.NormalizeCntPerPage(cntPerPage);
        var canvas = new Canvas();
        const double pageWidth = SettingsPreviewPaperWidth;
        const double pageHeight = SettingsPreviewPaperHeight;
        const double tableLeft = 45;
        const double tableTop = 30;
        const double tableWidth = 332;
        const double tableHeight = 535;
        var labelWidth = Math.Clamp(workCell.WidthMm * 3.0, 30, 92);
        const double titleHeight = 68;
        var bottomTitleHeight = Math.Clamp(workCell.HeightMm * 2.0, 20, 80);
        const double titleLineHeight = 39;

        var table = new Border
        {
            Width = tableWidth,
            Height = tableHeight,
            BorderBrush = Brush("#222"),
            BorderThickness = new Thickness(1.4),
            Background = Brushes.White
        };
        Canvas.SetLeft(table, tableLeft);
        Canvas.SetTop(table, tableTop);
        canvas.Children.Add(table);

        canvas.Children.Add(PreviewText(
            template.BodyTitle,
            tableLeft,
            tableTop + 10,
            tableWidth,
            26,
            center: true,
            fontSize: Math.Max(11, template.BodyTitleFontPt * 0.95),
            bold: true,
            fontFamily: template.FontFamily));
        canvas.Children.Add(PreviewText(
            template.BodySubtitle,
            tableLeft + 2,
            tableTop + 48,
            tableWidth - 4,
            20,
            center: false,
            fontSize: Math.Max(9, template.BodySubtitleFontPt * 0.76),
            bold: true,
            fontFamily: template.FontFamily));
        canvas.Children.Add(PreviewLine(tableLeft, tableTop + titleHeight, tableWidth, horizontal: true));
        canvas.Children.Add(PreviewLine(tableLeft + labelWidth, tableTop + titleHeight, tableHeight - titleHeight, horizontal: false));

        var rowHeight = (tableHeight - titleHeight - bottomTitleHeight) / cntPerPage;
        for (var i = 1; i < cntPerPage; i++)
        {
            canvas.Children.Add(PreviewLine(tableLeft, tableTop + titleHeight + rowHeight * i, tableWidth, horizontal: true));
        }
        canvas.Children.Add(PreviewLine(tableLeft, tableTop + titleHeight + rowHeight * cntPerPage, tableWidth, horizontal: true));
        canvas.Children.Add(PreviewLine(tableLeft + labelWidth, tableTop + titleHeight + rowHeight * cntPerPage, bottomTitleHeight, horizontal: false));

        for (var i = 0; i < cntPerPage; i++)
        {
            var y = tableTop + titleHeight + rowHeight * i;
            var imageWidth = tableWidth - labelWidth - 112;
            var preferredImageHeight = cntPerPage switch
            {
                <= 3 => 112,
                4 => 84,
                _ => Math.Max(8, rowHeight - 10)
            };
            var imageHeight = Math.Max(8, Math.Min(Math.Max(8, rowHeight - 8), preferredImageHeight));
            var imageLeft = tableLeft + labelWidth + 52;
            var imageTop = y + Math.Max(4, (rowHeight - imageHeight) / 2);
            canvas.Children.Add(PreviewText(SettingsPreviewPhaseLabel(i, cntPerPage), tableLeft + 14, y + rowHeight / 2 - 9, labelWidth - 9, 18, center: false, fontSize: 12, bold: true));

            var image = new Border
            {
                Width = imageWidth,
                Height = imageHeight,
                Background = Brush("#e8edf2"),
                BorderBrush = Brush("#777"),
                BorderThickness = new Thickness(1)
            };
            Canvas.SetLeft(image, imageLeft);
            Canvas.SetTop(image, imageTop);
            canvas.Children.Add(image);
            canvas.Children.Add(PreviewText("사진", imageLeft, imageTop + imageHeight / 2 - 8, imageWidth, 16, center: true, blue: true, fontSize: 12, bold: true));

            if (i == Math.Min(1, cntPerPage - 1) && rowHeight >= 60)
            {
                var centerX = tableLeft + tableWidth / 2;
                var topGuideTop = y + 8;
                var bottomGuideTop = imageTop + imageHeight + 8;
                canvas.Children.Add(PreviewVerticalGuideLine(centerX, y, imageTop, topGuideTop));
                canvas.Children.Add(PreviewVerticalGuideLine(centerX, imageTop + imageHeight, y + rowHeight, bottomGuideTop));
                canvas.Children.Add(PreviewBadge("5", centerX - 10, topGuideTop));
                canvas.Children.Add(PreviewBadge("6", imageLeft - 34, imageTop + imageHeight / 2 - 10));
                canvas.Children.Add(PreviewLine(tableLeft + labelWidth, imageTop + imageHeight / 2, imageLeft - tableLeft - labelWidth, horizontal: true, blue: true));
                canvas.Children.Add(PreviewLine(imageLeft + imageWidth, imageTop + imageHeight / 2, tableLeft + tableWidth - imageLeft - imageWidth, horizontal: true, blue: true));
            }
        }

        var bottomTop = tableTop + titleHeight + rowHeight * cntPerPage;
        canvas.Children.Add(PreviewText("공종", tableLeft + 12, bottomTop + 6, labelWidth - 12, 16, center: false, fontSize: 10, bold: true));
        canvas.Children.Add(PreviewText("공종 제목", tableLeft + labelWidth, bottomTop + 6, tableWidth - labelWidth, 16, center: true, fontSize: 10, bold: false));

        canvas.Children.Add(PreviewBadge("1", pageWidth / 2 - 10, tableTop - 16));
        canvas.Children.Add(PreviewBadge("2", pageWidth / 2 - 10, tableTop + tableHeight + 4));
        canvas.Children.Add(PreviewBadge("3", tableLeft - 34, tableTop + titleLineHeight));
        canvas.Children.Add(PreviewBadge("4", tableLeft + tableWidth + 22, tableTop + titleLineHeight));
        canvas.Children.Add(PreviewBadge("1", tableLeft - 26, bottomTop + bottomTitleHeight / 2 - 10, "#f08a00"));
        canvas.Children.Add(PreviewBadge("2", tableLeft + labelWidth / 2 - 10, tableTop + tableHeight + 19, "#f08a00"));
        canvas.Children.Add(PreviewText("아래", pageWidth / 2 - 36, tableTop + tableHeight + 7, 32, 14, center: true, blue: true, fontSize: 11, bold: true));
        canvas.Children.Add(PreviewLine(pageWidth / 2, 0, tableTop, horizontal: false, blue: true));
        canvas.Children.Add(PreviewLine(0, tableTop + titleLineHeight, tableLeft, horizontal: true, blue: true));
        canvas.Children.Add(PreviewLine(tableLeft + tableWidth, tableTop + titleLineHeight, pageWidth - tableLeft - tableWidth, horizontal: true, blue: true));
        canvas.Children.Add(PreviewLine(pageWidth / 2, tableTop + tableHeight, pageHeight - tableTop - tableHeight, horizontal: false, blue: true));
        canvas.Children.Add(PreviewLine(tableLeft - 26, bottomTop + bottomTitleHeight / 2, 26, horizontal: true, orange: true));
        canvas.Children.Add(PreviewLine(tableLeft, tableTop + tableHeight, 32, horizontal: false, orange: true));
        canvas.Children.Add(PreviewLine(tableLeft + labelWidth, tableTop + tableHeight, 32, horizontal: false, orange: true));
        canvas.Children.Add(PreviewLine(tableLeft, tableTop + tableHeight + 27, labelWidth, horizontal: true, orange: true));
        return canvas;
    }

    private static string SettingsPreviewPhaseLabel(int index, int count)
    {
        if (count == 1 || index == 0)
        {
            return "전";
        }

        if (index == count - 1)
        {
            return "후";
        }

        return count == 3 ? "중" : $"중{index}";
    }

    private static Control PreviewText(string text, double left, double top, double width, double height, bool center, bool blue = false, double fontSize = 11, bool bold = false, string? fontFamily = null)
    {
        var block = new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = bold ? FontWeight.Bold : FontWeight.Normal,
            FontFamily = string.IsNullOrWhiteSpace(fontFamily) ? FontFamily.Default : new FontFamily(fontFamily.Trim()),
            Width = width,
            Height = height,
            TextAlignment = center ? TextAlignment.Center : TextAlignment.Left,
            Foreground = blue ? Brush("#0b76d1") : Brushes.Black
        };
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, top);
        return block;
    }

    private static Control PreviewBadge(string text, double left, double top, string color = "#0b76d1")
    {
        var badge = SettingsNumberBadge(text, color);
        Canvas.SetLeft(badge, left);
        Canvas.SetTop(badge, top);
        return badge;
    }

    private static Control PreviewGuide(string text, double left, double top)
    {
        var block = new Border
        {
            Background = Brushes.White,
            Padding = new Thickness(2, 0),
            Child = new TextBlock
            {
                Text = text,
                Foreground = Brush("#2f7fd6"),
                FontSize = 11
            }
        };
        Canvas.SetLeft(block, left);
        Canvas.SetTop(block, top);
        return block;
    }

    private static void AddPreviewVerticalLineSegment(Canvas canvas, double left, double top, double bottom)
    {
        var length = bottom - top;
        if (length <= 0)
        {
            return;
        }

        canvas.Children.Add(PreviewLine(left, top, length, horizontal: false, blue: true));
    }

    private static Control PreviewVerticalGuideLine(double left, double top, double bottom, double labelTop)
    {
        var canvas = new Canvas
        {
            Width = 1,
            Height = Math.Max(0, bottom - top)
        };
        AddPreviewVerticalLineSegment(canvas, 0, 0, labelTop - top - 3);
        AddPreviewVerticalLineSegment(canvas, 0, labelTop - top + 17, bottom - top);
        Canvas.SetLeft(canvas, left);
        Canvas.SetTop(canvas, top);
        return canvas;
    }

    private static Control PreviewLine(double left, double top, double length, bool horizontal, bool blue = false, bool orange = false)
    {
        var line = new Border
        {
            Width = horizontal ? length : 1,
            Height = horizontal ? 1 : length,
            Background = orange ? Brush("#f08a00") : (blue ? Brush("#6ca7ef") : Brush("#777"))
        };
        Canvas.SetLeft(line, left);
        Canvas.SetTop(line, top);
        return line;
    }
}
