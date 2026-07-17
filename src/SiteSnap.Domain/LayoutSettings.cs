using System.Text.Json.Serialization;

namespace SiteSnap.Domain;

public sealed class WorkCellSizeSettings
{
    [JsonPropertyName("heightMm")]
    public int HeightMm { get; set; } = 18;

    [JsonPropertyName("widthMm")]
    public int WidthMm { get; set; } = 22;

    public void Normalize()
    {
        HeightMm = Math.Clamp(HeightMm, 5, 80);
        WidthMm = Math.Clamp(WidthMm, 5, 80);
    }

    public WorkCellSizeSettings Clone()
    {
        return new WorkCellSizeSettings
        {
            HeightMm = HeightMm,
            WidthMm = WidthMm
        };
    }
}

public sealed class CellMarginSettings
{
    [JsonPropertyName("verticalMm")]
    public int VerticalMm { get; set; } = 6;

    [JsonPropertyName("horizontalMm")]
    public int HorizontalMm { get; set; } = 18;

    public void Normalize()
    {
        VerticalMm = Math.Clamp(VerticalMm, 0, 100);
        HorizontalMm = Math.Clamp(HorizontalMm, 0, 100);
    }

    public CellMarginSettings Clone()
    {
        return new CellMarginSettings
        {
            VerticalMm = VerticalMm,
            HorizontalMm = HorizontalMm
        };
    }
}

public sealed class DocumentMarginSettings
{
    [JsonPropertyName("topMm")]
    public int TopMm { get; set; } = ExportSettings.DefaultMarginMm;

    [JsonPropertyName("bottomMm")]
    public int BottomMm { get; set; } = ExportSettings.DefaultMarginMm;

    [JsonPropertyName("leftMm")]
    public int LeftMm { get; set; } = ExportSettings.DefaultMarginMm;

    [JsonPropertyName("rightMm")]
    public int RightMm { get; set; } = ExportSettings.DefaultMarginMm;

    public void Normalize()
    {
        TopMm = Math.Clamp(TopMm, 0, 100);
        BottomMm = Math.Clamp(BottomMm, 0, 100);
        LeftMm = Math.Clamp(LeftMm, 0, 100);
        RightMm = Math.Clamp(RightMm, 0, 100);
    }

    public DocumentMarginSettings Clone()
    {
        return new DocumentMarginSettings
        {
            TopMm = TopMm,
            BottomMm = BottomMm,
            LeftMm = LeftMm,
            RightMm = RightMm
        };
    }
}
