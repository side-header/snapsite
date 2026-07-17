# SiteSnap Architecture

## Overview

SiteSnap is a cross-platform desktop application built with C#, .NET 10, and Avalonia UI. The codebase uses a multi-project DDD structure with explicit Domain, Application, Infrastructure, and Presentation boundaries.

The app scans a selected base folder, keeps classification state in memory, saves normalized JSON state to `sitesnape_manifest.json`, and exports photo-sheet documents as HWPX or DOCX.

## Project Layout

```text
src/
  SiteSnap.Domain/          # aggregates, entities, value/settings models
  SiteSnap.Application/     # use cases and infrastructure ports
  SiteSnap.Infrastructure/  # scanning, persistence, DOCX/HWPX adapters
  SnapSite.App/             # Avalonia Presentation and composition root
    Presentation/
      MainWindow/
      Services/             # Avalonia-specific thumbnail service
tests/
  SiteSnap.CharacterizationTests/
SiteSnap.slnx
```

Project references enforce this direction:

```text
SiteSnap.App ───────► SiteSnap.Application ───────► SiteSnap.Domain
      │                       ▲
      └────────────► SiteSnap.Infrastructure ──────┘
```

`App.cs` is the composition root. Presentation code consumes Application services; concrete file-system, persistence, and export adapters are created only at startup.

Supporting scripts:

```text
scripts/run-avalonia.sh
scripts/publish-macos-arm64.sh
scripts/publish-windows-x64.sh
scripts/publish-windows-x64.ps1
```

## Application Startup

`Program.cs` starts the Avalonia desktop lifetime. `App.cs` configures Avalonia and injects `WorkspaceService` and `DocumentExportService` into `MainWindow`. `MainWindow` builds the UI in code rather than XAML.

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
- derived blank-page state (`Title` empty and both cell lists empty)
- `Target`
- `Omit`
- `CntPerPage`

`Target` and `Omit` are ordered `PhotoCell` lists. Each cell owns an `Image` base-folder-relative path and a `Label`. Empty image strings preserve empty cells and their labels.

New groups start with three target cells labeled `전`, `중`, and `후` and an empty omit list. After creation, normal groups may persist any target or omit cell count, including zero; normalization does not recreate deleted cells. An item with an empty title and empty target and omit lists is treated as a blank page, and blank pages share the ordered `Groups` list. The UI renders those empty collections as target and omit drop areas without creating placeholder cells; the first drop creates cells in only the chosen collection and turns the item into a normal group.

### Legacy Manifest Migration

`PhotoGroup.MigrateLegacyCells()` converts older `before`, `processing`, `after`, and `other` photo/label arrays to `Target` and `Omit`. Legacy properties are cleared after conversion and omitted on the next save.

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

- Maximum folder depth is currently `7`, controlled by `MaxFolderDepth`.
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

`MainWindow` remains a Presentation partial class split into three behavior-preserving files:

- `Presentation/MainWindow/MainWindow.cs`: shell layout, top menu, panels, state loading, save, export, dialogs
- `Presentation/MainWindow/MainWindow.Photos.cs`: photo cards, drag and drop, preview and image interactions
- `Presentation/MainWindow/MainWindow.Settings.cs`: paper settings dialog, form controls, layout previews

Avalonia-specific thumbnail caching stays in Presentation because its public API returns `Bitmap`. File scanning, JSON persistence, and document export are Infrastructure adapters behind Application interfaces.

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

Drag targets allow classified photos to be dropped back into the unclassified area. This removes the entire matching target or omit cell, including its label, and empty cell arrays remain empty after saving and reopening.

### Classified Area

The classified area renders one row per `PhotoGroup`.

The header actions are hidden until a folder is opened. After opening a folder, the header shows:

- `빈 페이지 추가`
- `공종 페이지 추가`
- zoom out
- zoom in

Zoom levels are clamped by `PhotoScaleLimit`, currently `5` steps in either direction.

### Preview Area

The preview panel is optional. If no group is selected, it displays `공종을 선택해주세요` at the top-left. When a group is selected, it displays the group preview.

### Photo Cards

Photo cards load thumbnails through `ThumbnailService`. They support drag and drop, Shift range selection, Ctrl/Command toggle selection, preview, and path tooltips. The UI records the visible selectable card order and a transient range anchor; forward or reverse ranges update the ordered selection that is encoded in a prefixed JSON drag payload for multi-photo moves. Hidden and assigned photos are reconciled out when the unclassified tree is rebuilt.

Classified cells with an empty image render a white border-only photo card. The omit collection background remains `#F7F9FA`, the editable label remains above the card, empty target and omit labels show no watermark, and no image placeholder mark is rendered inside it.

Hovering a target card shows a centered bottom overlay with `−`, `←`, and `→`; hovering an omit card shows only `−`. Removal uses `AppState.RemoveCell`, while target-side arrows call `AppState.InsertEmptyCell` for the immediately adjacent index. All controls share stable hover resources, and button presses are excluded from photo drag initiation.

For a single-photo drop, `AppState.PlacePhotoAt` fills the exact drop cell when its image is empty; an occupied drop cell receives a new unlabeled cell immediately to its right. An exact-cell multi-photo drop uses `AppState.PlacePhotosBesideCell`. From an empty target cell, it fills empty target cells at or to the right of the drop index, skips occupied cells, then sends overflow through existing empty omit cells before appending new omit cells. From an empty omit cell, it fills right-side empty omit cells and appends the remainder there. An occupied cell remains the anchor for the existing behavior: all selected photos are inserted contiguously immediately to its right in the same collection. Collection background drops use `AppState.PlacePhotosInCollection`, reuse empty cells in that collection, and append the remainder there. Group headers reject photo drops because they do not identify a target or omit destination, while group reorder drops remain enabled.

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
  -> empty title + empty target/omit: create one blank ExportPage
  -> normal group: flatten every target cell in array order
     -> exclude omit cells
     -> preserve each target cell label
     -> render an empty photo cell when image is empty
     -> split by CntPerPage
     -> create table ExportPage objects
```

Each group starts on a new page. Different groups do not share a page. Blank pages preserve their list position, render no title or table content, and inherit document-level page numbering.

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

The scripts publish `src/SnapSite.App/SiteSnap.App.csproj`.

## Verification

Recommended checks:

- Run `dotnet build src/SnapSite.App/SiteSnap.App.csproj`.
- Publish with the target OS script when packaging is changed.
- For export changes, inspect generated DOCX/HWPX ZIP entries and key XML files.
- For UI changes, run the Avalonia app and verify the relevant workflow manually.

## Dependency Policy

Normal NuGet restore and build operations are part of development. Dedicated vulnerability scanning is not part of the default documentation update flow unless explicitly requested.
