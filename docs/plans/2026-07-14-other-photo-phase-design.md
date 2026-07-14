# Other Photo Phase

## Goal

Extend classified photo groups from `Before / Processing / After` to `Before / Processing / After / Other`. Photos in `Other` remain fully classified inside SiteSnap but must never be included in HWPX or DOCX output.

## Phase Boundaries

The domain exposes two explicit phase sets:

- All phases: `Before`, `Processing`, `After`, `Other`
- Export phases: `Before`, `Processing`, `After`

UI, assignment, counting, path replacement, metadata sanitation, and preview use all phases. Export image collection, page construction, HWPX content, DOCX content, and export preview text use export phases only.

## Data Compatibility

`PhotoGroup` persists the new phase as `other` and `otherLabels`. Existing metadata files omit these properties and therefore load with empty lists. Sanitization initializes and normalizes the new lists without changing existing phase data.

## UI Behavior

- Classified groups render `전 / 중 / 후 / 나머지` in that order.
- `나머지` supports the same drag, drop, reorder, label edit, rename, and preview interactions as the existing phases.
- Other photos count as classified and do not appear as unclassified.
- The preview panel includes the `나머지` section.
- Export settings continue to describe and preview `전 / 중 / 후` because `나머지` is not exported.

## Export Contract

HWPX and DOCX generation must not collect, embed, list, or render `Other` photos. A group containing only `Other` photos produces no export page.

## Validation

The repository has no automated test project. Validation will inspect every phase iteration site, verify that application flows use all phases and export flows use export phases, run formatting checks, and build the application with zero warnings and errors.
