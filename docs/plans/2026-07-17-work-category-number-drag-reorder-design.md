# Work Category Number Drag Reorder Design

## Goal

Restore work-category reordering when a user drags the category number onto another classified category while preserving the existing short-click navigation behavior.

## Interaction

- A short click on the category number continues to reveal that category's photos in the unclassified area.
- Pointer movement of at least the existing 6-pixel threshold starts the existing group reorder drag.
- Dropping onto another category keeps using `MoveGroupTo`, persistence, and targeted classified-area refresh behavior.
- Buttons, combo boxes, images, titles, photo cells, and category insertion controls retain their current interactions.

## Implementation

Register the group reorder press and release tracking on the tunnel route. This lets the containing category row observe number-originated pointer input before the number's click handler marks the bubbling event handled. Keep the existing movement threshold, drag payload, drop targets, and reorder state mutation unchanged.

## Verification

- Build the application in Debug and Release configurations.
- Verify the event path supports both a short number click and a number-originated drag.
- Confirm no automated UI test project currently covers Avalonia pointer routing, so no unrelated tests are added.
