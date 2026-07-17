# Classified Photo Status Path Design

## Goal

Remove the path-tree hover popup from classified photo filenames and show the clicked photo's full absolute path in the application's bottom status bar instead.

## Interaction

- Classified photo filenames no longer open a path-tree popup on hover.
- Unclassified photo filename path popups remain unchanged.
- A short click on a classified photo image or empty card surface shows the photo's absolute path, including its filename, in the bottom status bar.
- The existing folder expansion, scrolling, highlight animation, and image preview behaviors remain unchanged.
- Filename editing, cell-title editing, control buttons, dragging, and empty cells do not update the path status.
- The path remains visible until another application status message replaces it.

## Implementation

1. Remove `SetPhotoPathToolTip` only from the classified `AssignedPhotoCard` filename.
2. Add a small helper that converts a classified photo's relative path into an absolute path through `FileScanner.ToAbsolutePath` and assigns it to `status.Text`.
3. Call the helper from the classified image click callback and classified card-surface click handler.
4. Keep the helper out of editable filename, title, button, drag, and empty-cell paths.

## Edge Cases

- Ignore empty relative paths.
- If no root directory is open, leave the current status unchanged.
- Use the existing filesystem path conversion so separators and normalization match the host platform.
- Other status messages continue to replace the path normally.

## Verification

- Build the Avalonia application with zero errors.
- Verify classified filename hover no longer shows the tree popup.
- Verify image and card-surface clicks show the full absolute path.
- Verify unclassified filename hover behavior remains unchanged.
- Verify editing, controls, dragging, and empty cells do not alter the status.
