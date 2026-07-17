# Move Classified Photo Cell Design

## Goal

When a classified photo cell A is dragged onto another populated classified photo cell B, move the complete A cell to the position immediately after B instead of leaving an empty cell at A's former position.

## Interaction

- Remove A's complete `PhotoCell` from its original collection.
- Insert that same cell immediately after populated destination cell B.
- Preserve the photo path, cell label, and all other cell properties.
- Support movement within one collection, between target and omit collections, and between work groups.
- Correct the destination index when A and B belong to the same collection and A was originally before B.
- Treat dropping A onto itself as a no-op.
- Keep the current behavior when B is empty.
- Keep the existing unclassified-to-classified assignment behavior unchanged.
- Continue highlighting the moved destination photo cell for approximately 0.7 seconds after rendering.

## Implementation

1. Add an `AppState` operation that locates the source `PhotoCell` and destination collection by normalized photo path and target coordinates.
2. Use the cell-move operation only when the dragged photo is already classified and B contains a different photo.
3. Remove the source cell, adjust the target index when both cells share a collection, and insert the original cell after B.
4. Fall back to the existing `PlacePhotoAt` behavior for unclassified sources and empty destinations.
5. Reuse the existing post-drop refresh and destination highlight path.

## Verification

- Build the Avalonia application with zero errors.
- Verify A moved after B leaves no empty cell at A's former position.
- Verify A's label moves with its photo.
- Verify same-collection moves work in both directions without an off-by-one error.
- Verify movement across work groups and between target and omit collections.
- Verify self-drop is a no-op.
- Verify an unclassified photo drop and a drop into an empty cell retain their existing behavior.
