# Group Number to Unclassified Photo Navigation Design

## Goal

When the user clicks a group number in the classified area, reveal every folder that contains photos assigned to that group and scroll the unclassified area to the highest matching photo in tree order.

## Interaction

- A short left click on the group number triggers navigation.
- Existing expanded folders remain expanded.
- Every ancestor folder for each non-empty photo path in the group is expanded.
- After the unclassified tree is rebuilt, the scroll position moves to the first matching photo card in visual tree order.
- Blank pages and groups without photos do nothing.
- Hover insert controls and group drag behavior remain unchanged.

## Implementation

1. Collect normalized, non-empty image paths from the clicked `PhotoGroup`.
2. Derive every ancestor directory key used by the unclassified tree and add it to `expandedUnclassifiedPaths`. Keep `expandedExplorerPaths` synchronized without removing existing entries.
3. Refresh the left explorer and unclassified center area so newly expanded folders are rendered.
4. Keep references to the unclassified tree `ScrollViewer` and rendered photo cards.
5. On the UI dispatcher after layout, find the first matching card according to `visibleUnclassifiedPhotos` and call `BringIntoView()`.
6. Treat pointer movement beyond the existing click threshold as drag rather than navigation.

## Edge Cases

- Ignore empty image cells and paths not present in the latest scan.
- If assigned photos span multiple folders, expand all of those folder paths.
- If no matching card is rendered after refresh, leave the current scroll position unchanged.
- Do not alter photo selection state.

## Verification

- Build the Avalonia application with zero errors.
- Verify single-folder and multi-folder groups expand only required paths while preserving other expanded folders.
- Verify the first matching photo is brought into view.
- Verify empty groups and blank pages do not move the unclassified view.
- Verify the group-number hover insertion menu and group drag behavior still work.
