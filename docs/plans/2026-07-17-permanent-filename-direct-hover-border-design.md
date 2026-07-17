# Permanent Filename Direct Hover Border Design

## Goal

Make the filename field in both classified and unclassified photo cards show a clearly visible blue border while the pointer is over the field.

## Design

- Apply the hover border directly to the permanent filename `TextBox` instead of wrapping it with a separate hover `Border`.
- Keep the border transparent normally and change it to `#2f80ed` with a one-pixel thickness on pointer hover.
- Use the same behavior for classified and unclassified photo cards.
- Remove the filename-only outer hover wrapper so the visible border matches the actual interactive area.
- Keep assigned-photo filenames visually muted by using a light gray foreground instead of reducing the entire `TextBox` opacity. This keeps the blue hover border crisp.
- Preserve the existing selection, focus, filename editing, and rename behavior.

## Validation

- Build the Avalonia application.
- Confirm the classified and unclassified filename fields compile with the shared permanent filename editor.
- Check the diff for whitespace errors.
- No automated test change is required because this is a pointer-hover presentation fix with no domain or persistence contract change.
