using System.Text.Json.Serialization;

namespace NewGreen.Domain;

public sealed class ExportPageSettings
{
    [JsonPropertyName("hwpx")]
    public DocumentMarginSettings Hwpx { get; set; } = new();

    [JsonPropertyName("docx")]
    public DocumentMarginSettings Docx { get; set; } = new();

    [JsonPropertyName("hwpxCell")]
    public CellMarginSettings? HwpxCell { get; set; }

    [JsonPropertyName("docxCell")]
    public CellMarginSettings? DocxCell { get; set; }

    [JsonPropertyName("hwpxPhoto")]
    public CellMarginSettings? HwpxPhoto { get; set; }

    [JsonPropertyName("docxPhoto")]
    public CellMarginSettings? DocxPhoto { get; set; }

    [JsonPropertyName("hwpxWorkCell")]
    public WorkCellSizeSettings? HwpxWorkCell { get; set; }

    [JsonPropertyName("docxWorkCell")]
    public WorkCellSizeSettings? DocxWorkCell { get; set; }

    public void Normalize()
    {
        Hwpx ??= new DocumentMarginSettings();
        Docx ??= new DocumentMarginSettings();
        HwpxCell ??= new CellMarginSettings
        {
            VerticalMm = Hwpx.TopMm,
            HorizontalMm = Hwpx.LeftMm
        };
        DocxCell ??= new CellMarginSettings
        {
            VerticalMm = Docx.TopMm,
            HorizontalMm = Docx.LeftMm
        };
        HwpxPhoto ??= new CellMarginSettings();
        DocxPhoto ??= new CellMarginSettings();
        HwpxWorkCell ??= new WorkCellSizeSettings();
        DocxWorkCell ??= new WorkCellSizeSettings();

        Hwpx.Normalize();
        Docx.Normalize();
        HwpxCell.VerticalMm = 0;
        HwpxCell.HorizontalMm = 0;
        DocxCell.VerticalMm = 0;
        DocxCell.HorizontalMm = 0;
        HwpxCell.Normalize();
        DocxCell.Normalize();
        HwpxPhoto.Normalize();
        DocxPhoto.Normalize();
        HwpxWorkCell.Normalize();
        DocxWorkCell.Normalize();
    }

    public void ApplyMargins(int top, int leftRight, int bottom)
    {
        Hwpx.TopMm = top;
        Hwpx.BottomMm = bottom;
        Hwpx.LeftMm = leftRight;
        Hwpx.RightMm = leftRight;
        Docx.TopMm = top;
        Docx.BottomMm = bottom;
        Docx.LeftMm = leftRight;
        Docx.RightMm = leftRight;
    }

    public ExportPageSettings Clone()
    {
        return new ExportPageSettings
        {
            Hwpx = Hwpx.Clone(),
            Docx = Docx.Clone(),
            HwpxCell = HwpxCell?.Clone(),
            DocxCell = DocxCell?.Clone(),
            HwpxPhoto = HwpxPhoto?.Clone(),
            DocxPhoto = DocxPhoto?.Clone(),
            HwpxWorkCell = HwpxWorkCell?.Clone(),
            DocxWorkCell = DocxWorkCell?.Clone()
        };
    }
}
