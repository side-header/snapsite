# Classified Empty Cell Drag Design

## Goal

Allow empty classified cells to be dragged and moved with the same whole-cell semantics as classified photo cells.

## Interaction

- Both populated and empty classified cells are draggable.
- Dragging a classified cell moves its complete `PhotoCell`, including its label, image, and other properties.
- The source cell is inserted immediately before the destination cell.
- Movement works within the same collection and between 대상 and 나머지 collections.
- Dropping a cell onto itself is a no-op.
- Dropping into an empty collection places the source cell first.
- Existing unclassified-photo behavior remains unchanged: an unclassified photo fills an existing empty destination cell.
- Existing classified-photo-to-unclassified behavior remains available by reading the photo path from the classified-cell drag payload.

## Drag Data

- Add a classified-cell drag payload containing the source group ID, whether the source is in 나머지, the source cell index, and its photo path when present.
- Classified drop targets prioritize this payload and move the exact source cell by its source coordinates.
- Existing photo drag payloads remain for unclassified single-photo and multi-photo dragging.
- An empty classified cell has no photo path, so dropping it into the unclassified area has no effect.

## Domain Movement

1. Resolve the source collection from group ID and 대상/나머지.
2. Resolve the source cell by its drag-start index.
3. Retain the destination cell reference before removing the source.
4. Remove the source and resolve the destination's current index.
5. Insert the source immediately before the destination.
6. Restore the source to its original position if the destination cannot be resolved.

## Verification

- Move an empty cell left and right within the same collection.
- Move an empty cell between 대상 and 나머지 while preserving its label.
- Verify a self-drop changes nothing.
- Verify populated classified cells still move immediately before the destination.
- Verify a classified photo can still be returned to the unclassified area.
- Verify an unclassified photo still fills an existing empty classified cell.
- Build the Avalonia application with zero errors.
