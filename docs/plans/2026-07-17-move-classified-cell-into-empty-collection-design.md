# Move Classified Cell Into Empty Collection Design

## Goal

When an already-classified photo cell is dragged into a classified target or omit area whose underlying cell collection is empty, move the complete source cell so its label is not left behind.

## Cause

An empty collection has no destination `PhotoCell` and uses index `-1`. The current complete-cell move rejects that destination, so the fallback placement clears only the source image and creates a new image-only cell. The source label therefore remains at the old position.

## Interaction

- Remove the complete source `PhotoCell` from its original collection.
- Add that same cell as the first item in the empty destination collection.
- Preserve the source image, label, and all other cell properties.
- Keep existing complete-cell replacement for an empty destination cell.
- Keep existing insertion after a populated destination cell.
- Keep unclassified-photo placement into an empty collection unchanged.
- Continue highlighting the moved destination photo cell for approximately 0.7 seconds.

## Implementation

1. Treat `targetCells.Count == 0` with destination index `-1` as a valid complete-cell move destination.
2. Resolve the classified source cell before falling back to photo-only placement.
3. Remove the source cell and append the same object to the empty destination collection.
4. Retain the current rollback, self-drop, empty-cell replacement, and populated-cell insertion behavior.

## Verification

- Build the Avalonia application with zero errors.
- Verify moving a `후` cell into an empty omit collection moves both `후` and its photo.
- Verify no empty `후` cell remains in the source collection.
- Verify moving into an existing empty cell and after a populated cell still works.
- Verify an unclassified photo can still be placed into an empty collection.
