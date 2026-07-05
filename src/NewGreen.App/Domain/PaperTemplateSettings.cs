using System.Text.Json.Serialization;

namespace NewGreen.Domain;

public sealed class PaperTemplateSettings
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "일  장  제  목";

    [JsonPropertyName("titleFontPt")]
    public double TitleFontPt { get; set; } = 37;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = "공사명 : 2000년 은행나무 가로수 교체공사(대한로)";

    [JsonPropertyName("subtitleFontPt")]
    public double SubtitleFontPt { get; set; } = 17;

    [JsonPropertyName("company")]
    public string Company { get; set; } = "사 이 트 스 냅 대 표  홍 길 동";

    [JsonPropertyName("companyFontPt")]
    public double CompanyFontPt { get; set; } = 22;

    [JsonPropertyName("bodyTitle")]
    public string BodyTitle { get; set; } = "이 장 제 목";

    [JsonPropertyName("bodyTitleFontPt")]
    public double BodyTitleFontPt { get; set; } = 23;

    [JsonPropertyName("bodySubtitle")]
    public string BodySubtitle { get; set; } = "공사명 : 2000년 은행나무 가로수 교체공사(대한로)";

    [JsonPropertyName("bodySubtitleFontPt")]
    public double BodySubtitleFontPt { get; set; } = 14;

    [JsonPropertyName("lineSpacingPercent")]
    public int LineSpacingPercent { get; set; } = 160;

    [JsonPropertyName("fontFamily")]
    public string FontFamily { get; set; } = "함초롬바탕";

    [JsonPropertyName("showPageNumber")]
    public bool ShowPageNumber { get; set; }

    public static PaperTemplateSettings DocxDefault()
    {
        return new PaperTemplateSettings
        {
            LineSpacingPercent = 80
        };
    }

    public PaperTemplateSettings Clone()
    {
        return new PaperTemplateSettings
        {
            Title = Title,
            TitleFontPt = TitleFontPt,
            Subtitle = Subtitle,
            SubtitleFontPt = SubtitleFontPt,
            Company = Company,
            CompanyFontPt = CompanyFontPt,
            BodyTitle = BodyTitle,
            BodyTitleFontPt = BodyTitleFontPt,
            BodySubtitle = BodySubtitle,
            BodySubtitleFontPt = BodySubtitleFontPt,
            LineSpacingPercent = LineSpacingPercent,
            FontFamily = FontFamily,
            ShowPageNumber = ShowPageNumber
        };
    }

    public void Normalize()
    {
        Title ??= string.Empty;
        Subtitle ??= string.Empty;
        Company ??= string.Empty;
        FontFamily = string.IsNullOrWhiteSpace(FontFamily) ? "함초롬바탕" : FontFamily.Trim();
        if (string.IsNullOrWhiteSpace(BodyTitle))
        {
            BodyTitle = Title;
        }
        if (string.IsNullOrWhiteSpace(BodySubtitle))
        {
            BodySubtitle = Subtitle;
        }

        TitleFontPt = Clamp(TitleFontPt, 8, 72, 37);
        SubtitleFontPt = Clamp(SubtitleFontPt, 8, 48, 17);
        CompanyFontPt = Clamp(CompanyFontPt, 8, 48, 22);
        BodyTitleFontPt = Clamp(BodyTitleFontPt, 8, 48, 23);
        BodySubtitleFontPt = Clamp(BodySubtitleFontPt, 8, 36, 14);
        LineSpacingPercent = Clamp(LineSpacingPercent, 80, 300, 160);
    }

    private static int Clamp(int value, int min, int max, int fallback)
    {
        if (value <= 0)
        {
            value = fallback;
        }

        return Math.Clamp(value, min, max);
    }

    private static double Clamp(double value, double min, double max, double fallback)
    {
        if (value <= 0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            value = fallback;
        }

        return Math.Clamp(value, min, max);
    }
}

public sealed class PaperTemplateFormatSettings
{
    [JsonPropertyName("hwpx")]
    public PaperTemplateSettings? Hwpx { get; set; } = new();

    [JsonPropertyName("docx")]
    public PaperTemplateSettings? Docx { get; set; } = PaperTemplateSettings.DocxDefault();

    public void Normalize(PaperTemplateSettings? legacyTemplate = null)
    {
        legacyTemplate?.Normalize();
        Hwpx ??= legacyTemplate?.Clone() ?? new PaperTemplateSettings();
        Docx ??= legacyTemplate?.Clone() ?? PaperTemplateSettings.DocxDefault();
        Hwpx.Normalize();
        Docx.Normalize();
    }
}
