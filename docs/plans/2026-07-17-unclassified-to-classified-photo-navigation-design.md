# Unclassified to Classified Photo Navigation Design

## Goal

When the user clicks a gray, already-classified photo card in the unclassified area, reveal and briefly highlight the matching photo cell in the classified area without rebuilding either tree.

## Interaction

- Preserve the existing absolute-path status display for the clicked photo.
- Find the first rendered classified photo cell with the same normalized relative path.
- Scroll horizontally inside its work page when the photo cell is outside the visible range.
- Scroll the classified area vertically so the target cell's center is approximately 40% down the viewport.
- Show a blue border and pale-blue background on the matching photo cell, then restore its normal appearance within approximately 0.7 seconds.
- Do not change folder expansion, photo selection, or work-page selection state.
- If no rendered target exists, keep the path status update and otherwise do nothing.

## Implementation

1. Track rendered populated classified photo-card surfaces by normalized relative path during `RefreshRight`.
2. Extend the existing assigned-photo click handler to request classified-area navigation after updating the path status.
3. Use `BringIntoView` to reveal the target inside its horizontal photo scroller, then calculate the classified outer scroll offset so the card center sits at 40% of the viewport.
4. Apply the existing blue and pale-blue brush transition directly to the target photo-card surface and restore its normal white and gray styling after approximately 0.7 seconds.
5. Use the first rendered target if malformed metadata contains the same photo more than once.

## Verification

- Build the Avalonia application with zero errors.
- Verify an already-classified unclassified photo reveals the matching classified cell horizontally and vertically.
- Verify the target appears slightly above the viewport center and returns to normal styling within approximately 0.7 seconds.
- Verify the click still displays the absolute file path in the status bar.
- Verify the interaction does not redraw the classified area or change expansion and selection state.
- Verify clicking an unclassified, not-yet-assigned photo retains its existing selection behavior.
