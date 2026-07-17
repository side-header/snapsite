# Assigned Unclassified Photo Status Design

## Goal

Remove the path-tree filename popup from the unclassified area and show the absolute path in the bottom status bar when a user clicks a gray photo that is already assigned to a classified group.

## Interaction

- No unclassified photo filename shows the path-tree hover popup.
- Clicking the image, read-only filename, or empty card surface of a gray assigned photo shows its absolute path, including the filename, in the bottom status bar.
- The gray photo is not added to the unclassified selection and its resting visual state does not change.
- Clicking the image continues to open the existing image preview.
- A drag gesture does not update the path.
- Normal unclassified photos continue to show their absolute path through the existing selection flow.
- The path remains until another application status message replaces it.

## Implementation

1. Remove the remaining `SetPhotoPathToolTip` call from unclassified photo filenames.
2. Reuse `ShowPhotoPathInStatus` for assigned gray cards.
3. Add short-click handling to assigned card surfaces while keeping them outside the selectable-photo collection.
4. Invoke the status helper from the assigned image preview callback and read-only filename click callback, preventing duplicate updates from event bubbling.
5. Preserve the existing movement threshold so drag-like pointer movement does not update status.

## Edge Cases

- Ignore empty paths or an empty root directory.
- Repeated clicks simply keep or refresh the same path text.
- Normal selection, Rule 1 selection, and assigned card styling remain unchanged.

## Verification

- Build the Avalonia application with zero errors.
- Verify no unclassified filename opens a path-tree popup.
- Verify assigned image, filename, and empty-card clicks show the absolute path.
- Verify assigned images still open preview and do not enter photo selection.
- Verify drag gestures do not update the path.
