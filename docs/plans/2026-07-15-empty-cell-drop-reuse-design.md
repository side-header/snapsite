# Empty Cell Drop Reuse Design

## Goal

When a photo is dropped onto a classified cell whose `image` is empty, reuse that cell by setting its `image` instead of inserting another cell. Apply the same empty-cell-first rule to single-photo and ordered multi-photo drops.

## Single-Photo Placement

- Dropping onto an exact empty target or omit cell sets that cell's `image`.
- Dropping onto an occupied cell keeps the existing behavior and inserts a new unlabeled cell immediately to its right.
- Moving a photo clears its previous cell image while retaining the previous cell and label.

## Multi-Photo Placement

An exact empty drop cell has first priority: the first selected photo fills it.

For the remaining photos:

- If the group had no photos when the drop began, reuse empty target cells in target order, then reuse empty omit cells, then append new unlabeled omit cells.
- If the group had any photo when the drop began, reuse empty omit cells in omit order, then append new unlabeled omit cells.
- A cell already filled earlier in the same operation is skipped.
- Selection order is preserved and the selection is cleared after a successful drop.

For a group-level multi-photo drop without an exact cell, the same rules apply without the exact-cell priority. A new group therefore fills `전`, `중`, `후`, and `나머지` before creating additional omit cells.

## Persistence and Export

The manifest schema remains unchanged. Reused cells retain their existing labels and positions. Export continues to include non-empty target cells only and exclude omit cells.

## Validation

Verify single drops on empty and occupied cells, exact-cell multi drops, empty-group automatic placement, existing-group omit placement, source-cell retention, manifest persistence, and Debug/Release builds.
