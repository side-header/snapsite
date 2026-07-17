# Assigned Unclassified Filename Read-Only Design

## Goal

Prevent filename changes from the gray, already-classified photo cards shown in the unclassified area while keeping filename editing available in the classified area.

## Design

- Add an optional read-only mode to the shared permanent filename `TextBox` builder.
- Enable read-only mode only when an unclassified-area photo card represents an already-assigned photo.
- Keep unassigned unclassified filename fields editable.
- Keep classified filename fields editable.
- Preserve text selection, copying, hover borders, and path tooltips on read-only filename fields.
- Explicitly skip Enter-to-rename handling for read-only filename fields.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Verify the assigned unclassified call site passes read-only mode and the classified call site does not.
- Verify the rename handler rejects read-only fields.
- Restart the application with the new build.
- No automated test change is needed because there is no related UI interaction test suite.
