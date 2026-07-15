# Ordered Blank Export Pages

## Goal

Allow users to insert one or more blank-page items among classified work groups. Blank-page items share the work-group ordering controls, and their positions produce blank HWPX and DOCX pages in the same sequence.

## Data Model

`PhotoGroup` gains an `isBlankPage` flag. A missing or false value represents an existing normal group, preserving backward compatibility. Blank-page items stay in `AppState.Groups`, so the existing list order, drag identifiers, removal, and persistence flow remain authoritative.

Blank-page items do not own photos, labels, titles, or per-page layout settings. They are excluded from assigned-photo counting and image collection.

## UI

- Add `빈페이지 추가` immediately before `공종 추가`.
- Each click appends one blank-page item.
- A blank-page row shows its sequence number, the label `빈페이지`, and a remove button.
- It does not show a title editor, photo-count selector, or phase columns.
- Blank-page rows participate in the same drag-to-reorder behavior as normal groups.
- The classified header displays `공종 N개 / 사진 K개 / 빈 페이지 M개`.
- Photos cannot be dropped onto a blank-page row.

## Export Ordering

The export page model represents either a normal table page or a blank page. `BuildExportPages` walks `AppState.Groups` in order:

- A normal group emits its existing one-or-more table pages.
- A blank-page item emits exactly one blank page.

DOCX and HWPX output emit a page break and an empty page body for blank pages. No configured title, subtitle, work-group title, table, label, or photo is rendered. Existing document-level page numbering remains active, so a blank page still shows its page number when page numbering is enabled.

The cover page remains unchanged. For `공종1 / 빈페이지 / 공종2`, the output after the cover is `공종1 table page(s) / blank page / 공종2 table page(s)`.

## Validation

The repository has no automated test project. Validation will cover metadata compatibility, item counts, every export-page branch, DOCX and HWPX structure, formatting checks, and Debug/Release builds.
