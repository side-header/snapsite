# Move Classified Cell Into Empty Destination Design

## Goal

When an already-classified photo cell is dragged into an empty classified destination, move the complete source cell and replace the empty destination instead of moving only the photo path.

## Interaction

- Remove the source `PhotoCell` from its original collection.
- Replace the empty destination `PhotoCell` with that same source cell object.
- Preserve the source photo path, label, and all other cell properties.
- Apply the rule between target and omit collections, between work groups, and within one collection.
- Keep populated-destination behavior unchanged: insert the complete source cell immediately after the populated destination.
- Keep unclassified-to-classified behavior unchanged: an unclassified photo still fills an empty destination while retaining the destination label.
- Continue highlighting the moved destination photo cell for approximately 0.7 seconds.

## Implementation

1. Extend `MoveAssignedPhotoCellBeside` so an empty destination is applicable when the source photo already belongs to a classified cell.
2. Hold a reference to the destination cell, remove the source cell, and find the destination's updated index.
3. Replace the empty destination at that index with the source cell; otherwise insert the source after a populated destination.
4. Preserve rollback to the source's original index if the destination cannot be resolved after removal.
5. Continue falling back to `PlacePhotoAt` when the source photo is not already classified.

## Verification

- Build the Avalonia application with zero errors.
- Verify target-to-omit movement replaces the omit empty cell and removes the complete source cell.
- Verify the source label moves with the photo.
- Verify omit-to-target and cross-work-group empty-cell movement.
- Verify populated-destination movement still inserts after the destination.
- Verify unclassified photos still fill empty cells without replacing their predefined labels.
