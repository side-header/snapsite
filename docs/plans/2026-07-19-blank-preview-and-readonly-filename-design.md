# Blank Preview and Read-Only Filename Design

## Goal

Show target and omit surfaces in the preview for blank and populated pages, and keep preview filenames visible but non-editable.

## Preview Surfaces

- Allow blank pages to become the selected preview page.
- Render the existing target and omit preview sections for blank pages instead of the selection prompt.
- Use the existing photo-row minimum height of 320 pixels for each empty section.
- Keep target sections white and set the entire omit section to `#F7F9FA`, including space not occupied by photo rows.
- Preserve the existing separator, scrolling, and drop-target behavior.

## Preview Filename

- Continue using `PermanentFileNameTextBox` so typography, sizing, and layout remain aligned with classified cards.
- Add an option to disable the hover-border behavior while keeping it enabled by default for classified cards.
- Configure preview filename text boxes as read-only with no blue pointer-over border.

## Test Impact

The changes affect Avalonia preview composition and control behavior and have no direct coverage in the non-UI characterization project. Run the existing characterization tests and build the application.
