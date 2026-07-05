# SiteSnap Requirements

## Purpose

SiteSnap is a desktop app for organizing construction-site photos and exporting photo-sheet documents. A user opens a base folder, classifies photos into work categories, assigns each photo to the `before`, `processing`, or `after` phase, and exports the result as either HWPX or DOCX.

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
- `캐시 클리어`: clears the SiteSnap thumbnail cache.
- Left-side panel icon: toggles the explorer area.
- Right-side panel icon: toggles the preview area.

The `공종 추가` action belongs to the classified-area header and is visible only after a folder is opened.

## Program Information Dialog

- The information button is placed to the left of `폴더 열기`.
- The dialog shows the program version and thumbnail cache location.
- The program version is hard-coded as `v0.1.1`.
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
- Its header contains `공종 추가`, zoom-out, and zoom-in controls.
- If a folder is open and there are no work categories, it shows `아직 공종이 없습니다. 공종 추가를 눌러 시작하세요.` in gray.
- `공종 추가` creates a new empty work category.
- Each category row displays:
  - row number
  - editable category name
  - per-page photo count selector
  - remove button
  - `before`, `progress`, and `after` photo columns
- The remove button is shown as an `X` control and styled in red.
- A category name placeholder is shown in gray when empty.
- A category can contain photos in `전`, `중`, and `후` phases.
- Photos can be reordered or moved by drag and drop.
- A photo can belong to only one category and one phase at a time.
- Dragging a classified photo back to the unclassified area removes it from classification.
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

- `rootDir`
- `groups`
- per-phase photo paths and labels
- `cntPerPage`
- `exportSettings.page3`
- `exportSettings.page4`
- `paperTemplates.hwpx`
- `paperTemplates.docx`

Legacy fields are normalized on load:

- Old single-format export settings are migrated into `page3` and `page4`.
- Legacy cell-margin objects are normalized to the current shape.
- Old `paperTemplate` content is copied into both `paperTemplates.hwpx` and `paperTemplates.docx` when no split template exists.
- After save, legacy fields are omitted from the normalized manifest.

## Saving

- `저장하기` writes the current app state to `sitesnape_manifest.json`.
- Saving validates that the selected base folder still exists.
- The file is written through a temporary file and moved into place.
- Missing, duplicate, ignored, or deleted photo paths are removed during sanitization.
- Phase labels are kept aligned with photo paths.

## Export

- The user exports one format at a time from the `내보내기` menu.
- Default export names use `sitesnap-yyMMddHHmmss`.
- HWPX and DOCX exports use their own paper-template settings.
- Each work category starts on a new page.
- Photos from different categories are never mixed on the same page.
- Photos are ordered by phase: `전`, `중`, `후`.
- A page contains up to the category's `cntPerPage` value.
- If a category has more photos than `cntPerPage`, it continues on the next page.
- Each exported page uses one table.
- The top header contains the configured title and subtitle.
- The bottom row contains the `공종` label and category title.
- Table borders are black.
- If page numbers are enabled, DOCX uses a centered footer and HWPX uses a centered footer control.

## Thumbnail Cache

- The app does not decode all original images at full size for card display.
- Thumbnails are decoded to a fixed thumbnail width and cached on disk.
- Cache keys include the absolute path, last-write time, and file size.
- `캐시 클리어` clears only the SiteSnap thumbnail cache.

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
