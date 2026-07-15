# Contiguous Multi-Photo Cell Drop Design

## Goal

When an ordered multi-photo selection is dropped on an exact classified cell, place all selected photos contiguously beside that cell instead of routing them to omit because the group already contains a photo.

## Exact Cell Placement

The drop cell's target or omit collection and current index determine placement. Existing photos elsewhere in the group do not affect the result.

- Occupied drop cell: insert every selected photo in selection order immediately to its right.
- Empty drop cell: fill it with the first selected photo, then insert the remaining photos immediately to its right in selection order.
- Every inserted cell has an empty label.
- A target-cell drop keeps every selected photo in target.
- An omit-cell drop keeps every selected photo in omit.

Moving a path clears any previous classified source image while retaining that source cell and label, consistent with existing classified-to-classified moves.

## Other Multi-Photo Drop Targets

Group headers and collection background areas without an exact cell keep the existing automatic placement behavior. The contiguous rule applies only when the drop handler has an exact cell index.

## UI State

After a successful exact-cell multi-drop, clear the ordered selection, save once, refresh once, and select the destination group.

## Persistence and Export

The manifest schema does not change. Contiguous target cells export in their new array order. Omit cells remain excluded.

## Validation

Verify occupied and empty target-cell drops, occupied and empty omit-cell drops, selection order, empty inserted labels, source-cell clearing, unaffected group-level automatic placement, persistence, export ordering, and Debug/Release builds.
