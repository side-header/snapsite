# SiteSnap

SiteSnap is a C# + Avalonia desktop app for sorting photos by work phase (`before / progress / after`) and exporting them as `docx` and `hwpx` documents.

## Development Run

Development runs require the .NET 10 SDK.

Run on macOS or Windows:

```bash
./scripts/run-avalonia.sh
```

## Build

Release publishing requires the .NET 10 SDK.

Publish for macOS:

```bash
./scripts/publish-macos-arm64.sh
```

Publish for Windows:

```bash
./scripts/publish-windows-x64.sh
```

On Windows PowerShell, you can also use:

```powershell
.\scripts\publish-windows-x64.ps1
```

## Run Published App

Publishing creates an OS-specific output folder.

macOS:

```bash
./build/macos-arm64/SiteSnap
```

Windows:

```text
build/windows-x64/SiteSnap.exe
```

The app is published with `--self-contained true`, so end-user PCs do not need a separate .NET Runtime or SDK installation. The development machine that runs publish still needs the .NET 10 SDK.

The Windows build is published as a single executable, so you can usually send only `build/windows-x64/SiteSnap.exe` to a Windows user. Extra files such as `.pdb` may be generated, but they are not required for normal execution.

## Workflow

1. Select the base folder with `폴더 열기`.
2. Create a work category with `공종 추가`.
3. Select photos from the unclassified photo area.
4. Use the `전`, `중`, and `후` buttons in the right-side group area to classify photos.
5. Save the working state to `sitesnape_manifest.json` with `저장하기`.
6. Export files as `exports/sitesnap-yyMMddHHmmss.docx` and `exports/sitesnap-yyMMddHHmmss.hwpx` with `내보내기`.

## Cache Location

SiteSnap stores generated photo thumbnails in a disk cache. The `캐시 클리어` menu removes the cache below.

| OS | Thumbnail cache path |
| --- | --- |
| macOS | `~/Library/Caches/SiteSnap/thumbnails` |
| Windows | `%LOCALAPPDATA%\\SiteSnap\\Cache\\thumbnails` |
| Linux/other | `$XDG_CACHE_HOME/SiteSnap/thumbnails` or `SiteSnap/Cache/thumbnails` under the local app-data folder |

## Manifest JSON

`sitesnape_manifest.json` is stored in the selected base folder and keeps the SiteSnap working state. It includes photo classification, export paper settings, and cover/photo-template text settings. When saved by the app, it is normalized to the structure below.

```json
{
  "rootDir": "/Users/example/photos",
  "groups": [
    {
      "id": "group-1",
      "title": "은행나무 암수교체",
      "cntPerPage": 3,
      "before": ["site/before-1.jpg"],
      "beforeLabels": ["전"],
      "processing": ["site/progress-1.jpg"],
      "processingLabels": ["중"],
      "after": ["site/after-1.jpg"],
      "afterLabels": ["후"]
    }
  ],
  "exportSettings": {
    "page3": {
      "hwpx": { "topMm": 10, "bottomMm": 10, "leftMm": 20, "rightMm": 20 },
      "docx": { "topMm": 10, "bottomMm": 10, "leftMm": 20, "rightMm": 20 },
      "hwpxCell": { "verticalMm": 0, "horizontalMm": 0 },
      "docxCell": { "verticalMm": 0, "horizontalMm": 0 },
      "hwpxPhoto": { "verticalMm": 6, "horizontalMm": 18 },
      "docxPhoto": { "verticalMm": 6, "horizontalMm": 18 },
      "hwpxWorkCell": { "heightMm": 18, "widthMm": 22 },
      "docxWorkCell": { "heightMm": 18, "widthMm": 22 }
    },
    "page4": {
      "hwpx": { "topMm": 10, "bottomMm": 10, "leftMm": 20, "rightMm": 20 },
      "docx": { "topMm": 10, "bottomMm": 10, "leftMm": 20, "rightMm": 20 },
      "hwpxCell": { "verticalMm": 0, "horizontalMm": 0 },
      "docxCell": { "verticalMm": 0, "horizontalMm": 0 },
      "hwpxPhoto": { "verticalMm": 6, "horizontalMm": 18 },
      "docxPhoto": { "verticalMm": 6, "horizontalMm": 18 },
      "hwpxWorkCell": { "heightMm": 18, "widthMm": 22 },
      "docxWorkCell": { "heightMm": 18, "widthMm": 22 }
    }
  },
  "paperTemplates": {
    "hwpx": {
      "title": "일  장  제  목",
      "titleFontPt": 37,
      "subtitle": "공사명 : 2000년 은행나무 가로수 교체공사(대한로)",
      "subtitleFontPt": 17,
      "company": "사 이 트 스 냅 대 표  홍 길 동",
      "companyFontPt": 22,
      "bodyTitle": "이 장 제 목",
      "bodyTitleFontPt": 23,
      "bodySubtitle": "공사명 : 2000년 은행나무 가로수 교체공사(대한로)",
      "bodySubtitleFontPt": 14,
      "lineSpacingPercent": 160,
      "fontFamily": "함초롬바탕",
      "showPageNumber": false
    },
    "docx": {
      "title": "일  장  제  목",
      "titleFontPt": 37,
      "subtitle": "공사명 : 2000년 은행나무 가로수 교체공사(대한로)",
      "subtitleFontPt": 17,
      "company": "사 이 트 스 냅 대 표  홍 길 동",
      "companyFontPt": 22,
      "bodyTitle": "이 장 제 목",
      "bodyTitleFontPt": 23,
      "bodySubtitle": "공사명 : 2000년 은행나무 가로수 교체공사(대한로)",
      "bodySubtitleFontPt": 14,
      "lineSpacingPercent": 80,
      "fontFamily": "함초롬바탕",
      "showPageNumber": false
    }
  }
}
```

| Path | Description |
| --- | --- |
| `rootDir` | Absolute path of the selected base folder. It is updated when a folder is opened in the app. |
| `groups[].id` | Internal group identifier. |
| `groups[].title` | Group title rendered in the right cell of the bottom `공종` row. |
| `groups[].cntPerPage` | Number of photos per photo-sheet page. Use `3` or `4`. |
| `groups[].before`, `processing`, `after` | Photo paths relative to the base folder. They are exported as the `전`, `중`, and `후` phases. |
| `groups[].beforeLabels`, `processingLabels`, `afterLabels` | Labels stored in the same order as each photo path. Empty labels show the gray `X` hint in the app and export as blank cells in documents. |
| `exportSettings.page3` | Paper and cell margin settings for groups with `cntPerPage` set to `3`. |
| `exportSettings.page4` | Paper and cell margin settings for groups with `cntPerPage` set to `4`. |
| `hwpx`, `docx` | Document paper margins. `topMm`, `bottomMm`, `leftMm`, and `rightMm` are in millimeters. |
| `hwpxCell`, `docxCell` | Internal margins of the photo table cell. `verticalMm` and `horizontalMm` are in millimeters. |
| `hwpxPhoto`, `docxPhoto` | Margins used to shrink the photo itself inside the cell. |
| `hwpxWorkCell`, `docxWorkCell` | Size of the bottom `"공종"` row. `heightMm` is the bottom-row height and `widthMm` is the left label-column width, both in millimeters. |
| `paperTemplates.hwpx`, `paperTemplates.docx` | Cover and photo-sheet text settings for HWPX and DOCX exports. Older manifests with `paperTemplate` are copied into both formats when first opened. |
| `paperTemplates.*.title`, `subtitle`, `company` | Cover-page title, subtitle, and company name. |
| `paperTemplates.*.titleFontPt`, `subtitleFontPt`, `companyFontPt` | Cover-page font sizes in points. |
| `paperTemplates.*.bodyTitle`, `bodySubtitle` | Title and subtitle rendered at the top of photo-sheet pages from page 2 onward. |
| `paperTemplates.*.bodyTitleFontPt`, `bodySubtitleFontPt` | Photo-sheet title and subtitle font sizes in points. |
| `paperTemplates.*.lineSpacingPercent` | Paragraph line-spacing percentage. For example, `160` means 160%. |
| `paperTemplates.*.fontFamily` | Font family used for cover and photo-sheet text. The default is `함초롬바탕`. |
| `paperTemplates.*.showPageNumber` | Whether page numbers are shown during export. |

## Documents

- [Requirements](docs/requirements.md)
- [Architecture](docs/architecture.md)
