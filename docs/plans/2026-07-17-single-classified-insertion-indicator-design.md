# Single Classified Insertion Indicator Design

## Goal

Ensure each classified target or omit collection displays exactly one blue insertion line while a cell is dragged.

## Problem

The current UI creates one insertion indicator per cell. At a shared boundary, the previous cell's right indicator and the next cell's left indicator can remain visible together, making one insertion position look like two lines.

## Interaction

- Keep the existing cell spacing and table layout.
- Treat the right edge of one cell and the left edge of the next cell as the same insertion position.
- Show one blue vertical line for the active insertion position.
- Continue supporting positions before the first cell, between cells, and after the last cell.
- Keep the centered insertion line for a collection with no cells.
- Hide the line when the pointer leaves the collection or the drop completes.

## Rendering

- Create one shared insertion indicator overlay for each 대상 or 나머지 collection.
- Remove per-cell insertion indicators.
- Compute the boundary coordinate from `insertion index * slot width`.
- Position the shared indicator at that coordinate without changing layout measurements.
- Clamp the first and last indicator positions inside the collection bounds so the full line remains visible.

## Drop Behavior

- Retain the current left-half and right-half insertion-index calculation.
- Retain the current domain movement and same-collection index adjustment.
- Do not change unclassified-photo drop behavior.

## Verification

- Approach the boundary between two cells from either side and verify only one line is visible.
- Verify the line uses the same coordinate from both adjacent cells.
- Verify positions before the first and after the last cell.
- Verify the centered line for an empty collection.
- Verify drop results remain aligned with the displayed line.
- Build the Avalonia application with zero errors.
