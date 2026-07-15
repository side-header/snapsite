# Multi-Photo Drop Stays in Collection Design

## Goal

An ordered multi-photo selection dropped in a classified target or omit area stays entirely in that collection instead of being split by group-level automatic placement.

## Collection Background Placement

The destination collection is determined by the target or omit area receiving the drop.

- Reuse empty cells in collection order, preserving their labels.
- Append any remaining photos as new cells at the end of the same collection.
- New cells use empty labels.
- Preserve selection order.
- Existing photos elsewhere in the group do not affect placement.

No photo crosses from target to omit or from omit to target during this operation.

## Exact Cell Placement

Exact-cell multi-photo drops keep the existing contiguous placement behavior: fill an exact empty cell with the first photo or anchor on an occupied cell, then insert the remaining photos immediately to its right in the same collection.

## Group Header

The group header does not identify a target or omit destination and therefore rejects photo drops. Group reorder dragging remains enabled and unchanged.

## UI State

After a successful collection drop, clear the ordered selection, select the destination group, save once, and refresh once. Rejected header photo drops do not change state.

## Persistence and Export

The manifest schema does not change. Target photos export in their resulting array order; omit photos remain excluded.

## Validation

Verify target and omit background drops with empty and occupied cells, selection order, retained and new labels, zero-cell collections, exact-cell behavior, rejected group-header photo drops, group reorder behavior, persistence, export behavior, and Debug/Release builds.
