# Empty Cell Multi-Drop Fill Design

## Goal

When multiple unclassified photos are dropped on an empty classified cell, reuse empty cells to the right before creating new cells, and route target overflow into omit.

## Placement rules

For an empty target drop cell:

1. Walk target cells from the drop index to the end.
2. Skip occupied cells and fill empty cells in array order with the selected photos.
3. Fill remaining photos into existing empty omit cells from left to right.
4. Append any further photos to omit as new cells with empty labels.

Reused target and omit cells retain their existing labels. Photos preserve selection order.

For an empty omit drop cell, walk empty omit cells from the drop index to the end, then append any remainder to omit with empty labels.

Dropping multiple photos on an occupied target or omit cell keeps the existing behavior: insert all selected photos contiguously immediately to the right in the same collection. Single-photo placement and collection-background placement do not change.

## Validation

- Empty target cells to the right are reused even when occupied cells appear between them.
- Target overflow fills existing omit cells and then appends to omit.
- Empty omit-cell drops reuse right-side omit cells and append overflow.
- Reused labels and photo selection order are preserved.
- Occupied-cell multi-drop behavior remains unchanged.
- Manifest persistence, formatting, Debug/Release builds, and application startup remain clean.
