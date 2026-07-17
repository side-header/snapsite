using NewGreen.Domain;

namespace NewGreen.Infrastructure.Export;

public sealed partial class DocumentExporter
{
    private sealed record DocxTableLayout(int TableWidth, int LabelWidth, int ImageWidth, int TitleHeight, int RowHeight);

    private sealed record HwpxTableLayout(int TableWidth, int LabelWidth, int ImageWidth, int TitleHeight, int RowHeight);

    private sealed record DocumentImageSize(int Width, int Height);

    private sealed record ImagePixelSize(int Width, int Height)
    {
        public static ImagePixelSize FromDocxTwips(int widthTwips, int heightTwips, int imageDpi)
        {
            return new ImagePixelSize(
                Math.Max(1, (int)Math.Round(widthTwips / 1440.0 * imageDpi)),
                Math.Max(1, (int)Math.Round(heightTwips / 1440.0 * imageDpi)));
        }

        public static ImagePixelSize FromHwpxUnits(int widthUnits, int heightUnits, int imageDpi)
        {
            return new ImagePixelSize(
                Math.Max(1, (int)Math.Round(widthUnits / HwpxUnitsPerMillimeter / 25.4 * imageDpi)),
                Math.Max(1, (int)Math.Round(heightUnits / HwpxUnitsPerMillimeter / 25.4 * imageDpi)));
        }
    }

    private sealed record ExportImage(
        string RelativePath,
        string AbsolutePath,
        string Name,
        string DocxRelationshipId,
        string DocxTarget,
        string HwpxId,
        string HwpxTarget,
        string GroupId,
        string GroupTitle,
        int CellIndex);

    private sealed record ExportPage(string GroupTitle, int CntPerPage, List<ExportPageItem> Items, bool IsBlankPage = false);

    private sealed record ExportPageItem(string Label, ExportImage? Image);

    private sealed record HwpxCell(int Col, int ColSpan, int Width, string Content, int MarginLeft, int MarginRight, int MarginTop, int MarginBottom)
    {
        public bool HasMargin => MarginLeft != 0 || MarginRight != 0 || MarginTop != 0 || MarginBottom != 0;

        public static HwpxCell NoMargin(int col, int colSpan, int width, string content)
        {
            return new HwpxCell(col, colSpan, width, content, 0, 0, 0, 0);
        }
    }

    private sealed record ExportLayout(
        int TopTwips,
        int BottomTwips,
        int LeftTwips,
        int RightTwips,
        int TopHwpx,
        int BottomHwpx,
        int LeftHwpx,
        int RightHwpx)
    {
        public static ExportLayout From(DocumentMarginSettings settings)
        {
            settings ??= new DocumentMarginSettings();
            settings.Normalize();
            return new ExportLayout(
                DocumentExporter.MmToDocxTwips(settings.TopMm),
                DocumentExporter.MmToDocxTwips(settings.BottomMm),
                DocumentExporter.MmToDocxTwips(settings.LeftMm),
                DocumentExporter.MmToDocxTwips(settings.RightMm),
                DocumentExporter.MmToHwpxUnits(settings.TopMm),
                DocumentExporter.MmToHwpxUnits(settings.BottomMm),
                DocumentExporter.MmToHwpxUnits(settings.LeftMm),
                DocumentExporter.MmToHwpxUnits(settings.RightMm));
        }
    }

    private sealed record CellLayout(
        int VerticalTwips,
        int HorizontalTwips,
        int VerticalHwpx,
        int HorizontalHwpx,
        int MarginLeftTwips,
        int MarginRightTwips,
        int MarginTopTwips,
        int MarginBottomTwips,
        int MarginLeftHwpx,
        int MarginRightHwpx,
        int MarginTopHwpx,
        int MarginBottomHwpx)
    {
        public static CellLayout From(CellMarginSettings settings)
        {
            settings ??= new CellMarginSettings();
            settings.Normalize();
            return new CellLayout(
                DocumentExporter.MmToDocxTwips(settings.VerticalMm),
                DocumentExporter.MmToDocxTwips(settings.HorizontalMm),
                DocumentExporter.MmToHwpxUnits(settings.VerticalMm),
                DocumentExporter.MmToHwpxUnits(settings.HorizontalMm),
                DocumentExporter.MmToDocxTwips(settings.HorizontalMm),
                DocumentExporter.MmToDocxTwips(settings.HorizontalMm),
                DocumentExporter.MmToDocxTwips(settings.VerticalMm),
                DocumentExporter.MmToDocxTwips(settings.VerticalMm),
                DocumentExporter.MmToHwpxUnits(settings.HorizontalMm),
                DocumentExporter.MmToHwpxUnits(settings.HorizontalMm),
                DocumentExporter.MmToHwpxUnits(settings.VerticalMm),
                DocumentExporter.MmToHwpxUnits(settings.VerticalMm));
        }
    }
}
