# Classified Title Width Design

## Goal

Expand the classified work-title TextBox to use the available header space up to the left side of the per-page photo-count controls.

## Layout

- Keep the existing header columns: group number, flexible title, photo-count controls, remove button.
- Stretch the title TextBox and its hover outline across the full flexible title column.
- Leave an 8px gap between the title outline and the photo-count controls.
- Keep title text left-aligned and vertically centered.
- Apply the same width behavior to blank-page watermark text.

## Behavior

- Preserve identical text placement in display and edit states.
- Preserve Enter-to-save, selection highlighting, caret, and blue hover outline behavior.
- Let the title width respond automatically when the application window changes width.

## Verification

- Build the Avalonia application with zero errors.
- Verify the title outline fills the flexible column with an 8px right gap.
- Verify normal titles and blank-page watermarks use the expanded area.
- Verify text position and editing behavior remain unchanged.
