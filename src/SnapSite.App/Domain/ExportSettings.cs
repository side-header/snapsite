using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewGreen.Domain;

public sealed class ExportSettings
{
    public const int DefaultMarginMm = 20;
    public const int DefaultJpegQuality = 85;
    public const int DefaultImageDpi = 300;
    public const int MinImageDpi = 72;
    public const int MaxImageDpi = 600;

    [JsonPropertyName("hwpxImageDpi")]
    public int HwpxImageDpi { get; set; } = DefaultImageDpi;

    [JsonPropertyName("docxImageDpi")]
    public int DocxImageDpi { get; set; } = DefaultImageDpi;

    [JsonPropertyName("hwpxJpegQuality")]
    public int HwpxJpegQuality { get; set; } = DefaultJpegQuality;

    [JsonPropertyName("docxJpegQuality")]
    public int DocxJpegQuality { get; set; } = DefaultJpegQuality;

    [JsonPropertyName("page3")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExportPageSettings? Page3 { get; set; }

    [JsonPropertyName("page4")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ExportPageSettings? Page4 { get; set; }

    [JsonPropertyName("pages")]
    public Dictionary<int, ExportPageSettings>? Pages { get; set; }

    [JsonPropertyName("hwpx")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentMarginSettings? Hwpx { get; set; }

    [JsonPropertyName("docx")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentMarginSettings? Docx { get; set; }

    [JsonPropertyName("hwpxCell")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CellMarginSettings? HwpxCell { get; set; }

    [JsonPropertyName("docxCell")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CellMarginSettings? DocxCell { get; set; }

    [JsonPropertyName("hwpxPhoto")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CellMarginSettings? HwpxPhoto { get; set; }

    [JsonPropertyName("docxPhoto")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CellMarginSettings? DocxPhoto { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    [JsonPropertyName("margin1Mm")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyMargin1Mm { get; set; }

    [JsonPropertyName("margin2Mm")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyMargin2Mm { get; set; }

    [JsonPropertyName("margin3Mm")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyMargin3Mm { get; set; }

    [JsonPropertyName("margin1Px")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyMargin1Px { get; set; }

    [JsonPropertyName("margin2Px")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyMargin2Px { get; set; }

    [JsonPropertyName("margin3Px")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyMargin3Px { get; set; }

    public void Normalize()
    {
        var legacySettings = LegacySettings();

        MigrateLegacyCellSettings();

        HwpxCell ??= new CellMarginSettings
        {
            VerticalMm = legacySettings.Hwpx.TopMm,
            HorizontalMm = legacySettings.Hwpx.LeftMm
        };
        DocxCell ??= new CellMarginSettings
        {
            VerticalMm = legacySettings.Docx.TopMm,
            HorizontalMm = legacySettings.Docx.LeftMm
        };
        HwpxPhoto ??= new CellMarginSettings();
        DocxPhoto ??= new CellMarginSettings();

        if (LegacyMargin1Mm.HasValue || LegacyMargin2Mm.HasValue || LegacyMargin3Mm.HasValue)
        {
            legacySettings.ApplyMargins(
                LegacyMargin1Mm ?? DefaultMarginMm,
                LegacyMargin2Mm ?? DefaultMarginMm,
                LegacyMargin3Mm ?? DefaultMarginMm);
            LegacyMargin1Mm = null;
            LegacyMargin2Mm = null;
            LegacyMargin3Mm = null;
        }

        if (LegacyMargin1Px.HasValue || LegacyMargin2Px.HasValue || LegacyMargin3Px.HasValue)
        {
            legacySettings.ApplyMargins(
                LegacyMargin1Px.HasValue ? PxToMm(LegacyMargin1Px.Value) : DefaultMarginMm,
                LegacyMargin2Px.HasValue ? PxToMm(LegacyMargin2Px.Value) : DefaultMarginMm,
                LegacyMargin3Px.HasValue ? PxToMm(LegacyMargin3Px.Value) : DefaultMarginMm);
            LegacyMargin1Px = null;
            LegacyMargin2Px = null;
            LegacyMargin3Px = null;
        }

        legacySettings.HwpxCell ??= HwpxCell;
        legacySettings.DocxCell ??= DocxCell;
        legacySettings.HwpxPhoto ??= HwpxPhoto;
        legacySettings.DocxPhoto ??= DocxPhoto;
        legacySettings.Normalize();

        Pages ??= [];
        var page3Source = Pages.TryGetValue(3, out var existingPage3) && existingPage3 is not null
            ? existingPage3
            : Page3 ?? legacySettings;
        var page4Source = Pages.TryGetValue(4, out var existingPage4) && existingPage4 is not null
            ? existingPage4
            : Page4 ?? legacySettings;
        foreach (var count in Enumerable.Range(
                     PhotoGroup.MinCntPerPage,
                     PhotoGroup.MaxCntPerPage - PhotoGroup.MinCntPerPage + 1))
        {
            if (!Pages.TryGetValue(count, out var pageSettings) || pageSettings is null)
            {
                Pages[count] = (count <= 3 ? page3Source : page4Source).Clone();
            }
            Pages[count].Normalize();
        }
        foreach (var count in Pages.Keys
                     .Where(count => count < PhotoGroup.MinCntPerPage || count > PhotoGroup.MaxCntPerPage)
                     .ToList())
        {
            Pages.Remove(count);
        }
        HwpxImageDpi = NormalizeImageDpi(HwpxImageDpi);
        DocxImageDpi = NormalizeImageDpi(DocxImageDpi);
        HwpxJpegQuality = NormalizeJpegQuality(HwpxJpegQuality);
        DocxJpegQuality = NormalizeJpegQuality(DocxJpegQuality);

        Hwpx = null;
        Docx = null;
        HwpxCell = null;
        DocxCell = null;
        HwpxPhoto = null;
        DocxPhoto = null;
        Page3 = null;
        Page4 = null;
        ExtensionData = null;
    }

    public ExportPageSettings SettingsFor(int cntPerPage)
    {
        Normalize();
        return Pages![PhotoGroup.NormalizeCntPerPage(cntPerPage)];
    }

    private ExportPageSettings LegacySettings()
    {
        return new ExportPageSettings
        {
            Hwpx = Hwpx?.Clone() ?? new DocumentMarginSettings(),
            Docx = Docx?.Clone() ?? new DocumentMarginSettings(),
            HwpxCell = HwpxCell?.Clone(),
            DocxCell = DocxCell?.Clone(),
            HwpxPhoto = HwpxPhoto?.Clone(),
            DocxPhoto = DocxPhoto?.Clone()
        };
    }

    private static int PxToMm(int px)
    {
        return (int)Math.Round(px * 25.4 / 96.0);
    }

    public static int NormalizeJpegQuality(int quality)
    {
        return Math.Clamp(quality, 0, 100);
    }

    public static int NormalizeImageDpi(int dpi)
    {
        return Math.Clamp(dpi, MinImageDpi, MaxImageDpi);
    }

    private void MigrateLegacyCellSettings()
    {
        if (ExtensionData is null)
        {
            return;
        }

        if (HwpxPhoto is null && TryReadLegacyCellSettings("hwpxPhoto", out var hwpxPhoto))
        {
            HwpxPhoto = hwpxPhoto;
        }

        if (DocxPhoto is null && TryReadLegacyCellSettings("docxPhoto", out var docxPhoto))
        {
            DocxPhoto = docxPhoto;
        }
    }

    private bool TryReadLegacyCellSettings(string key, out CellMarginSettings settings)
    {
        settings = new CellMarginSettings();
        if (ExtensionData is null || !ExtensionData.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty("verticalMm", out var vertical) && vertical.TryGetInt32(out var verticalMm))
        {
            settings.VerticalMm = verticalMm;
        }

        if (element.TryGetProperty("horizontalMm", out var horizontal) && horizontal.TryGetInt32(out var horizontalMm))
        {
            settings.HorizontalMm = horizontalMm;
        }

        return true;
    }
}
