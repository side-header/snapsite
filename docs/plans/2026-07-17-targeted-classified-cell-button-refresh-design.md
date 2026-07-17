# Targeted Classified Cell Button Refresh Design

## Goal

Avoid full application refreshes when using a classified photo cell's remove, insert-left-empty, or insert-right-empty controls.

## Classified Group Refresh

- Capture the affected group's targeted-refresh snapshot before changing its cells.
- After the mutation, replace only that group's `GroupRow`.
- Preserve the classified area's vertical scroll and the group's horizontal photo scroll.
- Leave every unrelated classified group control untouched.
- Refresh the detail panel only when it displays the affected group.

## Empty Cell Insertion

- Insert the empty target cell at the requested index.
- Refresh only the affected classified group.
- Do not refresh the explorer or unclassified-photo area.
- Keep the existing debounced automatic save.

## Cell Removal

- Remove the selected cell from 대상 or 나머지.
- Refresh only the affected classified group.
- If the removed cell contains a photo, replace that photo's rendered gray assigned card with one selectable unclassified card at the same visual position.
- Update folder classified-count labels and panel header counts without rebuilding the folder tree.
- If the rendered card cannot be found, refresh only the unclassified area and restore its prior scroll offset.
- Removing an already-empty cell does not change the unclassified-photo area.

## Registries

- Reuse the existing targeted classified-group registry cleanup and replacement.
- When replacing one unclassified card, remove the old rendered-card and selectable-card registry entries before creating the replacement.
- Reconcile selection visuals and folder status counts after the replacement.

## Verification

- Remove a populated target cell and verify only its group and one unclassified card change.
- Remove a populated omit cell with the same result.
- Remove an empty cell without refreshing the unclassified area.
- Insert empty cells on the left and right and verify only the affected group changes.
- Verify classified vertical and group horizontal scroll positions remain stable.
- Verify folder classified counts, panel headers, detail view, and debounced persistence remain correct.
- Build the Avalonia application with zero errors.
