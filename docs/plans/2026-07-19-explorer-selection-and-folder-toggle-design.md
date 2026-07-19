# Explorer Selection And Folder Toggle Design

## Goal

Mirror the unclassified photo selection state in the explorer and make explorer folders behave as non-editable expand/collapse controls.

## Behavior

- Show a blue explorer folder glyph when the folder itself or any descendant contains a selected photo.
- Apply the same rule to every ancestor folder, including the opened root folder.
- Show the filename of each selected explorer photo in blue and restore its existing normal or assigned color when selection is cleared.
- Update glyph and filename colors in place so selection changes do not rebuild the explorer or disturb its scroll and expansion state.
- Render explorer folder names as non-editable text.
- Treat a left click on the disclosure marker, folder glyph, or folder name as one expand/collapse action.
- Preserve explorer photo filename editing and all existing photo/non-photo row behavior.

## Implementation

- Register explorer folder palette callbacks and selected-photo filename callbacks while explorer rows are built.
- Reuse `FolderPhotoSelection.HasSelectionInFolder` and the existing folder glyph palette so explorer and unclassified selection colors follow the same source of truth.
- Refresh the registered explorer selection visuals from `UpdateUnclassifiedPhotoSelectionVisuals`.
- Route directory names through the static explorer-name renderer instead of `EditableFileNameHost`; keep the directory row as the single expand/collapse click target.
- Clear and rebuild callback registries only when the explorer tree itself is rebuilt.

## Test Impact

This changes pointer and visual behavior in the Avalonia explorer without changing persistence or domain contracts. Keep the existing characterization tests unchanged unless a nearby UI-independent seam can cover the selection predicate. Run the characterization suite and build the application.
