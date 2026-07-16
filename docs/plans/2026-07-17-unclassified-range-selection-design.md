# Unclassified Range Selection Design

## Goal

Use standard desktop selection semantics in the unclassified photo area: Shift selects a visible range, while Ctrl or Command toggles individual photos.

## Selection model

Track the ordered paths of currently visible, selectable unclassified photo cards and one transient range anchor. Neither is persisted in the manifest.

- Plain click replaces the selection with the clicked photo and makes it the anchor.
- Ctrl/Command-click toggles only the clicked photo and makes it the anchor.
- Shift-click replaces the selection with the visible selectable range between the anchor and clicked photo.
- Ctrl/Command+Shift-click adds that range to the existing selection without duplicating paths.

Range paths are stored from the anchor toward the clicked photo, so green selection badges and multi-photo drag order follow the user's range direction. Assigned photos, photos under collapsed folders, and other cards not currently rendered as selectable are excluded.

If no valid visible anchor exists, Shift-click behaves as a plain single selection. When the unclassified tree is rebuilt, remove selections that are no longer visible and clear an anchor that is no longer visible.

Modifier-based clicks suppress image preview and filename editing. Plain clicks retain the existing preview and editing behavior.

## Components

- `MainWindow` owns the transient visible-photo order and range-anchor path alongside `selectedPhotos`.
- `PhotoCard` records each rendered selectable photo in visible order.
- One selection function accepts the complete `KeyModifiers` value and applies plain, toggle, range-replace, or range-add behavior.
- Existing selection visuals and drag payload generation continue to consume `selectedPhotos`.

## Validation

Cover forward and reverse Shift ranges, Ctrl/Command toggles, additive Ctrl/Command+Shift ranges, missing anchors, invisible selections, selection ordering, and duplicate prevention. Run formatting, Debug/Release builds, the focused selection harness, existing target/omit regression checks, and application startup.
