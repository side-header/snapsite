# Target/Omit Cells and Ordered Multi-Select

## Goal

Replace phase-specific photo and label arrays with ordered `target` and `omit` cell lists. Add Shift-based ordered multi-selection in the unclassified area, visible order badges, multi-photo drag payloads, and group-level automatic placement.

## Cell Model

Each cell stores only:

```json
{ "image": "relative/path.jpg", "label": "cell label" }
```

New normal groups start with three target cells labeled `전`, `중`, and `후`, plus one omit cell labeled `나머지`. Blank-page items keep empty target and omit lists.

Target cells with images are exported in array order. Omit cells count as classified but are never exported. Empty cells are never exported.

Dropping one photo on a cell inserts a new cell immediately to its right with an empty label. Removing or moving a photo clears only the source cell image and preserves its cell and label.

## Legacy Migration

On load, manifests containing `before`, `processing`, `after`, and `other` arrays are migrated once:

- The first photo and label of each phase populate that phase's base cell.
- Additional photos follow the base cell in their existing order.
- Missing photos retain an empty base cell and the existing first label or default Korean label.
- The other phase becomes the omit list.
- Saving writes only target and omit fields; legacy phase fields are omitted.

## Ordered Selection

- Normal click replaces the selection with one unclassified photo.
- Shift-click appends a photo or removes an already selected photo.
- Remaining selection indices are compacted after removal.
- Two or more selected photos show green numeric badges at the top-right of each card.
- Shift-click does not open preview or filename editing.
- Dragging a selected card moves the full ordered selection. Dragging an unselected card moves only that photo.

## Placement

Single-photo drops insert immediately after the exact target or omit cell.

For a multi-photo drop on a group:

- If the group has no non-empty image cells, photos 1, 2, and 3 are inserted after the target cells that were at indices 0, 1, and 2 when the drop began. Remaining photos append to omit.
- If any non-empty image cell exists, every selected photo appends to omit.
- Cell labels do not affect placement.
- Selection clears after a successful drop.

## Persistence and Export

Assigned counts, rename propagation, file sanitation, and unclassification traverse target and omit images. DOCX and HWPX image collection and page construction traverse non-empty target cells only. Existing blank-page ordering and page-number behavior remain unchanged.

## Validation

The repository has no automated test project. A temporary external validation script will verify legacy migration, target/omit serialization, selection-independent placement rules, unclassification behavior, and DOCX/HWPX exclusion of omit and empty cells. Formatting checks and Debug/Release builds complete validation.
