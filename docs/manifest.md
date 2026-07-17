# Manifest JSON

`sitesnape_manifest.json` is stored in the selected base folder and keeps the SiteSnap working state. It includes photo classification, export paper settings, and cover/photo-template text settings. When saved by the app, it is normalized to the structure below.

```json
{
  "appVersion": "0.1.4",
  "rootDir": "/Users/example/photos",
  "groups": [
    {
      "id": "group-1",
      "title": "은행나무 암수교체",
      "target": [
        { "image": "site/before-1.jpg", "label": "전" },
        { "image": "site/progress-1.jpg", "label": "중" },
        { "image": "site/after-1.jpg", "label": "후" }
      ],
      "omit": [
        { "image": "site/reference.jpg", "label": "" }
      ],
      "cntPerPage": 3
    }
  ],
  "exportSettings": {
    "hwpxImageDpi": 300,
    "docxImageDpi": 300,
    "hwpxJpegQuality": 85,
    "docxJpegQuality": 85,
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
| `appVersion` | SiteSnap version that last wrote the manifest. Older manifests may not have this field until saved again. |
| `rootDir` | Absolute path of the selected base folder. It is updated when a folder is opened in the app. |
| `groups[].id` | Internal group identifier. |
| blank-page group shape | A group is exported as a blank page when `title` is empty and both `target` and `omit` are empty arrays. Older `isBlankPage: true` entries are migrated to this shape and the legacy field is removed on the next save. |
| `groups[].title` | Group title rendered in the right cell of the bottom `공종` row. |
| `groups[].cntPerPage` | Number of photos per photo-sheet page. Use `3` or `4`. |
| `groups[].target[]` | Ordered cells rendered during export. An entry with an empty `image` becomes a labeled table row with an empty photo cell. |
| `groups[].omit[]` | Ordered classified cells excluded from HWPX and DOCX exports. |
| `groups[].target[].image`, `groups[].omit[].image` | Photo path relative to the base folder. An empty string represents an empty cell. |
| `groups[].target[].label`, `groups[].omit[].label` | Editable cell label. A move within classified groups retains the source cell label; unclassifying the photo removes its entire cell and label. |
| `exportSettings.hwpxImageDpi`, `exportSettings.docxImageDpi` | DPI used when resizing photos for HWPX and DOCX exports. Values are clamped from `72` to `600`; the default is `300`. |
| `exportSettings.hwpxJpegQuality`, `exportSettings.docxJpegQuality` | JPEG quality used when encoding resized photos for HWPX and DOCX exports. Values are clamped from `0` to `100`; the default is `85`. |
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

New groups start with empty `전`, `중`, and `후` target cells and an empty omit array. Older manifests using `before`, `processing`, `after`, `other`, and their label arrays are migrated when loaded; an explicit old other label is preserved, while a missing one defaults to an empty string. The next save writes only `target` and `omit`.

Dropping onto an empty cell updates that cell's `image` and preserves its label and position. New cells are created only after the applicable empty cells have been used.

Dragging a classified photo back to the unclassified area removes its entire cell. Saved `target` and `omit` arrays may therefore be empty and are not repopulated when reopened.

The classified-card `−` control removes the selected array entry; when it contained a photo, that photo becomes unclassified. Target-card `←` and `→` controls persist new adjacent target entries with empty `image` and `label` values; omit cards do not expose insertion controls.

An ordered multi-photo drop on an occupied exact cell inserts every photo immediately to its right in the same array. From an empty target cell, right-side empty target entries are reused first, followed by empty omit entries and then new omit entries. From an empty omit cell, right-side empty omit entries are reused before new omit entries are appended. Reused labels are preserved.

An ordered multi-photo drop on a target or omit area background also stays entirely in that array. Empty entries are reused in order and remaining photos append as unlabeled cells. Group headers do not accept photo drops.
