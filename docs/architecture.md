# SiteSnap Architecture

## Overview

SiteSnap is a cross-platform desktop application built with C#, .NET 10, and Avalonia UI. The user-facing product name is SiteSnap. The historical project folder and namespace still use `NewGreen` internally.

The app scans a selected base folder, keeps classification state in memory, saves normalized JSON state to `sitesnape_manifest.json`, and exports photo-sheet documents as HWPX or DOCX.

## Project Layout

```text
src/NewGreen.App/
  SiteSnap.App.csproj
  Program.cs
  App.cs
  app.manifest
  Resources/
    base.hwpx
  Domain/
    AppState.cs
    ExportPageSettings.cs
    ExportSettings.cs
    LayoutSettings.cs
    PaperTemplateSettings.cs
    Phase.cs
    PhotoGroup.cs
    ScannedFiles.cs
  Infrastructure/
    Export/
      DocumentExporter.cs
      DocumentExporter.Models.cs
    FileSystem/
      FileScanner.cs
    Persistence/
      MetadataStore.cs
    Thumbnails/
      ThumbnailService.cs
  UI/
    MainWindow.cs
    MainWindow.Photos.cs
    MainWindow.Settings.cs
```

Supporting scripts:

```text
scripts/run-avalonia.sh
scripts/publish-macos-arm64.sh
scripts/publish-windows-x64.sh
scripts/publish-windows-x64.ps1
```

## Application Startup

`Program.cs` starts the Avalonia desktop lifetime. `App.cs` configures the Avalonia application. `MainWindow` builds the UI in code rather than XAML.

The app uses one main window and keeps the active `AppState`, current `ScanResult`, selected group, selected photo, zoom levels, and panel visibility in the window instance.

## Domain Model

### AppState

`AppState` is the root persisted state.

```csharp
public sealed class AppState
{
    public string RootDir { get; set; }
    public List<PhotoGroup> Groups { get; set; }
    public ExportSettings ExportSettings { get; set; }
    public PaperTemplateSettings? PaperTemplate { get; set; }
    public PaperTemplateFormatSettings? PaperTemplates { get; set; }
}
```

`PaperTemplate` is a legacy JSON field. `NormalizePaperTemplates()` migrates it into `PaperTemplates` and clears the legacy field before saving.

### PhotoGroup

`PhotoGroup` stores one work category.

- `Id`
- `Title`
- `Before`
- `BeforeLabels`
- `Processing`
- `ProcessingLabels`
- `After`
- `AfterLabels`
- `CntPerPage`

Photos are stored as base-folder-relative paths. Labels are stored separately but normalized to stay aligned with each phase photo list.

### Phase

`Phase` has three values:

- `Before`, key `before`, label `전`
- `Processing`, key `processing`, label `중`
- `After`, key `after`, label `후`

### ExportSettings

`ExportSettings` stores layout settings by page count.

```text
exportSettings
  page3
    hwpx
    docx
    hwpxCell
    docxCell
    hwpxPhoto
    docxPhoto
    hwpxWorkCell
    docxWorkCell
  page4
    ...
```

`DocumentMarginSettings` stores paper margins in millimeters. `CellMarginSettings` stores vertical and horizontal photo margins in millimeters. `WorkCellSizeSettings` stores the bottom work row height and left label-column width.

Current normalization rules:

- Paper margins clamp to `0..100`.
- Photo margins clamp to `0..100`.
- Work-cell dimensions clamp to `5..80`.
- Table-internal cell margins are normalized to zero.
- Page-3 and page-4 settings are both present after normalization.
- Legacy single-format and legacy pixel/mm margin fields are migrated on load.

### PaperTemplateSettings

`PaperTemplateSettings` stores text and typography for cover and photo-sheet headers.

`PaperTemplateFormatSettings` stores separate templates:

- `hwpx`
- `docx`

The HWPX default line spacing is `160%`. The DOCX default line spacing is `80%`.

## Persistence

`MetadataStore` owns JSON load and save.

- File name: `sitesnape_manifest.json`
- Location: selected base folder
- Serialization: `System.Text.Json` with indented output
- Save strategy: write a temporary file, delete the existing manifest if present, then move the temporary file into place

Load flow:

1. If the manifest does not exist, create a new `AppState` with `RootDir`.
2. Deserialize JSON into `AppState`.
3. Replace `RootDir` with the currently selected folder.
4. Normalize paper templates.
5. Normalize export settings.
6. Sanitize groups and photo paths.

Sanitization removes ignored paths, missing files, and duplicate photo paths. It also normalizes `CntPerPage` to `3` or `4` and aligns labels with photo lists.

## File Scanning

`FileScanner` scans the selected base folder.

- Maximum folder depth is currently `3`, controlled by `MaxFolderDepth`.
- Ignored top-level directory names are `exports` and `.newgreen-cache`.
- Supported photo extensions are `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.tif`, and `.tiff`.
- Results are sorted case-insensitively by relative path.

The scanner returns a `ScanResult` with separate lists for photos, folders, and other files.

## Thumbnail Cache

`ThumbnailService` creates and caches photo thumbnails.

- Thumbnail decode width: `180`
- Cache file extension: `.png`
- Cache key input: absolute path, last-write timestamp, and file size
- In-memory cache: `Dictionary<string, Bitmap?>`
- Concurrent load guard: `HashSet<string>`

Cache roots:

| OS | Root |
| --- | --- |
| macOS | `~/Library/Caches/SiteSnap` |
| Windows | `%LOCALAPPDATA%\SiteSnap\Cache` |
| Linux/other | `$XDG_CACHE_HOME/SiteSnap` or local app-data fallback |

Thumbnails are stored under the `thumbnails` child folder. `ClearCache()` clears memory state and deletes that folder recursively.

## UI Architecture

`MainWindow` is split into three partial files:

- `MainWindow.cs`: shell layout, top menu, panels, state loading, save, export, dialogs
- `MainWindow.Photos.cs`: photo cards, drag and drop, preview, path tooltip, image interactions
- `MainWindow.Settings.cs`: paper settings dialog, form controls, layout previews

### Top Bar

The top bar contains the information button, folder-open button, paper settings, export menu, save button, cache clear, and side-panel toggles.

The save button reflects temporary save feedback by changing from `저장하기` to `저장 중`.

### Main Layout

The central grid can show:

```text
explorer | splitter | unclassified photos | splitter | classified area | splitter | preview
```

The explorer and preview columns can collapse to zero width through their toggle buttons. The unclassified and classified areas remain the primary workflow surface after a folder is opened.

### Explorer Area

The explorer is implemented with Avalonia controls. It renders a folder-first tree using the current scan result. Folder expansion state is tracked in `expandedExplorerPaths`.

The app does not embed native Finder or Windows Explorer controls.

### Unclassified Area

The unclassified area builds a tree rooted at the selected base folder name. It contains only photos that are not currently assigned to any group. Folder expansion state is tracked separately in `expandedUnclassifiedPaths`.

Drag targets allow classified photos to be dropped back into the unclassified area for removal.

### Classified Area

The classified area renders one row per `PhotoGroup`.

The header actions are hidden until a folder is opened. After opening a folder, the header shows:

- `공종 추가`
- zoom out
- zoom in

Zoom levels are clamped by `PhotoScaleLimit`, currently `5` steps in either direction.

### Preview Area

The preview panel is optional. If no group is selected, it displays `공종을 선택해주세요` at the top-left. When a group is selected, it displays the group preview.

### Photo Cards

Photo cards load thumbnails through `ThumbnailService`. They support drag and drop, click selection, preview, and path tooltips.

The file-path tooltip is anchored to the file-name text, not the entire card. It displays a relative tree path rooted at the selected base folder and stays open while the pointer is over the file name or tooltip content.

## Paper Settings UI

`MainWindow.Settings.cs` builds the paper settings dialog.

Top-level tabs:

- `공통`
- `페이지당 3장`
- `페이지당 4장`

The common tab owns `PaperTemplateSettings`. It has a format selector for:

- `한글`
- `MS Word`

The selector defaults to `한글`. Each format has its own input set and saves into `paperTemplates.hwpx` or `paperTemplates.docx`.

The page-count tabs own `ExportPageSettings`. They show a left-side preview and a right-side comparison table with HWPX and DOCX columns.

The reset button writes default values into both format templates and both page-count settings.

## Export Architecture

`DocumentExporter` is a partial class with export implementation and private layout models.

Public methods:

- `ExportAll(rootDir, state)`
- `ExportDocxOnly(rootDir, state)`
- `ExportHwpxOnly(rootDir, state)`
- `ExportDocxTo(rootDir, outputPath, state)`
- `ExportHwpxTo(rootDir, outputPath, state)`

The UI calls the single-format methods from the export menu. `ExportAll` remains available for compatibility or internal use.

Default output names use:

```text
sitesnap-yyMMddHHmmss.docx
sitesnap-yyMMddHHmmss.hwpx
```

### Common Export Page Model

DOCX and HWPX share the same page-splitting model:

```text
PhotoGroup
  -> flatten before, processing, after photos
  -> preserve per-photo phase labels
  -> split by CntPerPage
  -> create ExportPage objects
```

Each group starts on a new page. Different groups do not share a page.

### DOCX Generation

DOCX is generated directly with `ZipArchive` and OpenXML XML strings.

Main package entries:

```text
[Content_Types].xml
_rels/.rels
word/document.xml
word/_rels/document.xml.rels
word/styles.xml
word/footer1.xml
word/media/*
```

`footer1.xml` is included only when page numbers are enabled for the DOCX template.

DOCX generation uses:

- `paperTemplates.docx` for cover/header text and line spacing
- `exportSettings.page3/page4.docx` for paper margins
- `docxPhoto` for photo shrink margins
- `docxWorkCell` for bottom work-row sizing

Images are resized to JPEG entries in `word/media`.

### HWPX Generation

HWPX generation starts from `Resources/base.hwpx`, writes it to the output path, then updates selected package entries.

Updated entries:

```text
Contents/content.hpf
Contents/header.xml
Contents/section0.xml
Preview/PrvText.txt
BinData/*
```

HWPX generation uses:

- `paperTemplates.hwpx` for cover/header text and line spacing
- `exportSettings.page3/page4.hwpx` for paper margins
- `hwpxPhoto` for photo shrink margins
- `hwpxWorkCell` for bottom work-row sizing

The exporter injects or updates HWPX paragraph styles, character styles, black table border fills, page margins, footer controls, and image references. Images are re-encoded as JPEG and stored under `BinData`.

## Build and Publish

Development run:

```bash
./scripts/run-avalonia.sh
```

macOS ARM64 publish:

```bash
./scripts/publish-macos-arm64.sh
```

Windows x64 publish:

```bash
./scripts/publish-windows-x64.sh
```

Windows PowerShell publish:

```powershell
.\scripts\publish-windows-x64.ps1
```

The scripts publish `src/NewGreen.App/SiteSnap.App.csproj`.

## Verification

Recommended checks:

- Run `dotnet build src/NewGreen.App/SiteSnap.App.csproj`.
- Publish with the target OS script when packaging is changed.
- For export changes, inspect generated DOCX/HWPX ZIP entries and key XML files.
- For UI changes, run the Avalonia app and verify the relevant workflow manually.

## Dependency Policy

Normal NuGet restore and build operations are part of development. Dedicated vulnerability scanning is not part of the default documentation update flow unless explicitly requested.
