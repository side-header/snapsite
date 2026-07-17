# SiteSnap Requirements

## Purpose

SiteSnap is a desktop app for organizing construction-site photos and exporting photo-sheet documents. A user opens a base folder, arranges labeled photo cells in exported `target` or excluded `omit` lists, and exports the result as either HWPX or DOCX.

The app is developed with C# and Avalonia UI. It targets macOS and Windows as a self-contained desktop application, without a web server, installer requirement, background service, or database.

## Runtime Model

- The user starts the published executable directly.
- The app state is stored in `sitesnape_manifest.json` inside the selected base folder.
- Original photo files are not moved, copied, renamed, or deleted by classification.
- Saved photo paths are relative to the selected base folder and normalized with `/`.
- Exported documents are written under an `exports` folder inside the selected base folder.
- Thumbnail cache files are stored in the OS app-cache location, not inside the selected base folder.

## Base Folder Scanning

- The user selects the base folder with `폴더 열기`.
- The app scans files recursively up to three folder levels in `FileScanner`.
- Supported image extensions are `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.tif`, and `.tiff`.
- Folder and non-image file metadata is available for explorer display.
- `exports` and `.newgreen-cache` are ignored during scanning.
- If the base folder contains no photos, the unclassified area shows the empty-state message after the folder is opened.

## Main Screen

The main screen is composed of these working areas:

- Top menu bar
- Optional explorer area on the left
- Unclassified photo area
- Classified work-category area
- Optional preview area on the right
- Status bar

The explorer and preview areas can be toggled from the top menu. The central working area uses split panes so the user can adjust visible widths.

## Top Menu

- `ⓘ`: opens the program information dialog.
- `폴더 열기`: selects and scans the base folder.
- `종이 설정`: opens the paper and text-template settings dialog.
- `내보내기`: opens the export menu.
  - `.hwpx 로 내보내기`: exports only HWPX.
  - `.docx 로 내보내기`: exports only DOCX.
- `저장하기`: saves the current manifest. While the save feedback is visible, the button text is `저장 중`.
- Left-side panel icon: toggles the explorer area.
- Right-side panel icon: toggles the preview area.

The `공종 페이지 추가` action belongs to the classified-area header and is visible only after a folder is opened.

## Program Information Dialog

- The information button is placed to the left of `폴더 열기`.
- The dialog shows the program version and thumbnail cache location.
- The dialog includes a `캐시 클리어` button below the cache location so the user can clear the thumbnail cache manually.
- The program version is hard-coded as `v0.1.3`.
- The dialog content is shown as a compact table and closes with an `확인` button.

## Unclassified Photo Area

- The area is hidden until a base folder is opened.
- It displays photos that are not assigned to any work category.
- Photos are grouped in a tree-like folder layout rooted at the selected base folder name.
- The text `사진 파일` is not shown.
- If photos exist, the area shows guidance telling the user to drag photos to the right area or drag classified photos back to exclude them.
- If no unclassified photos remain, it shows `분류되지 않은 사진이 없습니다.`
- The zoom controls are shown only after a folder is opened.
- Zoom-in and zoom-out are limited to five steps in each direction.

## Classified Area

- The area is hidden until a base folder is opened.
- Its header contains `빈 페이지 추가`, `공종 페이지 추가`, zoom-out, and zoom-in controls.
- If a folder is open and there are no work categories, it shows `아직 공종이 없습니다. 공종 페이지 추가를 눌러 시작하세요.` in gray.
- `공종 페이지 추가` creates a new empty work category.
- `빈 페이지 추가` creates an item with an empty title and empty target and omit lists. This shape is treated as a blank page and can be reordered and removed like a category.
- The blank-page title guide reads `빈 페이지로 출력됩니다.`. Entering a title changes the zero-cell item into a normal category.
- A blank page uses the normal category-row layout and shows one-slot-width empty target and omit drop areas without persisting placeholder cells.
- Dropping one or more photos into either blank-page area creates empty-label cells only in that collection and changes the item into a normal category.
- The header separately counts work categories, classified photos, and blank pages.
- Each category row displays:
  - row number
  - editable category name
  - per-page photo count selector
  - remove button
  - ordered `target` and `omit` photo cells
- The remove button is shown as an `X` control and styled in red.
- A category name placeholder is shown in gray when empty.
- A new category contains empty `전`, `중`, and `후` target cells and an empty omit list.
- Empty target and omit labels show no watermark or guide character.
- An empty classified cell keeps its editable label and shows a white photo-card box without an image placeholder.
- Hovering a target photo or empty card shows centered bottom `−`, `←`, and `→` controls without changing the row height.
- Hovering an omit photo or empty card shows only the centered bottom `−` control.
- `−` removes the exact cell and unclassifies its photo if present; target `←` and `→` insert an empty unlabeled target cell immediately beside it.
- Hover controls do not initiate photo dragging, preview, or filename editing.
- A single-photo drop fills the exact cell when its image is empty; an occupied drop cell inserts a new unlabeled cell immediately to its right.
- Moving a photo between classified cells clears its source image while retaining the source cell and label.
- Omit cells count as classified and appear in the category preview, but are excluded from HWPX and DOCX exports.
- Photos can be reordered or moved by drag and drop.
- A normal click selects one unclassified photo and sets the range anchor.
- Shift-click replaces the selection with every visible selectable photo between the anchor and clicked photo.
- Ctrl/Command-click toggles one photo, while Ctrl/Command+Shift-click adds an anchored visible range to the existing selection.
- Assigned photos and photos inside collapsed folders are excluded from range selection.
- When at least two photos are selected, green top-right badges show their 1-based selection order.
- Dragging any selected card moves the ordered multi-selection as one operation.
- When multiple photos are dropped on an exact empty target cell, empty target cells from the drop position to the right are filled in selection order, skipping occupied cells.
- Target overflow fills existing empty omit cells from left to right and then appends empty-label omit cells.
- An exact empty omit-cell drop fills empty omit cells from that position to the right and appends any remainder to omit.
- When multiple photos are dropped on an exact occupied cell, every selected photo is inserted immediately to its right.
- Reused empty cells retain their labels, and occupied-cell insertions keep selection order with empty labels.
- Multi-photo drops on a target or omit area background reuse empty cells and append remaining photos only within that destination collection.
- Group headers reject photo drops because they do not identify a target or omit destination; group reorder dragging remains available.
- A photo can belong to only one cell at a time.
- Dragging a classified photo back to the unclassified area removes its entire cell and label. Deleted default cells are not recreated after saving or reopening.
- Zoom-in and zoom-out are limited to five steps in each direction.

## Photo Cards and Path Tooltip

- Photo cards use cached thumbnails.
- The displayed photo label is based on the file name.
- The path tooltip appears only when the mouse is over the file-name text.
- The tooltip is positioned below the file-name text.
- The tooltip shows a tree-style relative path rooted at the selected base folder.
- The tooltip remains available while the mouse is over the file-name text or over the tooltip itself.
- Long tooltip content can scroll horizontally.
- The tooltip text is selectable.

## Preview Area

- The preview area can be toggled from the top menu.
- When no work category is selected, it shows `공종을 선택해주세요` at the top-left.
- When a category is selected, it previews the selected category's photos and phase layout.
- The preview area does not replace the classified area. It is a side panel.

## Paper Settings Dialog

The `종이 설정` dialog contains three top-level tabs:

- `공통`
- `페이지당 3장`
- `페이지당 4장`

### Common Tab

- The common tab opens by default when the dialog is opened.
- The common tab has a format selector in the upper-right area.
- `한글` is selected by default.
- `한글` and `MS Word` text settings are stored independently.
- The common tab edits:
  - paragraph line spacing
  - font family
  - page-number visibility
  - photo DPI from `72` to `600`, defaulting to `300`
  - JPEG photo quality from `0` to `100`, defaulting to `85`
  - first-page title, subtitle, company text, and font sizes
  - page-2-onward title, subtitle, and font sizes
- The preview is informational. It shows text placement and approximate sizing, but final exported layout can differ.
- The default HWPX line spacing is `160%`.
- The default DOCX line spacing is `80%`.

### Page Count Tabs

- `페이지당 3장` and `페이지당 4장` configure export layout per page count.
- Each page-count tab shows a left preview and a right comparison table.
- The table has separate HWPX and DOCX columns.
- The settings include:
  - paper margins: top, bottom, left, right
  - photo outside margins: vertical and horizontal
  - work-cell size: bottom-row height and left label-column width
- Table-internal cell margins are normalized to zero by code.
- Inputs use millimeters.
- The default paper margins are `20mm` on all sides.
- The default photo outside margins are `6mm` vertical and `18mm` horizontal.
- The default work-cell size is `18mm` height and `22mm` width.

### Reset and Save

- `기본 값으로 되돌리기` restores default values for HWPX, DOCX, page-3, and page-4 settings.
- The save button writes the normalized settings to `sitesnape_manifest.json`.
- Settings cannot be saved until a base folder is selected.

## Manifest JSON

The manifest stores:

- `appVersion`
- `rootDir`
- `groups`
- ordered `target` and `omit` cells containing image paths and labels
- `cntPerPage`
- `exportSettings.page3`
- `exportSettings.page4`
- `exportSettings.hwpxImageDpi`
- `exportSettings.docxImageDpi`
- `exportSettings.hwpxJpegQuality`
- `exportSettings.docxJpegQuality`
- `paperTemplates.hwpx`
- `paperTemplates.docx`

Legacy fields are normalized on load:

- Missing `appVersion` is written as the current app version on the next save.
- Old single-format export settings are migrated into `page3` and `page4`.
- Legacy cell-margin objects are normalized to the current shape.
- Old `paperTemplate` content is copied into both `paperTemplates.hwpx` and `paperTemplates.docx` when no split template exists.
- Old `before`, `processing`, `after`, and `other` photo/label arrays are migrated to `target` and `omit` cells.
- After save, legacy fields are omitted from the normalized manifest.

## Saving

- `저장하기` writes the current app state to `sitesnape_manifest.json`.
- Saving validates that the selected base folder still exists.
- The file is written through a temporary file and moved into place.
- Missing, duplicate, ignored, or deleted photo paths are removed during sanitization.
- Empty, invalid, or duplicate image paths are cleared without deleting their cells or labels.

## Export

- The user exports one format at a time from the `내보내기` menu.
- Default export names use `sitesnap-yyMMddHHmmss`.
- HWPX and DOCX exports use their own paper-template settings.
- Each work category starts on a new page.
- Photos from different categories are never mixed on the same page.
- Every target cell is exported in array order.
- An empty target image renders its label and a same-sized empty photo cell without creating image media.
- Omit cells are not embedded, listed, or rendered in exported documents.
- A page contains up to the category's `cntPerPage` target cells, including empty-image cells.
- If a category has more target cells than `cntPerPage`, it continues on the next page.
- Each exported page uses one table.
- The top header contains the configured title and subtitle.
- The bottom row contains the `공종` label and category title.
- Table borders are black.
- If page numbers are enabled, DOCX uses a centered footer and HWPX uses a centered footer control.
- Each item whose title, target list, and omit list are all empty emits exactly one output page at its ordered position.
- Blank pages omit the configured title, subtitle, work-category title, table, labels, and photos while retaining document-level page numbering.

## Thumbnail Cache

- The app does not decode all original images at full size for card display.
- Thumbnails are decoded to a fixed thumbnail width and cached on disk.
- Cache keys include the absolute path, last-write time, and file size.
- The thumbnail cache is also cleared automatically when the program exits.

Cache locations:

| OS | Path |
| --- | --- |
| macOS | `~/Library/Caches/SiteSnap/thumbnails` |
| Windows | `%LOCALAPPDATA%\SiteSnap\Cache\thumbnails` |
| Linux/other | `$XDG_CACHE_HOME/SiteSnap/thumbnails` or `SiteSnap/Cache/thumbnails` under the local app-data folder |

## Non-Functional Requirements

- The app is implemented in C#.
- The UI framework is Avalonia UI.
- The target SDK is .NET 10.
- The app must publish for macOS ARM64 and Windows x64.
- The app must not require a web server, database, or external background process.
- Exported HWPX should prioritize compatibility with Hancom viewers.
- The documentation language is English, while user-facing Korean UI labels are preserved as literal labels.
