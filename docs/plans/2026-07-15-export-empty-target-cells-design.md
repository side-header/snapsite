# Export Empty Target Cells Design

## Goal

Export target cells whose `image` is empty as real table rows with an empty photo area instead of omitting them from DOCX and HWPX output.

## Export Page Model

Every target cell becomes an `ExportPageItem` in array order. The page item always carries its label and optionally carries an `ExportImage`.

- Non-empty image: resolve and embed the image using the existing media pipeline.
- Empty image: keep the item and label but leave its image absent.
- Omit cells remain excluded regardless of image value.

Empty target cells count toward the group's `cntPerPage` limit. A group containing only empty target cells still creates table pages with the configured title and work-category row.

## DOCX and HWPX Rendering

Both formats retain the normal table border, label cell, photo-cell dimensions, and row height.

- A populated item renders the existing image content.
- An empty item renders an empty paragraph in the photo cell.
- Empty items do not create media files, DOCX relationships, or HWPX BinData records.
- Image-size calculations skip empty items.

HWPX preview text includes the empty item's label without a filename. Any fallback section generation follows the same label-only behavior.

## Persistence

The manifest schema and cell order do not change. Export remains read-only with respect to application state.

## Validation

Generate DOCX and HWPX fixtures containing populated, empty, and omit cells. Verify row counts and ordering, label presence, blank photo-cell content, page splitting with empty cells, media counts, omit exclusion, preview text, existing blank-page behavior, and Debug/Release builds.
