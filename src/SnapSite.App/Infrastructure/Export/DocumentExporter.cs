using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using NewGreen.Domain;
using NewGreen.Infrastructure.FileSystem;
using SkiaSharp;

namespace NewGreen.Infrastructure.Export;

public sealed partial class DocumentExporter
{
    private const string ApplicationName = "SiteSnap";
    private const string ExportDocumentTitle = "SiteSnap Export";
    private const string BaseHwpxResourceSuffix = ".Resources.base.hwpx";
    private const string HwpxTableBorderFillId = "3";
    private const int HwpxOuterTableBorderFillBaseId = 3;
    private const int HwpxTableColumnCount = 2;
    private const string HwpxCenterParaPrId = "1";
    private const string HwpxZeroParaPrId = "20";
    private const string HwpxBoldCharPrId = "2";
    private const string HwpxTemplateTitleParaPrId = "21";
    private const string HwpxTemplateLeftParaPrId = "22";
    private const string HwpxTemplateTitleCharPrId = "7";
    private const string HwpxTemplateSubtitleCharPrId = "8";
    private const string HwpxTablePhaseCharPrId = "9";
    private const string HwpxTableWorkTypeCharPrId = "10";
    private const string HwpxCoverTitleCharPrId = "11";
    private const string HwpxCoverSubtitleCharPrId = "12";
    private const string HwpxCoverCompanyCharPrId = "13";
    private const string HwpxTableWorkTitleCharPrId = "14";
    private const string DefaultTemplateFontFace = "함초롬바탕";
    private const int DocxPageWidth = 11906;
    private const int DocxPageHeight = 16838;
    private const int DocxHeaderTwips = 0;
    private const int DocxFooterTwips = 0;
    private const int DocxTableVerticalSafetyTwips = 360;
    private const double DocxTwipsPerMillimeter = 1440.0 / 25.4;
    private const int HwpxPageWidth = 59528;
    private const int HwpxPageHeight = 84188;
    private const int HwpxHeaderUnits = 0;
    private const int HwpxFooterUnits = 0;
    private static readonly int HwpxTableVerticalSafetyUnits = MmToHwpxUnits(2);
    private const int HwpxFixedOuterMarginUnits = 0;
    private const int HwpxFixedPhotoOuterMarginUnits = 0;
    private const double HwpxUnitsPerMillimeter = HwpxPageWidth / 210.0;
    public void ExportAll(string rootDir, AppState state)
    {
        var outDir = Path.Combine(rootDir, "exports");
        Directory.CreateDirectory(outDir);
        var fileStem = ExportFileStem();
        ExportDocx(rootDir, Path.Combine(outDir, $"{fileStem}.docx"), state);
        ExportHwpx(rootDir, Path.Combine(outDir, $"{fileStem}.hwpx"), state);
    }

    public string ExportDocxOnly(string rootDir, AppState state)
    {
        var outDir = Path.Combine(rootDir, "exports");
        Directory.CreateDirectory(outDir);
        var outputPath = Path.Combine(outDir, $"{ExportFileStem()}.docx");
        ExportDocx(rootDir, outputPath, state);
        return outputPath;
    }

    public void ExportDocxTo(string rootDir, string outputPath, AppState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? rootDir);
        ExportDocx(rootDir, outputPath, state);
    }

    public string ExportHwpxOnly(string rootDir, AppState state)
    {
        var outDir = Path.Combine(rootDir, "exports");
        Directory.CreateDirectory(outDir);
        var outputPath = Path.Combine(outDir, $"{ExportFileStem()}.hwpx");
        ExportHwpx(rootDir, outputPath, state);
        return outputPath;
    }

    public void ExportHwpxTo(string rootDir, string outputPath, AppState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? rootDir);
        ExportHwpx(rootDir, outputPath, state);
    }

    private static string ExportFileStem()
    {
        return "sitesnap-" + DateTime.Now.ToString("yyMMddHHmmss");
    }

    private static void ExportDocx(string rootDir, string outputPath, AppState state)
    {
        state.NormalizePaperTemplates();
        var template = state.PaperTemplates!.Docx!;
        state.ExportSettings.Normalize();
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var images = CollectImages(rootDir, state).ToList();
        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
        WriteEntry(archive, "[Content_Types].xml", DocxContentTypes(images, template.ShowPageNumber));
        WriteEntry(archive, "_rels/.rels", DocxRootRels());
        WriteEntry(archive, "word/_rels/document.xml.rels", DocxDocumentRels(images, template.ShowPageNumber));
        WriteEntry(archive, "word/document.xml", DocxDocumentXml(state, images));
        WriteEntry(archive, "word/styles.xml", DocxStyles());
        if (template.ShowPageNumber)
        {
            WriteEntry(archive, "word/footer1.xml", DocxFooterXml());
        }

        var imageSizes = BuildDocxImagePixelSizes(state, images);
        var jpegQuality = ExportSettings.NormalizeJpegQuality(state.ExportSettings.DocxJpegQuality);
        foreach (var image in images)
        {
            WriteResizedJpegEntry(archive, image.DocxTarget, image, imageSizes[image], CompressionLevel.Optimal, jpegQuality);
        }
    }

    private static void ExportHwpx(string rootDir, string outputPath, AppState state)
    {
        state.NormalizePaperTemplates();
        var template = state.PaperTemplates!.Hwpx!;
        state.ExportSettings.Normalize();
        if (File.Exists(outputPath))
        {
            File.Delete(outputPath);
        }

        var images = CollectImages(rootDir, state).ToList();
        var templateBytes = ReadBaseHwpxTemplate();
        File.WriteAllBytes(outputPath, templateBytes);

        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Update);
        ReplaceEntry(archive, "Contents/content.hpf", HwpxContentHpfFromTemplate(templateBytes, images));
        ReplaceEntry(archive, "Contents/header.xml", HwpxHeaderXmlWithBlackTableBorder(templateBytes, template));
        ReplaceEntry(archive, "Contents/section0.xml", HwpxSectionXmlFromTemplate(templateBytes, state, images));
        ReplaceEntry(archive, "Preview/PrvText.txt", HwpxPreviewText(state));

        var imageSizes = BuildHwpxImagePixelSizes(state, images);
        var jpegQuality = ExportSettings.NormalizeJpegQuality(state.ExportSettings.HwpxJpegQuality);
        foreach (var image in images)
        {
            archive.GetEntry(image.HwpxTarget)?.Delete();
            WriteResizedJpegEntry(archive, image.HwpxTarget, image, imageSizes[image], CompressionLevel.NoCompression, jpegQuality);
        }
    }

    private static IEnumerable<ExportImage> CollectImages(string rootDir, AppState state)
    {
        var index = 1;
        foreach (var group in state.Groups)
        {
            foreach (var phase in new[] { Phase.Before, Phase.Processing, Phase.After })
            {
                foreach (var relativePath in group.Photos(phase))
                {
                    var normalized = AppState.NormalizePath(relativePath);
                    var absolutePath = FileScanner.ToAbsolutePath(rootDir, normalized);
                    if (!File.Exists(absolutePath))
                    {
                        throw new FileNotFoundException($"사진 파일을 찾을 수 없습니다: {normalized}", absolutePath);
                    }

                    var mediaName = $"image{index}.jpg";
                    var hwpxId = $"BIN{index:0000}";
                    var hwpxExt = ".jpg";
                    var hwpxName = $"{hwpxId}{hwpxExt}";
                    yield return new ExportImage(
                        normalized,
                        absolutePath,
                        Path.GetFileName(normalized),
                        $"rId{index + 10}",
                        $"word/media/{mediaName}",
                        hwpxId,
                        $"BinData/{hwpxName}",
                        group.Title,
                        phase);
                    index++;
                }
            }
        }
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static void WriteStoredEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static void ReplaceEntry(ZipArchive archive, string name, string content)
    {
        archive.GetEntry(name)?.Delete();
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static void WriteResizedJpegEntry(ZipArchive archive, string entryName, ExportImage image, ImagePixelSize size, CompressionLevel compressionLevel, int jpegQuality)
    {
        using var source = SKBitmap.Decode(image.AbsolutePath)
            ?? throw new InvalidOperationException($"이미지 변환에 실패했습니다: {image.RelativePath}");
        var targetWidth = Math.Max(1, size.Width);
        var targetHeight = Math.Max(1, size.Height);
        using var surface = SKSurface.Create(new SKImageInfo(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(source, new SKRect(0, 0, targetWidth, targetHeight));
        canvas.Flush();

        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Jpeg, jpegQuality)
            ?? throw new InvalidOperationException($"이미지 인코딩에 실패했습니다: {image.RelativePath}");

        var entry = archive.CreateEntry(entryName, compressionLevel);
        using var stream = entry.Open();
        encoded.SaveTo(stream);
    }

    private static byte[] ReadBaseHwpxTemplate()
    {
        var assembly = typeof(DocumentExporter).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(BaseHwpxResourceSuffix, StringComparison.Ordinal));

        if (resourceName is null)
        {
            throw new InvalidOperationException("HWPX 기본 템플릿 리소스를 찾을 수 없습니다.");
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("HWPX 기본 템플릿 리소스를 열 수 없습니다.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static string ReadTemplateEntry(byte[] templateBytes, string name)
    {
        using var memory = new MemoryStream(templateBytes, writable: false);
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);
        var entry = archive.GetEntry(name)
            ?? throw new InvalidOperationException($"HWPX 기본 템플릿에서 {name} 파일을 찾을 수 없습니다.");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string DocxContentTypes(List<ExportImage> images, bool showPageNumber)
    {
        var builder = new StringBuilder();
        builder.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">""");
        builder.Append("""<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>""");
        builder.Append("""<Default Extension="xml" ContentType="application/xml"/>""");

        foreach (var ext in images.Select(image => Path.GetExtension(image.DocxTarget).TrimStart('.').ToLowerInvariant()).Where(ext => ext.Length > 0).Distinct())
        {
            var contentType = ext == "jpg" ? "image/jpeg" : $"image/{ext}";
            builder.Append($"""<Default Extension="{Escape(ext)}" ContentType="{Escape(contentType)}"/>""");
        }

        builder.Append("""<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>""");
        builder.Append("""<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>""");
        if (showPageNumber)
        {
            builder.Append("""<Override PartName="/word/footer1.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml"/>""");
        }
        builder.Append("</Types>");
        return builder.ToString();
    }

    private static string DocxRootRels()
    {
        return """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>""";
    }

    private static string DocxDocumentRels(List<ExportImage> images, bool showPageNumber)
    {
        var builder = new StringBuilder("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">""");
        builder.Append("""<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>""");
        if (showPageNumber)
        {
            builder.Append("""<Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer" Target="footer1.xml"/>""");
        }
        foreach (var image in images)
        {
            builder.Append($"""<Relationship Id="{image.DocxRelationshipId}" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/image" Target="{Escape(image.DocxTarget["word/".Length..])}"/>""");
        }
        builder.Append("</Relationships>");
        return builder.ToString();
    }

    private static string DocxDocumentXml(AppState state, List<ExportImage> images)
    {
        var template = state.PaperTemplates!.Docx!;
        var pages = BuildExportPages(state, images).ToList();
        var builder = new StringBuilder("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        builder.Append("""<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships" xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing" xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"><w:body>""");
        builder.Append(DocxCoverPage(template));

        for (var i = 0; i < pages.Count; i++)
        {
            builder.Append(PageBreak());
            var settings = state.ExportSettings.SettingsFor(pages[i].CntPerPage);
            builder.Append(DocxTemplateHeader(template));
            builder.Append(DocxPageTable(pages[i], template, settings.Docx, settings.DocxCell!, settings.DocxPhoto!, settings.DocxWorkCell!));
        }

        var sectionSettings = state.ExportSettings.SettingsFor(pages.FirstOrDefault()?.CntPerPage ?? 3);
        builder.Append(DocxSectionPr(sectionSettings.Docx, template.ShowPageNumber));
        builder.Append("</w:body></w:document>");
        return builder.ToString();
    }

    private static IEnumerable<ExportPage> BuildExportPages(AppState state, List<ExportImage> images)
    {
        var index = images.ToDictionary(image => $"{image.GroupTitle}|{image.Phase.Key()}|{image.RelativePath}", StringComparer.OrdinalIgnoreCase);

        foreach (var group in state.Groups)
        {
            var items = new List<ExportPageItem>();
            foreach (var phase in new[] { Phase.Before, Phase.Processing, Phase.After })
            {
                var photos = group.Photos(phase);
                for (var i = 0; i < photos.Count; i++)
                {
                    var normalized = AppState.NormalizePath(photos[i]);
                    var key = $"{group.Title}|{phase.Key()}|{normalized}";
                    if (!index.TryGetValue(key, out var image))
                    {
                        continue;
                    }

                    var label = group.LabelAt(phase, i);
                    items.Add(new ExportPageItem(label, image));
                }
            }

            var limit = group.CntPerPage == 4 ? 4 : 3;
            for (var i = 0; i < items.Count; i += limit)
            {
                yield return new ExportPage(group.Title, limit, items.Skip(i).Take(limit).ToList());
            }
        }
    }

    private static Dictionary<ExportImage, ImagePixelSize> BuildDocxImagePixelSizes(AppState state, List<ExportImage> images)
    {
        var sizes = new Dictionary<ExportImage, ImagePixelSize>();
        var imageDpi = ExportSettings.NormalizeImageDpi(state.ExportSettings.DocxImageDpi);
        foreach (var page in BuildExportPages(state, images))
        {
            var settings = state.ExportSettings.SettingsFor(page.CntPerPage);
            var metrics = DocxTableMetrics(page.CntPerPage, settings.Docx, settings.DocxCell!, settings.DocxWorkCell!, state.PaperTemplates!.Docx);
            foreach (var item in page.Items)
            {
                var size = DocxImageSizeTwips(metrics.ImageWidth, metrics.RowHeight, CellLayout.From(settings.DocxPhoto!));
                sizes[item.Image] = ImagePixelSize.FromDocxTwips(size.Width, size.Height, imageDpi);
            }
        }

        foreach (var image in images)
        {
            sizes.TryAdd(image, new ImagePixelSize(1200, 800));
        }

        return sizes;
    }

    private static Dictionary<ExportImage, ImagePixelSize> BuildHwpxImagePixelSizes(AppState state, List<ExportImage> images)
    {
        var sizes = new Dictionary<ExportImage, ImagePixelSize>();
        var imageDpi = ExportSettings.NormalizeImageDpi(state.ExportSettings.HwpxImageDpi);
        foreach (var page in BuildExportPages(state, images))
        {
            var settings = state.ExportSettings.SettingsFor(page.CntPerPage);
            var metrics = HwpxTableMetrics(page.CntPerPage, settings.Hwpx, settings.HwpxCell!, settings.HwpxWorkCell!, state.PaperTemplates!.Hwpx);
            foreach (var item in page.Items)
            {
                var size = HwpxImageSize(metrics.ImageWidth, metrics.RowHeight, CellLayout.From(settings.HwpxPhoto!));
                sizes[item.Image] = ImagePixelSize.FromHwpxUnits(size.Width, size.Height, imageDpi);
            }
        }

        foreach (var image in images)
        {
            sizes.TryAdd(image, new ImagePixelSize(1200, 800));
        }

        return sizes;
    }

    private static string DocxStyles()
    {
        return """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:rPr><w:b/><w:sz w:val="40"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:rPr><w:b/><w:sz w:val="28"/></w:rPr></w:style><w:style w:type="paragraph" w:styleId="Caption"><w:name w:val="Caption"/><w:rPr><w:sz w:val="18"/></w:rPr></w:style></w:styles>""";
    }

    private static string Paragraph(string text, string style)
    {
        var styleXml = string.IsNullOrWhiteSpace(style) ? string.Empty : $"""<w:pPr><w:pStyle w:val="{Escape(style)}"/></w:pPr>""";
        return $"""<w:p>{styleXml}<w:r><w:t>{Escape(text)}</w:t></w:r></w:p>""";
    }

    private static string DocxTemplateHeader(PaperTemplateSettings? template)
    {
        template ??= new PaperTemplateSettings();
        template.Normalize();
        var title = Escape(template.BodyTitle);
        var subtitle = Escape(template.BodySubtitle);
        var titleSize = DocxHalfPoints(template.BodyTitleFontPt);
        var subtitleSize = DocxHalfPoints(template.BodySubtitleFontPt);
        var titleLine = DocxLineTwips(template.BodyTitleFontPt, template.LineSpacingPercent);
        var subtitleLine = DocxLineTwips(template.BodySubtitleFontPt, template.LineSpacingPercent);
        var fontXml = DocxRunFontXml(template.FontFamily);
        return $"""
<w:p><w:pPr><w:jc w:val="center"/><w:spacing w:before="0" w:after="0" w:line="{titleLine}" w:lineRule="exact"/></w:pPr><w:r><w:rPr>{fontXml}<w:b/><w:bCs/><w:sz w:val="{titleSize}"/></w:rPr><w:t>{title}</w:t></w:r></w:p>
<w:p><w:pPr><w:jc w:val="left"/><w:spacing w:before="0" w:after="0" w:line="{subtitleLine}" w:lineRule="exact"/></w:pPr><w:r><w:rPr>{fontXml}<w:b/><w:bCs/><w:sz w:val="{subtitleSize}"/></w:rPr><w:t>{subtitle}</w:t></w:r></w:p>
""";
    }

    private static string DocxCoverPage(PaperTemplateSettings? template)
    {
        template ??= new PaperTemplateSettings();
        template.Normalize();
        var title = Escape(template.Title);
        var subtitle = Escape(template.Subtitle);
        var company = Escape(template.Company);
        var titleSize = DocxHalfPoints(template.TitleFontPt);
        var subtitleSize = DocxHalfPoints(template.SubtitleFontPt);
        var companySize = DocxHalfPoints(template.CompanyFontPt);
        var titleLine = DocxLineTwips(template.TitleFontPt, template.LineSpacingPercent);
        var subtitleLine = DocxLineTwips(template.SubtitleFontPt, template.LineSpacingPercent);
        var companyLine = DocxLineTwips(template.CompanyFontPt, template.LineSpacingPercent);
        var titleBottomSpacer = MmToDocxTwips(18);
        var subtitleBottomSpacer = MmToDocxTwips(133);
        var fontXml = DocxRunFontXml(template.FontFamily);
        return $"""
<w:p><w:pPr><w:jc w:val="center"/><w:spacing w:before="750" w:after="{titleBottomSpacer}" w:line="{titleLine}" w:lineRule="exact"/></w:pPr><w:r><w:rPr>{fontXml}<w:b/><w:bCs/><w:sz w:val="{titleSize}"/></w:rPr><w:t>{title}</w:t></w:r></w:p>
<w:p><w:pPr><w:jc w:val="center"/><w:spacing w:before="0" w:after="{subtitleBottomSpacer}" w:line="{subtitleLine}" w:lineRule="exact"/></w:pPr><w:r><w:rPr>{fontXml}<w:b/><w:bCs/><w:sz w:val="{subtitleSize}"/></w:rPr><w:t>{subtitle}</w:t></w:r></w:p>
<w:p><w:pPr><w:jc w:val="center"/><w:spacing w:before="0" w:after="0" w:line="{companyLine}" w:lineRule="exact"/></w:pPr><w:r><w:rPr>{fontXml}<w:b/><w:bCs/><w:sz w:val="{companySize}"/></w:rPr><w:t>{company}</w:t></w:r></w:p>
""";
    }

    private static string DocxPageTable(ExportPage page, PaperTemplateSettings template, DocumentMarginSettings paperSettings, CellMarginSettings cellSettings, CellMarginSettings photoSettings, WorkCellSizeSettings workCellSettings)
    {
        var cell = CellLayout.From(cellSettings);
        var photo = CellLayout.From(photoSettings);
        var metrics = DocxTableMetrics(page.CntPerPage, paperSettings, cellSettings, workCellSettings, template);
        var builder = new StringBuilder();
        builder.Append($"""
<w:tbl><w:tblPr><w:tblW w:w="{metrics.TableWidth}" w:type="dxa"/><w:tblLayout w:type="fixed"/><w:tblBorders><w:top w:val="single" w:sz="4" w:space="0" w:color="000000"/><w:left w:val="single" w:sz="4" w:space="0" w:color="000000"/><w:bottom w:val="single" w:sz="4" w:space="0" w:color="000000"/><w:right w:val="single" w:sz="4" w:space="0" w:color="000000"/><w:insideH w:val="single" w:sz="4" w:space="0" w:color="000000"/><w:insideV w:val="single" w:sz="4" w:space="0" w:color="000000"/></w:tblBorders></w:tblPr><w:tblGrid><w:gridCol w:w="{metrics.LabelWidth}"/><w:gridCol w:w="{metrics.ImageWidth}"/></w:tblGrid>
""");
        foreach (var item in page.Items)
        {
            builder.Append($"""<w:tr><w:trPr><w:trHeight w:val="{metrics.RowHeight}" w:hRule="exact"/></w:trPr>""");
            builder.Append($"""<w:tc><w:tcPr><w:tcW w:w="{metrics.LabelWidth}" w:type="dxa"/><w:vAlign w:val="center"/></w:tcPr>{DocxCellParagraph(item.Label, center: true, bold: true, fontSizeHalfPoints: 30, fontFamily: template.FontFamily)}</w:tc>""");
            builder.Append($"""<w:tc><w:tcPr><w:tcW w:w="{metrics.ImageWidth}" w:type="dxa"/>{DocxCellMarginsXml(cell)}<w:vAlign w:val="center"/></w:tcPr>{DocxImageParagraph(item.Image, metrics.ImageWidth, metrics.RowHeight, photo)}</w:tc>""");
            builder.Append("</w:tr>");
        }

        builder.Append($"""<w:tr><w:trPr><w:trHeight w:val="{metrics.TitleHeight}" w:hRule="exact"/></w:trPr>""");
        builder.Append($"""<w:tc><w:tcPr><w:tcW w:w="{metrics.LabelWidth}" w:type="dxa"/><w:vAlign w:val="center"/></w:tcPr>{DocxCellParagraph("공종", center: true, bold: true, fontSizeHalfPoints: 26, fontFamily: template.FontFamily)}</w:tc>""");
        builder.Append($"""<w:tc><w:tcPr><w:tcW w:w="{metrics.ImageWidth}" w:type="dxa"/><w:vAlign w:val="center"/></w:tcPr>{DocxCellParagraph(page.GroupTitle, center: true, bold: false, fontSizeHalfPoints: 26, fontFamily: template.FontFamily)}</w:tc>""");
        builder.Append("</w:tr>");
        builder.Append("</w:tbl>");
        return builder.ToString();
    }

    private static DocxTableLayout DocxTableMetrics(int cntPerPage, DocumentMarginSettings paperSettings, CellMarginSettings cellSettings, WorkCellSizeSettings workCellSettings, PaperTemplateSettings? template)
    {
        _ = cellSettings;
        template ??= new PaperTemplateSettings();
        template.Normalize();
        workCellSettings ??= new WorkCellSizeSettings();
        workCellSettings.Normalize();
        var margin = ExportLayout.From(paperSettings);
        var tableWidth = Math.Max(5200, DocxPageWidth - margin.LeftTwips - margin.RightTwips);
        var labelWidth = Math.Clamp(MmToDocxTwips(workCellSettings.WidthMm), 420, tableWidth / 2);
        var imageWidth = Math.Max(3200, tableWidth - labelWidth);
        var titleHeight = Math.Max(280, MmToDocxTwips(workCellSettings.HeightMm));
        var contentHeight = Math.Max(
            6000,
            DocxPageHeight - margin.TopTwips - margin.BottomTwips - DocxHeaderTwips - DocxFooterTwips - DocxTableVerticalSafetyTwips - DocxTemplateHeaderHeightTwips(template));
        var rowHeight = Math.Max(1500, (contentHeight - titleHeight) / Math.Max(cntPerPage, 1));
        return new DocxTableLayout(tableWidth, labelWidth, imageWidth, titleHeight, rowHeight);
    }

    private static int DocxTemplateHeaderHeightTwips(PaperTemplateSettings template)
    {
        return DocxLineTwips(template.BodyTitleFontPt, template.LineSpacingPercent) +
            DocxLineTwips(template.BodySubtitleFontPt, template.LineSpacingPercent) +
            260;
    }

    private static int DocxLineTwips(double fontPt, int lineSpacingPercent)
    {
        return Math.Max(180, (int)Math.Round(fontPt * 20.0 * lineSpacingPercent / 100.0));
    }

    private static int DocxHalfPoints(double fontPt)
    {
        return Math.Max(2, (int)Math.Round(fontPt * 2.0));
    }

    private static string DocxCellParagraph(string text, bool center, bool bold, int? fontSizeHalfPoints = null, string fontFamily = DefaultTemplateFontFace)
    {
        var align = center ? """<w:jc w:val="center"/>""" : string.Empty;
        var fontXml = DocxRunFontXml(fontFamily);
        var boldXml = bold ? "<w:b/><w:bCs/>" : string.Empty;
        var sizeXml = fontSizeHalfPoints is null ? string.Empty : $"""<w:sz w:val="{fontSizeHalfPoints.Value}"/>""";
        var runPr = fontXml.Length == 0 && boldXml.Length == 0 && sizeXml.Length == 0 ? string.Empty : $"<w:rPr>{fontXml}{boldXml}{sizeXml}</w:rPr>";
        return $"""<w:p><w:pPr>{align}</w:pPr><w:r>{runPr}<w:t>{Escape(text)}</w:t></w:r></w:p>""";
    }

    private static string DocxRunFontXml(string fontFamily)
    {
        var font = string.IsNullOrWhiteSpace(fontFamily) ? DefaultTemplateFontFace : fontFamily.Trim();
        var escaped = Escape(font);
        return $"""<w:rFonts w:ascii="{escaped}" w:hAnsi="{escaped}" w:eastAsia="{escaped}" w:cs="{escaped}"/>""";
    }

    private static string DocxCellMarginsXml(CellLayout cell)
    {
        return $"""<w:tcMar><w:top w:w="{cell.MarginTopTwips}" w:type="dxa"/><w:bottom w:w="{cell.MarginBottomTwips}" w:type="dxa"/><w:left w:w="{cell.MarginLeftTwips}" w:type="dxa"/><w:right w:w="{cell.MarginRightTwips}" w:type="dxa"/></w:tcMar>""";
    }

    private static string DocxSectionPr(DocumentMarginSettings settings, bool showPageNumber)
    {
        var margin = ExportLayout.From(settings);
        var footerReference = showPageNumber ? """<w:footerReference w:type="default" r:id="rId3"/>""" : string.Empty;
        return $"""<w:sectPr><w:pgSz w:w="{DocxPageWidth}" w:h="{DocxPageHeight}"/><w:pgMar w:top="{margin.TopTwips}" w:right="{margin.RightTwips}" w:bottom="{margin.BottomTwips}" w:left="{margin.LeftTwips}" w:header="{DocxHeaderTwips}" w:footer="{DocxFooterTwips}" w:gutter="0"/><w:titlePg/>{footerReference}</w:sectPr>""";
    }

    private static string ImageParagraph(ExportImage image)
    {
        const long cx = 4572000;
        const long cy = 3048000;
        var name = Escape(image.Name);
        return $"""<w:p><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0"><wp:extent cx="{cx}" cy="{cy}"/><wp:docPr id="1" name="{name}"/><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="0" name="{name}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="{image.DocxRelationshipId}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="{cx}" cy="{cy}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>""";
    }

    private static string DocxImageParagraph(ExportImage image, int cellWidthTwips, int rowHeightTwips, CellLayout cell)
    {
        var size = DocxImageSizeTwips(cellWidthTwips, rowHeightTwips, cell);
        var widthTwips = size.Width;
        var heightTwips = size.Height;
        var cx = widthTwips * 635L;
        var cy = heightTwips * 635L;
        var name = Escape(image.Name);
        return $"""<w:p><w:pPr><w:jc w:val="center"/></w:pPr><w:r><w:drawing><wp:inline distT="0" distB="0" distL="0" distR="0"><wp:extent cx="{cx}" cy="{cy}"/><wp:docPr id="1" name="{name}"/><a:graphic><a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture"><pic:pic><pic:nvPicPr><pic:cNvPr id="0" name="{name}"/><pic:cNvPicPr/></pic:nvPicPr><pic:blipFill><a:blip r:embed="{image.DocxRelationshipId}"/><a:stretch><a:fillRect/></a:stretch></pic:blipFill><pic:spPr><a:xfrm><a:off x="0" y="0"/><a:ext cx="{cx}" cy="{cy}"/></a:xfrm><a:prstGeom prst="rect"><a:avLst/></a:prstGeom></pic:spPr></pic:pic></a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>""";
    }

    private static DocumentImageSize DocxImageSizeTwips(int cellWidthTwips, int rowHeightTwips, CellLayout cell)
    {
        return new DocumentImageSize(
            Math.Max(900, cellWidthTwips - cell.HorizontalTwips * 2),
            Math.Max(900, rowHeightTwips - cell.VerticalTwips * 2));
    }

    private static string PageBreak()
    {
        return """<w:p><w:pPr><w:spacing w:before="0" w:after="0" w:line="1" w:lineRule="exact"/><w:rPr><w:sz w:val="1"/></w:rPr></w:pPr><w:r><w:br w:type="page"/></w:r></w:p>""";
    }

    private static string DocxFooterXml()
    {
        return """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:ftr xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><w:p><w:pPr><w:jc w:val="center"/></w:pPr><w:r><w:fldChar w:fldCharType="begin"/></w:r><w:r><w:instrText xml:space="preserve"> PAGE </w:instrText></w:r><w:r><w:fldChar w:fldCharType="separate"/></w:r><w:r><w:t>1</w:t></w:r><w:r><w:fldChar w:fldCharType="end"/></w:r></w:p></w:ftr>""";
    }

    private static string HwpxManifest(List<ExportImage> images)
    {
        var builder = new StringBuilder("""<?xml version="1.0" encoding="UTF-8"?><odf:manifest xmlns:odf="urn:oasis:names:tc:opendocument:xmlns:manifest:1.0">""");
        builder.Append("""<odf:file-entry odf:full-path="/" odf:media-type="application/hwp+zip"/>""");
        builder.Append("""<odf:file-entry odf:full-path="version.xml" odf:media-type="text/xml"/>""");
        builder.Append("""<odf:file-entry odf:full-path="settings.xml" odf:media-type="text/xml"/>""");
        builder.Append("""<odf:file-entry odf:full-path="META-INF/container.xml" odf:media-type="text/xml"/>""");
        builder.Append("""<odf:file-entry odf:full-path="Contents/content.hpf" odf:media-type="text/xml"/>""");
        builder.Append("""<odf:file-entry odf:full-path="Contents/header.xml" odf:media-type="text/xml"/>""");
        builder.Append("""<odf:file-entry odf:full-path="Contents/section0.xml" odf:media-type="text/xml"/>""");
        builder.Append("""<odf:file-entry odf:full-path="Preview/PrvText.txt" odf:media-type="text/plain"/>""");
        foreach (var image in images)
        {
            builder.Append($"""<odf:file-entry odf:full-path="{Escape(image.HwpxTarget)}" odf:media-type="{Escape(ImageContentType(image.Name))}"/>""");
        }
        builder.Append("</odf:manifest>");
        return builder.ToString();
    }

    private static string HwpxVersionXml()
    {
        return """<?xml version="1.0" encoding="UTF-8"?><hv:HWPVersion xmlns:hv="urn:hancom:office:hwpml:version" major="1" minor="1" micro="0" buildNumber="0"/>""";
    }

    private static string HwpxSettingsXml()
    {
        return """<?xml version="1.0" encoding="UTF-8"?><config-item-set xmlns="urn:oasis:names:tc:opendocument:xmlns:config:1.0" name="ooo:view-settings"><config-item-map-indexed name="Views"><config-item-map-entry><config-item name="ViewId" type="string">view1</config-item></config-item-map-entry></config-item-map-indexed></config-item-set>""";
    }

    private static string HwpxContainerXml()
    {
        return """<?xml version="1.0" encoding="UTF-8"?><container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container"><rootfiles><rootfile full-path="Contents/content.hpf" media-type="application/hwp+zip"/></rootfiles></container>""";
    }

    private static string HwpxContentHpf(List<ExportImage> images)
    {
        const string hc = "http://www.hancom.co.kr/hwpml/2011/core";
        var builder = new StringBuilder($"""<?xml version="1.0" encoding="UTF-8"?><hc:package xmlns:hc="{hc}"><hc:metadata><hc:title>{ExportDocumentTitle}</hc:title><hc:creator>{ApplicationName}</hc:creator></hc:metadata><hc:manifest>""");
        builder.Append("""<hc:item id="header" href="Contents/header.xml" media-type="text/xml"/>""");
        builder.Append("""<hc:item id="section0" href="Contents/section0.xml" media-type="text/xml"/>""");
        foreach (var image in images)
        {
            builder.Append($"""<hc:item id="{Escape(image.HwpxId)}" href="{Escape(image.HwpxTarget)}" media-type="{Escape(ImageContentType(image.Name))}" isEmbeded="1"/>""");
        }
        builder.Append("""</hc:manifest><hc:spine><hc:itemref idref="section0"/></hc:spine></hc:package>""");
        return builder.ToString();
    }

    private static string HwpxContentHpfFromTemplate(byte[] templateBytes, List<ExportImage> images)
    {
        var content = ReadTemplateEntry(templateBytes, "Contents/content.hpf");
        if (images.Count == 0)
        {
            return content;
        }

        var builder = new StringBuilder();
        foreach (var image in images)
        {
            builder.Append($"""<opf:item id="{Escape(image.HwpxId)}" href="{Escape(image.HwpxTarget)}" media-type="{Escape(ImageContentType(image.HwpxTarget))}" isEmbeded="1"/>""");
        }

        return content.Replace("</opf:manifest>", builder + "</opf:manifest>", StringComparison.Ordinal);
    }

    private static string HwpxHeaderXmlFromTemplate(byte[] templateBytes, List<ExportImage> images)
    {
        var header = ReadTemplateEntry(templateBytes, "Contents/header.xml");
        if (images.Count == 0)
        {
            return header;
        }

        var builder = new StringBuilder($"""<hh:binDataList itemCnt="{images.Count}">""");
        for (var i = 0; i < images.Count; i++)
        {
            var image = images[i];
            var binDataName = Path.GetFileName(image.HwpxTarget);
            var format = Path.GetExtension(image.HwpxTarget).TrimStart('.').ToLowerInvariant();
            builder.Append($"""<hh:binItem id="{i}" Type="Embedding" BinData="{Escape(binDataName)}" Format="{Escape(format)}"/>""");
        }
        builder.Append("</hh:binDataList>");

        return header.Replace("</hh:refList>", builder + "</hh:refList>", StringComparison.Ordinal);
    }

    private static string HwpxHeaderXmlWithBlackTableBorder(byte[] templateBytes, PaperTemplateSettings template)
    {
        template.Normalize();
        var header = ReadTemplateEntry(templateBytes, "Contents/header.xml");
        header = ForceHwpxDefaultParagraphCenter(header);
        header = EnsureHwpxCenterParagraphStyle(header);
        header = EnsureHwpxZeroParagraphStyle(header);
        header = EnsureHwpxTemplateParagraphStyles(header, template.LineSpacingPercent);
        header = EnsureHwpxTemplateFontFace(header, template.FontFamily);
        header = EnsureHwpxBoldCharacterStyle(header);
        header = EnsureHwpxTemplateCharacterStyles(header, template);
        header = EnsureHwpxTableBorderFills(header);
        return header;
    }

    private static string EnsureHwpxTableBorderFills(string header)
    {
        const string borderFillsClose = "</hh:borderFills>";
        if (!header.Contains(borderFillsClose, StringComparison.Ordinal))
        {
            return header;
        }

        var builder = new StringBuilder();
        if (!header.Contains("<hh:borderFill id=\"" + HwpxTableBorderFillId + "\"", StringComparison.Ordinal))
        {
            builder.AppendLine(HwpxTableBorderFillXml(HwpxOuterTableBorderFillBaseId, 0));
        }

        for (var mask = 1; mask < 16; mask++)
        {
            var id = HwpxOuterTableBorderFillBaseId + mask;
            if (!header.Contains($"<hh:borderFill id=\"{id}\"", StringComparison.Ordinal))
            {
                builder.AppendLine(HwpxTableBorderFillXml(id, mask));
            }
        }

        if (builder.Length == 0)
        {
            return header;
        }

        var insertAt = header.IndexOf(borderFillsClose, StringComparison.Ordinal);
        header = header.Insert(insertAt, builder.ToString());

        return System.Text.RegularExpressions.Regex.Replace(
            header,
            """<hh:borderFills itemCnt="(\d+)">""",
            match =>
            {
                var count = int.TryParse(match.Groups[1].Value, out var value) ? value + builder.ToString().Count(ch => ch == '\n') : 1;
                return $"""<hh:borderFills itemCnt="{count}">""";
            },
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string HwpxTableBorderFillXml(int id, int outerMask)
    {
        var left = (outerMask & 8) != 0 ? "0.6 mm" : "0.1 mm";
        var right = (outerMask & 2) != 0 ? "0.6 mm" : "0.1 mm";
        var top = (outerMask & 1) != 0 ? "0.6 mm" : "0.1 mm";
        var bottom = (outerMask & 4) != 0 ? "0.6 mm" : "0.1 mm";
        return $"""<hh:borderFill id="{id}" threeD="0" shadow="0" centerLine="NONE" breakCellSeparateLine="0"><hh:slash type="NONE" Crooked="0" isCounter="0"/><hh:backSlash type="NONE" Crooked="0" isCounter="0"/><hh:leftBorder type="SOLID" width="{left}" color="#000000"/><hh:rightBorder type="SOLID" width="{right}" color="#000000"/><hh:topBorder type="SOLID" width="{top}" color="#000000"/><hh:bottomBorder type="SOLID" width="{bottom}" color="#000000"/><hh:diagonal type="NONE" width="0.1 mm" color="#000000"/></hh:borderFill>""";
    }

    private static string EnsureHwpxCenterParagraphStyle(string header)
    {
        if (header.Contains("<hh:paraPr id=\"" + HwpxCenterParaPrId + "\"", StringComparison.Ordinal))
        {
            return ForceHwpxParagraphCenter(header, HwpxCenterParaPrId);
        }

        const string paraPropertiesClose = "</hh:paraProperties>";
        var insertAt = header.IndexOf(paraPropertiesClose, StringComparison.Ordinal);
        if (insertAt < 0)
        {
            return header;
        }

        var centerParaPr = $"""
<hh:paraPr id="{HwpxCenterParaPrId}" tabPrIDRef="0" condense="0" fontLineHeight="0" snapToGrid="0" suppressLineNumbers="0" checked="0" textDir="LTR"><hp:align horizontal="CENTER" vertical="BASELINE"/><hp:heading type="NONE" idRef="0" level="0"/><hp:breakSetting breakLatinWord="KEEP_WORD" breakNonLatinWord="KEEP_WORD" widowOrphan="0" keepWithNext="0" keepLines="0" pageBreakBefore="0" lineWrap="BREAK"/><hp:margin indent="0" left="0" right="0" prev="0" next="0"/><hp:lineSpacing type="PERCENT" value="100"/></hh:paraPr>
""";
        header = header.Insert(insertAt, centerParaPr);

        return System.Text.RegularExpressions.Regex.Replace(
            header,
            """<hh:paraProperties itemCnt="(\d+)">""",
            match =>
            {
                var count = int.TryParse(match.Groups[1].Value, out var value) ? value + 1 : 1;
                return $"""<hh:paraProperties itemCnt="{count}">""";
            },
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string ForceHwpxDefaultParagraphCenter(string header)
    {
        return ForceHwpxParagraphCenter(header, "0");
    }

    private static string ForceHwpxParagraphCenter(string header, string paraPrId)
    {
        return ForceHwpxParagraphAlignment(header, paraPrId, "CENTER");
    }

    private static string ForceHwpxParagraphAlignment(string header, string paraPrId, string horizontalAlign)
    {
        header = System.Text.RegularExpressions.Regex.Replace(
            header,
            $"""(<hh:paraPr id="{paraPrId}"[\s\S]*?<(?:hh|hp):align horizontal=")(?:LEFT|RIGHT|CENTER|JUSTIFY)("[^>]*/>)""",
            $"$1{horizontalAlign}$2",
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));

        return ForceHwpxParagraphMarginZero(header, paraPrId);
    }

    private static string ForceHwpxParagraphMarginZero(string header, string paraPrId)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            header,
            $"""<hh:paraPr id="{paraPrId}"[\s\S]*?</hh:paraPr>""",
            match =>
            {
                var paragraph = match.Value;
                paragraph = System.Text.RegularExpressions.Regex.Replace(
                    paragraph,
                    """<hc:(intent|left|right|prev|next) value="-?\d+" unit="HWPUNIT"/>""",
                    """<hc:$1 value="0" unit="HWPUNIT"/>""",
                    System.Text.RegularExpressions.RegexOptions.None,
                    TimeSpan.FromSeconds(1));
                paragraph = System.Text.RegularExpressions.Regex.Replace(
                    paragraph,
                    """<hp:margin indent="-?\d+" left="-?\d+" right="-?\d+" prev="-?\d+" next="-?\d+"/>""",
                    """<hp:margin indent="0" left="0" right="0" prev="0" next="0"/>""",
                    System.Text.RegularExpressions.RegexOptions.None,
                    TimeSpan.FromSeconds(1));
                return paragraph;
            },
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string EnsureHwpxZeroParagraphStyle(string header)
    {
        if (header.Contains("<hh:paraPr id=\"" + HwpxZeroParaPrId + "\"", StringComparison.Ordinal))
        {
            return ForceHwpxParagraphCenter(header, HwpxZeroParaPrId);
        }

        const string paraPropertiesClose = "</hh:paraProperties>";
        var insertAt = header.IndexOf(paraPropertiesClose, StringComparison.Ordinal);
        if (insertAt < 0)
        {
            return header;
        }

        var zeroParaPr = $"""
<hh:paraPr id="{HwpxZeroParaPrId}" tabPrIDRef="0" condense="0" fontLineHeight="0" snapToGrid="0" suppressLineNumbers="0" checked="0" textDir="LTR"><hp:align horizontal="CENTER" vertical="BASELINE"/><hp:heading type="NONE" idRef="0" level="0"/><hp:breakSetting breakLatinWord="KEEP_WORD" breakNonLatinWord="KEEP_WORD" widowOrphan="0" keepWithNext="0" keepLines="0" pageBreakBefore="0" lineWrap="BREAK"/><hp:margin indent="0" left="0" right="0" prev="0" next="0"/><hp:lineSpacing type="FIXED" value="0"/></hh:paraPr>
""";
        header = header.Insert(insertAt, zeroParaPr);

        return System.Text.RegularExpressions.Regex.Replace(
            header,
            """<hh:paraProperties itemCnt="(\d+)">""",
            match =>
            {
                var count = int.TryParse(match.Groups[1].Value, out var value) ? value + 1 : 1;
                return $"""<hh:paraProperties itemCnt="{count}">""";
            },
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string EnsureHwpxBoldCharacterStyle(string header)
    {
        if (header.Contains("<hh:charPr id=\"" + HwpxBoldCharPrId + "\"", StringComparison.Ordinal))
        {
            return header;
        }

        const string charPropertiesClose = "</hh:charProperties>";
        var insertAt = header.IndexOf(charPropertiesClose, StringComparison.Ordinal);
        if (insertAt < 0)
        {
            return header;
        }

        var boldCharPr = $"""
<hh:charPr id="{HwpxBoldCharPrId}" height="1000" textColor="#000000" shadeColor="none" useFontSpace="0" useKerning="0" symMark="NONE" borderFillIDRef="2"><hh:fontRef hangul="1" latin="1" hanja="1" japanese="1" other="1" symbol="1" user="1"/><hh:ratio hangul="100" latin="100" hanja="100" japanese="100" other="100" symbol="100" user="100"/><hh:spacing hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/><hh:relSz hangul="100" latin="100" hanja="100" japanese="100" other="100" symbol="100" user="100"/><hh:offset hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/><hh:bold/><hh:underline type="NONE" shape="SOLID" color="#000000"/><hh:strikeout shape="NONE" color="#000000"/><hh:outline type="NONE"/><hh:shadow type="NONE" color="#C0C0C0" offsetX="10" offsetY="10"/></hh:charPr>
""";
        header = header.Insert(insertAt, boldCharPr);

        return System.Text.RegularExpressions.Regex.Replace(
            header,
            """<hh:charProperties itemCnt="(\d+)">""",
            match =>
            {
                var count = int.TryParse(match.Groups[1].Value, out var value) ? value + 1 : 1;
                return $"""<hh:charProperties itemCnt="{count}">""";
            },
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string EnsureHwpxTemplateParagraphStyles(string header, int lineSpacingPercent)
    {
        header = EnsureHwpxParagraphStyle(header, HwpxTemplateTitleParaPrId, "CENTER", fixedLineSpacing: false, lineSpacingPercent);
        header = EnsureHwpxParagraphStyle(header, HwpxTemplateLeftParaPrId, "LEFT", fixedLineSpacing: false, lineSpacingPercent);
        return header;
    }

    private static string EnsureHwpxParagraphStyle(string header, string id, string horizontalAlign, bool fixedLineSpacing, int lineSpacingPercent = 160)
    {
        if (header.Contains("<hh:paraPr id=\"" + id + "\"", StringComparison.Ordinal))
        {
            header = ForceHwpxParagraphAlignment(header, id, horizontalAlign);
            var replacementLineSpacing = fixedLineSpacing
                ? """<$1:lineSpacing type="FIXED" value="0"/>"""
                : $"""<$1:lineSpacing type="PERCENT" value="{lineSpacingPercent}"/>""";
            return System.Text.RegularExpressions.Regex.Replace(
                header,
                $"""<hh:paraPr id="{id}"[\s\S]*?</hh:paraPr>""",
                match => System.Text.RegularExpressions.Regex.Replace(
                    match.Value,
                    """<(hh|hp):lineSpacing\s+[^>]*/>""",
                    replacementLineSpacing,
                    System.Text.RegularExpressions.RegexOptions.None,
                    TimeSpan.FromSeconds(1)),
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromSeconds(1));
        }

        const string paraPropertiesClose = "</hh:paraProperties>";
        var insertAt = header.IndexOf(paraPropertiesClose, StringComparison.Ordinal);
        if (insertAt < 0)
        {
            return header;
        }

        var lineSpacing = fixedLineSpacing
            ? """<hh:lineSpacing type="FIXED" value="0"/>"""
            : $"""<hh:lineSpacing type="PERCENT" value="{lineSpacingPercent}"/>""";
        var paraPr = $"""
<hh:paraPr id="{id}" tabPrIDRef="0" condense="0" fontLineHeight="0" snapToGrid="0" suppressLineNumbers="0" checked="0" textDir="LTR"><hh:align horizontal="{horizontalAlign}" vertical="BASELINE"/><hh:heading type="NONE" idRef="0" level="0"/><hh:breakSetting breakLatinWord="KEEP_WORD" breakNonLatinWord="KEEP_WORD" widowOrphan="0" keepWithNext="0" keepLines="0" pageBreakBefore="0" lineWrap="BREAK"/><hh:margin><hc:intent value="0" unit="HWPUNIT"/><hc:left value="0" unit="HWPUNIT"/><hc:right value="0" unit="HWPUNIT"/><hc:prev value="0" unit="HWPUNIT"/><hc:next value="0" unit="HWPUNIT"/></hh:margin>{lineSpacing}</hh:paraPr>
""";
        header = header.Insert(insertAt, paraPr);

        return System.Text.RegularExpressions.Regex.Replace(
            header,
            """<hh:paraProperties itemCnt="(\d+)">""",
            match =>
            {
                var count = int.TryParse(match.Groups[1].Value, out var value) ? value + 1 : 1;
                return $"""<hh:paraProperties itemCnt="{count}">""";
            },
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string EnsureHwpxTemplateFontFace(string header, string fontFamily)
    {
        var fontFace = string.IsNullOrWhiteSpace(fontFamily) ? DefaultTemplateFontFace : fontFamily.Trim();
        var escapedFontFace = Escape(fontFace);
        var fontFaceAttribute = $"face=\"{escapedFontFace}\"";
        if (header.Contains(fontFaceAttribute, StringComparison.Ordinal))
        {
            return header;
        }

        return System.Text.RegularExpressions.Regex.Replace(
            header,
            """<hh:fontface lang="([^"]+)" fontCnt="(\d+)">([\s\S]*?)</hh:fontface>""",
            match =>
            {
                var lang = match.Groups[1].Value;
                var count = int.TryParse(match.Groups[2].Value, out var value) ? value : 0;
                var content = match.Groups[3].Value;
                if (content.Contains(fontFaceAttribute, StringComparison.Ordinal))
                {
                    return match.Value;
                }

                var fontId = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var nextCount = (count + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                return $"""<hh:fontface lang="{lang}" fontCnt="{nextCount}">{content}<hh:font id="{fontId}" face="{escapedFontFace}" type="TTF" isEmbedded="0"><hh:typeInfo familyType="FCAT_GOTHIC" weight="6" proportion="4" contrast="0" strokeVariation="1" armStyle="1" letterform="1" midline="1" xHeight="1"/></hh:font></hh:fontface>""";
            },
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string HwpxTemplateFontId(string header, string fontFamily)
    {
        var fontFace = string.IsNullOrWhiteSpace(fontFamily) ? DefaultTemplateFontFace : fontFamily.Trim();
        var match = System.Text.RegularExpressions.Regex.Match(
            header,
            "<hh:fontface lang=\"HANGUL\" fontCnt=\"\\d+\">[\\s\\S]*?<hh:font id=\"(\\d+)\" face=\"" + System.Text.RegularExpressions.Regex.Escape(Escape(fontFace)) + "\"",
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
        return match.Success ? match.Groups[1].Value : "1";
    }

    private static string EnsureHwpxTemplateCharacterStyles(string header, PaperTemplateSettings template)
    {
        template.Normalize();
        var fontId = HwpxTemplateFontId(header, template.FontFamily);
        header = EnsureHwpxCharacterStyle(header, HwpxTemplateTitleCharPrId, PtToHwpxCharHeight(template.BodyTitleFontPt), bold: true, fontId);
        header = EnsureHwpxCharacterStyle(header, HwpxTemplateSubtitleCharPrId, PtToHwpxCharHeight(template.BodySubtitleFontPt), bold: true, fontId);
        header = EnsureHwpxCharacterStyle(header, HwpxTablePhaseCharPrId, 1500, bold: true, fontId);
        header = EnsureHwpxCharacterStyle(header, HwpxTableWorkTypeCharPrId, 1300, bold: true, fontId);
        header = EnsureHwpxCharacterStyle(header, HwpxCoverTitleCharPrId, PtToHwpxCharHeight(template.TitleFontPt), bold: true, fontId);
        header = EnsureHwpxCharacterStyle(header, HwpxCoverSubtitleCharPrId, PtToHwpxCharHeight(template.SubtitleFontPt), bold: true, fontId);
        header = EnsureHwpxCharacterStyle(header, HwpxCoverCompanyCharPrId, PtToHwpxCharHeight(template.CompanyFontPt), bold: true, fontId);
        header = EnsureHwpxCharacterStyle(header, HwpxTableWorkTitleCharPrId, 1300, bold: false, fontId);
        return header;
    }

    private static string EnsureHwpxCharacterStyle(string header, string id, int height, bool bold, string fontId)
    {
        var charPr = HwpxCharacterStyleXml(id, height, bold, fontId);
        var charPrPattern = $"""<hh:charPr\s+id="{System.Text.RegularExpressions.Regex.Escape(id)}"[^>]*>.*?</hh:charPr>""";
        if (System.Text.RegularExpressions.Regex.IsMatch(
            header,
            charPrPattern,
            System.Text.RegularExpressions.RegexOptions.Singleline,
            TimeSpan.FromSeconds(1)))
        {
            return System.Text.RegularExpressions.Regex.Replace(
                header,
                charPrPattern,
                charPr.TrimEnd(),
                System.Text.RegularExpressions.RegexOptions.Singleline,
                TimeSpan.FromSeconds(1));
        }

        const string charPropertiesClose = "</hh:charProperties>";
        var insertAt = header.IndexOf(charPropertiesClose, StringComparison.Ordinal);
        if (insertAt < 0)
        {
            return header;
        }

        header = header.Insert(insertAt, charPr);

        return System.Text.RegularExpressions.Regex.Replace(
            header,
            """<hh:charProperties itemCnt="(\d+)">""",
            match =>
            {
                var count = int.TryParse(match.Groups[1].Value, out var value) ? value + 1 : 1;
                return $"""<hh:charProperties itemCnt="{count}">""";
            },
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string HwpxCharacterStyleXml(string id, int height, bool bold, string fontId)
    {
        var boldXml = bold ? "<hh:bold/>" : string.Empty;
        return $"""
<hh:charPr id="{id}" height="{height}" textColor="#000000" shadeColor="none" useFontSpace="0" useKerning="0" symMark="NONE" borderFillIDRef="2"><hh:fontRef hangul="{fontId}" latin="{fontId}" hanja="{fontId}" japanese="{fontId}" other="{fontId}" symbol="{fontId}" user="{fontId}"/><hh:ratio hangul="100" latin="100" hanja="100" japanese="100" other="100" symbol="100" user="100"/><hh:spacing hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/><hh:relSz hangul="100" latin="100" hanja="100" japanese="100" other="100" symbol="100" user="100"/><hh:offset hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/>{boldXml}<hh:underline type="NONE" shape="SOLID" color="#000000"/><hh:strikeout shape="NONE" color="#000000"/><hh:outline type="NONE"/><hh:shadow type="NONE" color="#C0C0C0" offsetX="10" offsetY="10"/></hh:charPr>
""";
    }

    private static string HwpxHeaderXml()
    {
        const string hp = "http://www.hancom.co.kr/hwpml/2011/paragraph";
        const string hh = "http://www.hancom.co.kr/hwpml/2011/head";
        return $$"""
<?xml version="1.0" encoding="UTF-8"?>
<hh:head xmlns:hh="{{hh}}" xmlns:hp="{{hp}}">
  <hh:beginNum page="1" footnote="1" endnote="1"/>
  <hh:refList>
    <hh:fontfaces itemCnt="7">
      <hh:fontface lang="HANGUL"><hp:font id="0" face="함초롬바탕" type="TTF"/></hh:fontface>
      <hh:fontface lang="LATIN"><hp:font id="0" face="함초롬바탕" type="TTF"/></hh:fontface>
      <hh:fontface lang="HANJA"><hp:font id="0" face="함초롬바탕" type="TTF"/></hh:fontface>
      <hh:fontface lang="JAPANESE"><hp:font id="0" face="함초롬바탕" type="TTF"/></hh:fontface>
      <hh:fontface lang="OTHER"><hp:font id="0" face="함초롬바탕" type="TTF"/></hh:fontface>
      <hh:fontface lang="SYMBOL"><hp:font id="0" face="함초롬바탕" type="TTF"/></hh:fontface>
      <hh:fontface lang="USER"><hp:font id="0" face="함초롬바탕" type="TTF"/></hh:fontface>
    </hh:fontfaces>
    <hh:borderFills itemCnt="1">
      <hh:borderFill id="1">
        <hh:slash type="NONE"/><hh:backSlash type="NONE"/>
        <hh:leftBorder type="NONE" width="0.1mm" color="#000000"/>
        <hh:rightBorder type="NONE" width="0.1mm" color="#000000"/>
        <hh:topBorder type="NONE" width="0.1mm" color="#000000"/>
        <hh:bottomBorder type="NONE" width="0.1mm" color="#000000"/>
      </hh:borderFill>
    </hh:borderFills>
    <hh:charProperties itemCnt="3">
      <hh:charPr id="0" height="1000" textColor="#000000" shadeColor="none" useFontSpace="0" useKerning="0" symMark="NONE" borderFillIDRef="1">
        <hp:fontRef hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/>
        <hp:ratio hangul="100" latin="100" hanja="100" japanese="100" other="100" symbol="100" user="100"/>
        <hp:spacing hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/>
        <hp:relSz hangul="100" latin="100" hanja="100" japanese="100" other="100" symbol="100" user="100"/>
        <hp:offset hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/>
      </hh:charPr>
      <hh:charPr id="1" height="1600" textColor="#000000" shadeColor="none" useFontSpace="0" useKerning="0" symMark="NONE" borderFillIDRef="1">
        <hp:fontRef hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/>
        <hp:bold/>
      </hh:charPr>
      <hh:charPr id="2" height="1200" textColor="#000000" shadeColor="none" useFontSpace="0" useKerning="0" symMark="NONE" borderFillIDRef="1">
        <hp:fontRef hangul="0" latin="0" hanja="0" japanese="0" other="0" symbol="0" user="0"/>
        <hp:bold/>
      </hh:charPr>
    </hh:charProperties>
    <hh:tabProperties itemCnt="1"><hh:tabPr id="0" autoTabLeft="0" autoTabRight="0"/></hh:tabProperties>
    <hh:paraProperties itemCnt="2">
      <hh:paraPr id="0" tabPrIDRef="0" condense="0" fontLineHeight="0" snapToGrid="0" suppressLineNumbers="0" checked="0">
        <hp:align horizontal="LEFT" vertical="BASELINE"/>
        <hp:heading type="NONE" idRef="0" level="0"/>
        <hp:breakSetting breakLatinWord="KEEP_WORD" breakNonLatinWord="KEEP_WORD" widowOrphan="0" keepWithNext="0" keepLines="0" pageBreakBefore="0" lineWrap="BREAK"/>
        <hp:margin indent="0" left="0" right="0" prev="0" next="0"/>
        <hp:lineSpacing type="PERCENT" value="160"/>
      </hh:paraPr>
      <hh:paraPr id="1" tabPrIDRef="0" condense="0" fontLineHeight="0" snapToGrid="0" suppressLineNumbers="0" checked="0">
        <hp:align horizontal="CENTER" vertical="BASELINE"/>
        <hp:heading type="NONE" idRef="0" level="0"/>
        <hp:breakSetting breakLatinWord="KEEP_WORD" breakNonLatinWord="KEEP_WORD" widowOrphan="0" keepWithNext="0" keepLines="0" pageBreakBefore="0" lineWrap="BREAK"/>
        <hp:margin indent="0" left="0" right="0" prev="0" next="300"/>
        <hp:lineSpacing type="PERCENT" value="160"/>
      </hh:paraPr>
    </hh:paraProperties>
    <hh:styles itemCnt="1"><hh:style id="0" type="PARA" name="Normal" engName="Normal" paraPrIDRef="0" charPrIDRef="0" nextStyleIDRef="0" langID="1042" lockForm="0"/></hh:styles>
  </hh:refList>
</hh:head>
""";
    }

    private static string HwpxSectionXml(AppState state, List<ExportImage> images)
    {
        const string hp = "http://www.hancom.co.kr/hwpml/2011/paragraph";
        const string hs = "http://www.hancom.co.kr/hwpml/2011/section";
        var builder = new StringBuilder($"""<?xml version="1.0" encoding="UTF-8"?><hs:sec xmlns:hs="{hs}" xmlns:hp="{hp}">""");
        var paragraphId = 1000000001;

        builder.Append($"""<hp:p id="{paragraphId++}" paraPrIDRef="0" styleIDRef="0" pageBreak="0" columnBreak="0" merged="0"><hp:run charPrIDRef="0"><hp:secPr textDirection="HORIZONTAL" spaceColumns="1134"><hp:pageSize width="{HwpxPageWidth}" height="{HwpxPageHeight}"/><hp:pageMar left="4252" right="4252" top="5669" bottom="4252" header="{HwpxHeaderUnits}" footer="{HwpxFooterUnits}"/></hp:secPr><hp:ctrl><hp:colPr id="" type="NEWSPAPER" layout="LEFT" colCount="1" sameSz="1" sameGap="0"/></hp:ctrl></hp:run><hp:run charPrIDRef="0"><hp:t/></hp:run></hp:p>""");
        builder.Append(HwpxParagraph(ref paragraphId, ExportDocumentTitle, "1", "1"));
        foreach (var group in state.Groups)
        {
            builder.Append(HwpxParagraph(ref paragraphId, group.Title, "1", "2"));
            var countOnPage = 0;
            var limit = group.CntPerPage == 4 ? 4 : 3;

            foreach (var phase in new[] { Phase.Before, Phase.Processing, Phase.After })
            {
                var photos = group.Photos(phase);
                if (photos.Count == 0)
                {
                    continue;
                }

                for (var i = 0; i < photos.Count; i++)
                {
                    if (countOnPage >= limit)
                    {
                        builder.Append(HwpxParagraph(ref paragraphId, string.Empty, "0", "0", pageBreak: true));
                        countOnPage = 0;
                    }

                    var path = photos[i];
                    builder.Append(HwpxParagraph(ref paragraphId, group.LabelAt(phase, i), "0", "2"));
                    builder.Append(HwpxParagraph(ref paragraphId, $"[사진] {Path.GetFileName(path)}", "0", "0"));
                    countOnPage++;
                }
            }

            builder.Append(HwpxParagraph(ref paragraphId, string.Empty, "0", "0", pageBreak: true));
        }

        builder.Append("</hs:sec>");
        return builder.ToString();
    }

    private static string HwpxSectionXmlFromTemplate(byte[] templateBytes, AppState state, List<ExportImage> images)
    {
        var templateSection = ReadTemplateEntry(templateBytes, "Contents/section0.xml");
        var firstParagraphStart = templateSection.IndexOf("<hp:p", StringComparison.Ordinal);
        var firstParagraphEnd = templateSection.IndexOf("</hp:p>", StringComparison.Ordinal);
        if (firstParagraphStart < 0 || firstParagraphEnd < 0)
        {
            throw new InvalidOperationException("HWPX 기본 템플릿의 첫 문단 구조를 찾을 수 없습니다.");
        }
        firstParagraphEnd += "</hp:p>".Length;

        var pages = BuildExportPages(state, images).ToList();
        var template = state.PaperTemplates!.Hwpx!;
        var sectionSettings = state.ExportSettings.SettingsFor(pages.FirstOrDefault()?.CntPerPage ?? 3);
        var sectionHead = templateSection[..firstParagraphStart];
        var sectionControls = ExtractHwpxSectionControls(templateSection[firstParagraphStart..firstParagraphEnd]);
        sectionControls = ApplyHwpxPageMargins(sectionControls, sectionSettings.Hwpx);
        if (template.ShowPageNumber)
        {
            sectionControls = HideHwpxFirstPageNumber(sectionControls);
            sectionControls = InjectHwpxPageNumberControl(sectionControls);
        }
        var builder = new StringBuilder(sectionHead);
        var paragraphId = 1000;
        builder.Append(HwpxCoverPage(ref paragraphId, template, sectionControls, sectionSettings.Hwpx));

        for (var i = 0; i < pages.Count; i++)
        {
            var settings = state.ExportSettings.SettingsFor(pages[i].CntPerPage);
            builder.Append(HwpxPageTable(ref paragraphId, pages[i], template, settings.Hwpx, settings.HwpxCell!, settings.HwpxPhoto!, settings.HwpxWorkCell!, string.Empty, pageBreak: true));
        }

        builder.Append("</hs:sec>");
        return builder.ToString();
    }

    private static string HwpxPageTable(ref int id, ExportPage page, PaperTemplateSettings? template, DocumentMarginSettings paperSettings, CellMarginSettings cellSettings, CellMarginSettings photoSettings, WorkCellSizeSettings workCellSettings, string sectionControls = "", bool pageBreak = false)
    {
        var photo = CellLayout.From(photoSettings);
        var metrics = HwpxTableMetrics(page.CntPerPage, paperSettings, cellSettings, workCellSettings, template);
        var tableHeight = metrics.TitleHeight + metrics.RowHeight * Math.Max(page.Items.Count, 1);
        var tableId = id + 50000;
        var builder = new StringBuilder();
        builder.Append(HwpxTemplateHeader(ref id, template, sectionControls, pageBreak));

        builder.Append($"""<hp:p id="{id++}" paraPrIDRef="{HwpxCenterParaPrId}" styleIDRef="0" pageBreak="0" columnBreak="0" merged="0"><hp:run charPrIDRef="0"><hp:tbl id="{tableId}" zOrder="0" numberingType="TABLE" textWrap="TOP_AND_BOTTOM" textFlow="BOTH_SIDES" lock="0" dropcapstyle="None" pageBreak="TABLE" repeatHeader="0" rowCnt="{page.Items.Count + 1}" colCnt="2" cellSpacing="0" borderFillIDRef="{HwpxTableBorderFillId}" noAdjust="0"><hp:sz width="{metrics.TableWidth}" widthRelTo="ABSOLUTE" height="{tableHeight}" heightRelTo="ABSOLUTE" protect="0"/><hp:pos treatAsChar="0" affectLSpacing="0" flowWithText="1" allowOverlap="0" holdAnchorAndSO="0" vertRelTo="PARA" horzRelTo="COLUMN" vertAlign="TOP" horzAlign="CENTER" vertOffset="0" horzOffset="0"/><hp:outMargin left="{HwpxFixedOuterMarginUnits}" right="{HwpxFixedOuterMarginUnits}" top="{HwpxFixedOuterMarginUnits}" bottom="{HwpxFixedOuterMarginUnits}"/><hp:inMargin left="0" right="0" top="0" bottom="0"/>""");

        for (var i = 0; i < page.Items.Count; i++)
        {
            var item = page.Items[i];
            var row = i;
            builder.Append(HwpxTableRow(ref id, row, metrics.RowHeight, page.Items.Count + 1, [
                HwpxCell.NoMargin(0, 1, metrics.LabelWidth, HwpxCellText(ref id, item.Label, HwpxTablePhaseCharPrId)),
                HwpxCell.NoMargin(1, 1, metrics.ImageWidth, HwpxCellImage(ref id, item.Image, metrics.ImageWidth, metrics.RowHeight, photo))
            ]));
        }

        var titleRow = page.Items.Count;
        builder.Append(HwpxTableRow(ref id, titleRow, metrics.TitleHeight, page.Items.Count + 1, [
            HwpxCell.NoMargin(0, 1, metrics.LabelWidth, HwpxCellText(ref id, "공종", HwpxTableWorkTypeCharPrId)),
            HwpxCell.NoMargin(1, 1, metrics.ImageWidth, HwpxCellText(ref id, page.GroupTitle, HwpxTableWorkTitleCharPrId))
        ]));
        builder.Append("""</hp:tbl></hp:run></hp:p>""");
        return builder.ToString();
    }

    private static string HwpxCoverPage(ref int id, PaperTemplateSettings? template, string sectionControls, DocumentMarginSettings paperSettings)
    {
        template ??= new PaperTemplateSettings();
        template.Normalize();
        var margin = ExportLayout.From(paperSettings);
        var tableWidth = Math.Max(26000, HwpxPageWidth - margin.LeftHwpx - margin.RightHwpx);
        var contentHeight = Math.Max(36000, HwpxPageHeight - margin.TopHwpx - margin.BottomHwpx - HwpxHeaderUnits - HwpxFooterUnits);
        var titleTopSpacer = Math.Max(2600, contentHeight * 105 / 1000);
        var shiftedSpacer = titleTopSpacer / 2;
        var rows = new[]
        {
            shiftedSpacer,
            Math.Max(3600, PtToHwpxLineHeight(template.TitleFontPt, template.LineSpacingPercent)),
            MmToHwpxUnits(18),
            Math.Max(3000, PtToHwpxLineHeight(template.SubtitleFontPt, template.LineSpacingPercent)),
            MmToHwpxUnits(133),
            Math.Max(3600, PtToHwpxLineHeight(template.CompanyFontPt, template.LineSpacingPercent))
        };
        var tableHeight = rows.Sum();
        var tableId = id + 50000;
        var pageBreakValue = "0";
        var builder = new StringBuilder();
        builder.Append($"""<hp:p id="{id++}" paraPrIDRef="{HwpxCenterParaPrId}" styleIDRef="0" pageBreak="{pageBreakValue}" columnBreak="0" merged="0"><hp:run charPrIDRef="0">{sectionControls}<hp:tbl id="{tableId}" zOrder="0" numberingType="TABLE" textWrap="TOP_AND_BOTTOM" textFlow="BOTH_SIDES" lock="0" dropcapstyle="None" pageBreak="TABLE" repeatHeader="0" rowCnt="6" colCnt="1" cellSpacing="0" borderFillIDRef="2" noAdjust="0"><hp:sz width="{tableWidth}" widthRelTo="ABSOLUTE" height="{tableHeight}" heightRelTo="ABSOLUTE" protect="0"/><hp:pos treatAsChar="0" affectLSpacing="0" flowWithText="1" allowOverlap="0" holdAnchorAndSO="0" vertRelTo="PARA" horzRelTo="COLUMN" vertAlign="TOP" horzAlign="CENTER" vertOffset="0" horzOffset="0"/><hp:outMargin left="0" right="0" top="0" bottom="0"/><hp:inMargin left="0" right="0" top="0" bottom="0"/>""");
        builder.Append(HwpxCoverRow(ref id, 0, rows[0], tableWidth, string.Empty, "0"));
        builder.Append(HwpxCoverRow(ref id, 1, rows[1], tableWidth, template.Title, HwpxCoverTitleCharPrId));
        builder.Append(HwpxCoverRow(ref id, 2, rows[2], tableWidth, string.Empty, "0"));
        builder.Append(HwpxCoverRow(ref id, 3, rows[3], tableWidth, template.Subtitle, HwpxCoverSubtitleCharPrId));
        builder.Append(HwpxCoverRow(ref id, 4, rows[4], tableWidth, string.Empty, "0"));
        builder.Append(HwpxCoverRow(ref id, 5, rows[5], tableWidth, template.Company, HwpxCoverCompanyCharPrId));
        builder.Append("""</hp:tbl></hp:run></hp:p>""");
        return builder.ToString();
    }

    private static string HwpxCoverRow(ref int id, int rowIndex, int rowHeight, int width, string text, string charPrId)
    {
        return $"""<hp:tr><hp:tc name="" header="0" hasMargin="0" protect="0" editable="0" dirty="0" borderFillIDRef="2"><hp:subList id="" textDirection="HORIZONTAL" lineWrap="BREAK" vertAlign="CENTER" linkListIDRef="0" linkListNextIDRef="0" textWidth="{width}" textHeight="{rowHeight}" hasTextRef="0" hasNumRef="0">{HwpxCellText(ref id, text, charPrId)}</hp:subList><hp:cellAddr colAddr="0" rowAddr="{rowIndex}"/><hp:cellSpan rowSpan="1" colSpan="1"/><hp:cellSz width="{width}" height="{rowHeight}"/><hp:cellMargin left="0" right="0" top="0" bottom="0"/></hp:tc></hp:tr>""";
    }

    private static string HwpxTemplateHeader(ref int id, PaperTemplateSettings? template, string sectionControls, bool pageBreak)
    {
        template ??= new PaperTemplateSettings();
        template.Normalize();
        var pageBreakValue = pageBreak ? "1" : "0";
        var title = Escape(template.BodyTitle);
        var subtitle = Escape(template.BodySubtitle);
        return $"""
<hp:p id="{id++}" paraPrIDRef="{HwpxTemplateTitleParaPrId}" styleIDRef="0" pageBreak="{pageBreakValue}" columnBreak="0" merged="0"><hp:run charPrIDRef="{HwpxTemplateTitleCharPrId}">{sectionControls}<hp:t>{title}</hp:t></hp:run></hp:p>
<hp:p id="{id++}" paraPrIDRef="{HwpxTemplateLeftParaPrId}" styleIDRef="0" pageBreak="0" columnBreak="0" merged="0"><hp:run charPrIDRef="{HwpxTemplateSubtitleCharPrId}"><hp:t>{subtitle}</hp:t></hp:run></hp:p>
""";
    }

    private static HwpxTableLayout HwpxTableMetrics(int cntPerPage, DocumentMarginSettings paperSettings, CellMarginSettings cellSettings, WorkCellSizeSettings workCellSettings, PaperTemplateSettings? template)
    {
        _ = cellSettings;
        template ??= new PaperTemplateSettings();
        template.Normalize();
        workCellSettings ??= new WorkCellSizeSettings();
        workCellSettings.Normalize();
        var margin = ExportLayout.From(paperSettings);
        var tableWidth = Math.Max(26000, HwpxPageWidth - margin.LeftHwpx - margin.RightHwpx);
        var labelWidth = Math.Clamp(MmToHwpxUnits(workCellSettings.WidthMm), 1800, tableWidth / 2);
        var imageWidth = Math.Max(18000, tableWidth - labelWidth);
        var titleHeight = Math.Max(900, MmToHwpxUnits(workCellSettings.HeightMm));
        var contentHeight = Math.Max(
            36000,
            HwpxPageHeight - margin.TopHwpx - margin.BottomHwpx - HwpxHeaderUnits - HwpxFooterUnits - (HwpxFixedOuterMarginUnits * 2) - HwpxTableVerticalSafetyUnits - HwpxTemplateHeaderHeightUnits(template));
        var rowHeight = Math.Max(9000, (contentHeight - titleHeight) / Math.Max(cntPerPage, 1));
        return new HwpxTableLayout(tableWidth, labelWidth, imageWidth, titleHeight, rowHeight);
    }

    private static int HwpxTemplateHeaderHeightUnits(PaperTemplateSettings template)
    {
        return PtToHwpxLineHeight(template.BodyTitleFontPt, template.LineSpacingPercent) +
            PtToHwpxLineHeight(template.BodySubtitleFontPt, template.LineSpacingPercent) +
            MmToHwpxUnits(10);
    }

    private static string InjectHwpxPageNumberControl(string sectionHead)
    {
        const string marker = "</hp:ctrl>";
        var insertPos = sectionHead.IndexOf(marker, StringComparison.Ordinal);
        if (insertPos < 0)
        {
            return sectionHead;
        }

        return sectionHead.Insert(insertPos + marker.Length, HwpxPageNumberControl());
    }

    private static string HideHwpxFirstPageNumber(string sectionHead)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            sectionHead,
            "hideFirstPageNum=\"(?:0|1)\"",
            "hideFirstPageNum=\"1\"",
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string ExtractHwpxSectionControls(string firstParagraph)
    {
        var paragraphStartEnd = firstParagraph.IndexOf('>');
        var paragraphClose = firstParagraph.LastIndexOf("</hp:p>", StringComparison.Ordinal);
        if (paragraphStartEnd < 0 || paragraphClose < 0 || paragraphStartEnd >= paragraphClose)
        {
            return string.Empty;
        }

        var controls = firstParagraph[(paragraphStartEnd + 1)..paragraphClose]
            .Replace("""<hp:run charPrIDRef="0"><hp:t/></hp:run>""", string.Empty, StringComparison.Ordinal)
            .Trim();
        const string runClose = "</hp:run>";
        var runOpenEnd = controls.IndexOf('>');
        if (controls.StartsWith("<hp:run", StringComparison.Ordinal) &&
            controls.EndsWith(runClose, StringComparison.Ordinal) &&
            runOpenEnd > 0)
        {
            controls = controls[(runOpenEnd + 1)..^runClose.Length];
        }

        return controls;
    }

    private static string HwpxTableRow(ref int id, int rowIndex, int rowHeight, int rowCount, IReadOnlyList<HwpxCell> cells)
    {
        var builder = new StringBuilder("<hp:tr>");
        foreach (var cell in cells)
        {
            var textWidth = Math.Max(0, cell.Width - cell.MarginLeft - cell.MarginRight);
            var textHeight = Math.Max(0, rowHeight - cell.MarginTop - cell.MarginBottom);
            var hasMargin = cell.HasMargin ? "1" : "0";
            var borderFillId = HwpxTableCellBorderFillId(rowIndex, rowCount, cell);
            builder.Append($"""<hp:tc name="" header="0" hasMargin="{hasMargin}" protect="0" editable="0" dirty="0" borderFillIDRef="{borderFillId}"><hp:subList id="" textDirection="HORIZONTAL" lineWrap="BREAK" vertAlign="CENTER" linkListIDRef="0" linkListNextIDRef="0" textWidth="{textWidth}" textHeight="{textHeight}" hasTextRef="0" hasNumRef="0">{cell.Content}</hp:subList><hp:cellAddr colAddr="{cell.Col}" rowAddr="{rowIndex}"/><hp:cellSpan rowSpan="1" colSpan="{cell.ColSpan}"/><hp:cellSz width="{cell.Width}" height="{rowHeight}"/><hp:cellMargin left="{cell.MarginLeft}" right="{cell.MarginRight}" top="{cell.MarginTop}" bottom="{cell.MarginBottom}"/></hp:tc>""");
        }
        builder.Append("</hp:tr>");
        return builder.ToString();
    }

    private static string HwpxTableCellBorderFillId(int rowIndex, int rowCount, HwpxCell cell)
    {
        var mask = 0;
        if (rowIndex == 0)
        {
            mask |= 1;
        }
        if (cell.Col + cell.ColSpan >= HwpxTableColumnCount)
        {
            mask |= 2;
        }
        if (rowIndex == rowCount - 1)
        {
            mask |= 4;
        }
        if (cell.Col == 0)
        {
            mask |= 8;
        }

        return (HwpxOuterTableBorderFillBaseId + mask).ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string ApplyHwpxPageMargins(string sectionHead, DocumentMarginSettings settings)
    {
        var margin = ExportLayout.From(settings);
        var pageMargin = $"""<hp:margin header="{HwpxHeaderUnits}" footer="{HwpxFooterUnits}" gutter="0" left="{margin.LeftHwpx}" right="{margin.RightHwpx}" top="{margin.TopHwpx}" bottom="{margin.BottomHwpx}"/>""";

        return System.Text.RegularExpressions.Regex.Replace(
            sectionHead,
            """<hp:margin\s+[^>]*/>""",
            pageMargin,
            System.Text.RegularExpressions.RegexOptions.None,
            TimeSpan.FromSeconds(1));
    }

    private static string HwpxCellText(ref int id, string text, string charPrId)
    {
        return $"""<hp:p id="{id++}" paraPrIDRef="{HwpxCenterParaPrId}" styleIDRef="0" pageBreak="0" columnBreak="0" merged="0"><hp:run charPrIDRef="{charPrId}"><hp:t>{Escape(text)}</hp:t></hp:run></hp:p>""";
    }

    private static string HwpxCellImage(ref int id, ExportImage image, int cellWidth, int rowHeight, CellLayout cell)
    {
        var size = HwpxImageSize(cellWidth, rowHeight, cell);
        var imageWidth = size.Width;
        var imageHeight = size.Height;
        var paragraphId = id++;
        var objectId = paragraphId + 100000;
        var centerX = imageWidth / 2;
        var centerY = imageHeight / 2;
        return $"""<hp:p id="{paragraphId}" paraPrIDRef="{HwpxCenterParaPrId}" styleIDRef="0" pageBreak="0" columnBreak="0" merged="0"><hp:run charPrIDRef="0"><hp:pic id="{objectId}" zOrder="0" numberingType="PICTURE" textWrap="TOP_AND_BOTTOM" textFlow="BOTH_SIDES" lock="0" dropcapstyle="None" href="" groupLevel="0" instid="{objectId}" reverse="0"><hp:offset x="0" y="0"/><hp:orgSz width="{imageWidth}" height="{imageHeight}"/><hp:curSz width="{imageWidth}" height="{imageHeight}"/><hp:flip horizontal="0" vertical="0"/><hp:rotationInfo angle="0" centerX="{centerX}" centerY="{centerY}" rotateimage="1"/><hp:renderingInfo><hc:transMatrix e1="1" e2="0" e3="0" e4="0" e5="1" e6="0"/><hc:scaMatrix e1="1" e2="0" e3="0" e4="0" e5="1" e6="0"/><hc:rotMatrix e1="1" e2="0" e3="0" e4="0" e5="1" e6="0"/></hp:renderingInfo><hp:imgRect><hc:pt0 x="0" y="0"/><hc:pt1 x="{imageWidth}" y="0"/><hc:pt2 x="{imageWidth}" y="{imageHeight}"/><hc:pt3 x="0" y="{imageHeight}"/></hp:imgRect><hp:imgClip left="0" right="{imageWidth}" top="0" bottom="{imageHeight}"/><hp:inMargin left="0" right="0" top="0" bottom="0"/><hp:imgDim dimwidth="{imageWidth}" dimheight="{imageHeight}"/><hc:img binaryItemIDRef="{Escape(image.HwpxId)}" bright="0" contrast="0" effect="REAL_PIC" alpha="0"/><hp:sz width="{imageWidth}" widthRelTo="ABSOLUTE" height="{imageHeight}" heightRelTo="ABSOLUTE" protect="0"/><hp:pos treatAsChar="1" affectLSpacing="0" flowWithText="0" allowOverlap="0" holdAnchorAndSO="0" vertRelTo="PARA" horzRelTo="PARA" vertAlign="CENTER" horzAlign="CENTER" vertOffset="0" horzOffset="0"/><hp:outMargin left="0" right="0" top="0" bottom="0"/></hp:pic></hp:run></hp:p>""";
    }

    private static DocumentImageSize HwpxImageSize(int cellWidth, int rowHeight, CellLayout cell)
    {
        return new DocumentImageSize(
            Math.Max(6000, cellWidth - cell.HorizontalHwpx * 2 - HwpxFixedPhotoOuterMarginUnits * 2),
            Math.Max(6000, rowHeight - cell.VerticalHwpx * 2 - HwpxFixedPhotoOuterMarginUnits * 2));
    }

    private static string HwpxParagraph(ref int id, string text, string paraPrId, string charPrId, bool pageBreak = false)
    {
        var pageBreakValue = pageBreak ? "1" : "0";
        var textXml = string.IsNullOrEmpty(text) ? "<hp:t/>" : $"<hp:t>{Escape(text)}</hp:t>";
        return $"""<hp:p id="{id++}" paraPrIDRef="{paraPrId}" styleIDRef="0" pageBreak="{pageBreakValue}" columnBreak="0" merged="0"><hp:run charPrIDRef="{charPrId}">{textXml}</hp:run></hp:p>""";
    }

    private static string HwpxPageNumberControl()
    {
        return """<hp:ctrl><hp:pageNum pos="BOTTOM_CENTER" formatType="DIGIT" sideChar=""/></hp:ctrl>""";
    }

    private static string HwpxImageParagraph(ref int id, ExportImage image)
    {
        const int imageSize = 17008;
        var paragraphId = id++;
        var objectId = paragraphId + 100000;
        var center = imageSize / 2;
        return $"""<hp:p id="{paragraphId}" paraPrIDRef="0" styleIDRef="0" pageBreak="0" columnBreak="0" merged="0"><hp:run charPrIDRef="0"><hp:pic textWrap="SQUARE" textFlow="BOTH_SIDES" reverse="0" id="{objectId}" zOrder="0" numberingType="PICTURE" lock="0" dropcapstyle="None" href="" groupLevel="0" instid="{objectId}"><hp:offset x="0" y="0"/><hp:orgSz width="{imageSize}" height="{imageSize}"/><hp:curSz width="{imageSize}" height="{imageSize}"/><hp:flip horizontal="0" vertical="0"/><hp:rotationInfo angle="0" centerX="{center}" centerY="{center}" rotateimage="1"/><hp:renderingInfo><hc:transMatrix e1="1" e2="0" e3="0" e4="0" e5="1" e6="0"/><hc:scaMatrix e1="1" e2="0" e3="0" e4="0" e5="1" e6="0"/><hc:rotMatrix e1="1" e2="0" e3="0" e4="0" e5="1" e6="0"/></hp:renderingInfo><hp:imgRect><hc:pt0 x="0" y="0"/><hc:pt1 x="{imageSize}" y="0"/><hc:pt2 x="{imageSize}" y="{imageSize}"/><hc:pt3 x="0" y="{imageSize}"/></hp:imgRect><hp:imgClip left="0" right="{imageSize}" top="0" bottom="{imageSize}"/><hp:inMargin left="0" right="0" top="0" bottom="0"/><hp:imgDim dimwidth="{imageSize}" dimheight="{imageSize}"/><hc:img binaryItemIDRef="{Escape(image.HwpxId)}" bright="0" contrast="0" effect="REAL_PIC" alpha="0"/><hp:effects/><hp:sz width="{imageSize}" height="{imageSize}" widthRelTo="ABSOLUTE" heightRelTo="ABSOLUTE" protect="0"/><hp:pos treatAsChar="1" affectLSpacing="0" flowWithText="1" allowOverlap="0" holdAnchorAndSO="0" vertRelTo="PARA" horzRelTo="COLUMN" vertAlign="TOP" horzAlign="LEFT" vertOffset="0" horzOffset="0"/><hp:outMargin left="0" right="0" top="0" bottom="0"/><hp:shapeComment/></hp:pic></hp:run></hp:p>""";
    }

    private static string HwpxPreviewText(AppState state)
    {
        var builder = new StringBuilder();
        builder.AppendLine(ExportDocumentTitle);
        foreach (var group in state.Groups)
        {
            builder.AppendLine(group.Title);
            foreach (var phase in new[] { Phase.Before, Phase.Processing, Phase.After })
            {
                var photos = group.Photos(phase);
                for (var i = 0; i < photos.Count; i++)
                {
                    builder.AppendLine($"{group.LabelAt(phase, i)} {Path.GetFileName(photos[i])}".TrimStart());
                }
            }
        }
        return builder.ToString();
    }

    private static string ImageContentType(string name)
    {
        return Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".tif" or ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };
    }

    private static string Escape(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    private static int MmToDocxTwips(int mm)
    {
        return (int)Math.Round(mm * DocxTwipsPerMillimeter);
    }

    private static int MmToHwpxUnits(int mm)
    {
        return (int)Math.Round(mm * HwpxUnitsPerMillimeter);
    }

    private static int PtToHwpxCharHeight(double pt)
    {
        return Math.Max(100, (int)Math.Round(pt * 100.0));
    }

    private static int PtToHwpxLineHeight(double pt, int lineSpacingPercent)
    {
        return Math.Max(800, (int)Math.Round(pt * 100.0 * lineSpacingPercent / 100.0));
    }


}
