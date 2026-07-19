# Assigned Filename No-Hover Border Design

## Goal

Remove the blue pointer-over border from filenames of already classified photos shown in the unclassified area.

## Behavior

- Keep assigned filenames read-only and visually muted.
- Disable `PermanentFileNameTextBox` hover-border behavior when `assigned` is true.
- Preserve the blue hover border for editable, unassigned filenames.
- Preserve the read-only, no-hover behavior already used by preview filenames.
- Do not change photo-card selection, navigation, assignment badges, or drag behavior.

## Test Impact

This is a visual pointer-over change with no direct coverage in the non-UI characterization project. Run the existing characterization tests and build the application.
