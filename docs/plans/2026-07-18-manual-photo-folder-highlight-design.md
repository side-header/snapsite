# Manual Photo Folder Highlight Design

## Goal

Highlight the folder ancestry of a manually selected photo in the unclassified tree.

## Behavior

- Use the existing blue selected-folder icon palette.
- Highlight the photo's containing folder, every ancestor folder, and the unclassified root folder.
- Apply the same ancestry rule to ordinary manual selection and Rule 1 selection.
- Restore the normal icon palette when no selected photo remains in a folder subtree.
- Do not change folder names, row backgrounds, hover behavior, selection counts, or expansion state.

## Test Impact

Add regression assertions that a single selected photo belongs to its containing folder, its ancestor, and the root while excluding sibling folders. Then run the characterization tests and application build.
