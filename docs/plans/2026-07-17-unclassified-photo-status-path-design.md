# Unclassified Photo Status Path Design

## Goal

Show the full absolute path of the last clicked unclassified photo in the application's bottom status bar whenever normal photo selection is processed.

## Interaction

- Single selection shows the clicked photo's absolute path, including its filename.
- Control/Command toggle selection and Shift range selection show the last clicked photo's path.
- Clicking an already selected photo to deselect it still shows that photo's path.
- Clicking a photo while Rule 1 selection is active switches to normal selection and shows the clicked photo's path.
- Clicking outside photo cards to clear selection leaves the last path unchanged.
- Dragging, Rule 1 bulk selection, and internal automatic selection changes do not update the path.
- The path remains until another application status message replaces it.

## Implementation

1. Reuse the existing absolute-path status helper used by classified photo cells, renaming it so both areas can call it.
2. Invoke the helper at the beginning of `SelectUnclassifiedPhoto`, before selection-mode branching.
3. Keep path updates out of selection reconciliation, clearing, Rule 1 bulk selection, and drag operations.
4. Continue using `FileScanner.ToAbsolutePath` for platform-correct absolute paths.

## Edge Cases

- Ignore empty relative paths or an empty root directory.
- Selection results may be empty after a toggle; the clicked path is still shown.
- Other status messages continue to replace the path normally.

## Verification

- Build the Avalonia application with zero errors.
- Verify single, toggle, range, and deselect clicks show the last clicked photo path.
- Verify outside-click clearing and Rule 1 bulk selection do not replace the path.
- Verify classified photo status-path behavior remains unchanged.
