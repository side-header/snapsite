# Inferred Blank Page Design

## Goal

Represent an export-only blank page without a dedicated manifest flag. An ordered group is a blank page when its title is empty and both `target` and `omit` are empty arrays:

```json
{
  "title": "",
  "target": [],
  "omit": []
}
```

## Data model and compatibility

Remove `isBlankPage` from the current manifest model and saved JSON. Add one shared derived predicate that identifies a blank page from the empty title and empty cell collections. Existing manifests that contain `isBlankPage: true` remain readable: deserialization accepts the legacy field long enough to normalize that item to an empty title, empty target, and empty omit, then omits the field on the next save.

The `빈페이지 추가` action remains. It creates an ordered group with an empty title and empty cell collections. Blank pages continue to share the same group list and reorder behavior as normal work types.

## UI behavior

Keep the compact white blank-page row. Replace its fixed `빈페이지` text with an editable title whose empty-state guide reads `빈 페이지로 출력됩니다.`. When the user enters a title, the item no longer satisfies the blank-page predicate and is displayed as a normal work type with empty target and omit collections; cells are not recreated automatically.

The classified-area summary continues to count derived blank pages separately from normal work types.

## Export behavior

DOCX and HWPX export use the shared derived predicate. A derived blank page emits no title and no table, but retains the normal page number. A titled item with empty target and omit collections is treated as a normal work type rather than a blank page.

## Validation

- A newly added blank page saves as an empty title with empty `target` and `omit`, without `isBlankPage`.
- A legacy `isBlankPage: true` item loads as the derived blank-page shape and is rewritten without the flag.
- Entering a title changes the item to a normal work type without adding cells.
- Clearing the title of a zero-cell item changes it back to a blank page.
- DOCX and HWPX retain page numbering while omitting the title and table for derived blank pages.
