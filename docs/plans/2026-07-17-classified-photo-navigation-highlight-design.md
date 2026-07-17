# Classified Photo Navigation and Highlight Design

## Goal

When a user clicks a classified photo cell, reveal that photo in the unclassified area, scroll it into view, and animate it from the blue selected appearance back to the assigned gray appearance within approximately 0.7 seconds.

## Interaction

- A short left click on the photo image or empty card surface triggers navigation and highlighting.
- Clicking the image continues to open the existing image preview.
- Filename editing, cell-title editing, and cell control buttons do not trigger navigation.
- Pointer movement beyond the existing click threshold is treated as a drag and does not trigger navigation or preview.
- Existing expanded folders remain expanded.
- Actual photo selection and the selection action panel remain unchanged.

## Implementation

1. Extract the existing folder expansion, tree refresh, scroll, and highlight flow into a path-based helper that accepts one or more photo paths.
2. Keep group-number clicks using the helper with every scanned photo assigned to the group.
3. Call the helper from the classified photo card click flow with only the clicked photo path.
4. Expand the clicked photo's ancestor folder keys in both shared expansion sets.
5. After the tree is rendered, bring the matching assigned card into view and apply the existing blue-to-gray brush transition.
6. Preserve the image preview callback and avoid duplicate navigation when the image click bubbles through the card surface.

## Edge Cases

- Ignore empty or no-longer-scanned paths.
- A missing rendered target leaves the current scroll position unchanged.
- Rapid repeated clicks restart navigation and highlighting against the newly rendered card.
- Controls and editable text fields keep their existing behavior.

## Verification

- Build the Avalonia application with zero errors.
- Verify image and empty-card clicks reveal and highlight only the clicked photo.
- Verify filename, cell title, and control buttons do not trigger navigation.
- Verify image preview and drag behavior remain intact.
- Verify the card returns to gray within approximately 0.7 seconds without changing selection state.
