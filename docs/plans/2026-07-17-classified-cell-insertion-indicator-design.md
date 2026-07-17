# Classified Cell Insertion Indicator Design

## Goal

Show the exact insertion position while dragging a classified cell and place the cell at the displayed position on drop.

## Interaction

- Show a blue vertical insertion line only while dragging a classified populated or empty cell over a classified cell collection.
- The left half of a cell selects the position immediately before that cell.
- The right half of a cell selects the position immediately after that cell.
- A collection with four cells has five valid positions: before the first cell, between each pair, and after the last cell.
- When a 대상 or 나머지 collection contains no cells, show one blue vertical line in the center and insert the dragged cell first.
- Hide the insertion line when the pointer leaves the target or the drag ends.
- Keep unclassified-photo drops unchanged.

## Rendering

- Add a non-layout-changing overlay line to each rendered classified cell slot.
- Align the line to the slot's left or right edge according to the pointer's horizontal half.
- Add a centered overlay line to an empty collection surface.
- Use the existing blue interaction color so the indicator matches other classified-area hover feedback.

## Movement

1. Convert the selected boundary into an insertion index from `0` through `cell count`.
2. Resolve and remove the complete source `PhotoCell`.
3. When source and target are the same collection, decrement the insertion index if the source was before the selected boundary.
4. Insert the source cell at the adjusted index.
5. Treat an unchanged effective position as a no-op.
6. Preserve the source cell's image, label, and other properties.

## Verification

- Verify insertion before the first and after the last cell.
- Verify every boundary between `전 / 중 / 후 / 빈`.
- Verify left-to-right and right-to-left movement in the same collection.
- Verify movement between 대상 and 나머지.
- Verify a centered indicator and first-cell insertion for an empty collection.
- Verify an empty classified cell uses the same indicator and movement rules.
- Verify unclassified photos still fill existing empty cells.
- Build the Avalonia application with zero errors.
