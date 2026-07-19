# Preview Label and Filename Sync Design

## Goal

Refresh edited photo labels immediately in the selected category preview and render preview filenames with the same text-box implementation as classified photo cards.

## Behavior

- After Enter commits a changed `PhotoCell.Label`, refresh the detail preview when that group is selected.
- Keep autosave behavior unchanged.
- Replace the preview-only filename display/editor host with `PermanentFileNameTextBox`.
- Reuse the classified-card font size, foreground, selection colors, background, hover border, rename, and Enter behavior.
- Keep only the preview-specific width and margin so the filename fits beneath the larger preview image.

## Test Impact

The label refresh and control reuse affect Avalonia UI wiring and have no direct coverage in the non-UI characterization project. Run the existing characterization tests and build the application.
