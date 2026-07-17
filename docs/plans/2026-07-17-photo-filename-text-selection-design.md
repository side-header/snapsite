# Photo Filename Text Selection Design

## Goal

Make filename `TextBox` controls in classified and unclassified photo cards show and support text-range selection like the other editable text fields.

## Design

- Use `#d8ecff` as the filename selection background, matching the group-title editor.
- Keep permanent filename fields focusable so normal pointer presses and drags reach the native `TextBox` selection behavior.
- In the unclassified area, process an ordinary filename click as both photo selection and text editing without allowing the event to toggle the parent card a second time.
- Preserve Shift and Ctrl/Command filename clicks as photo range/toggle selection gestures and prevent those modifier gestures from starting text selection.
- Preserve filename rename, Enter-to-save, hover border, classified group preview selection, and card drag behavior.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Verify classified filename fields use the visible selection brush.
- Verify unclassified filename pointer routing retains modifier-based photo selection while allowing ordinary text selection.
- Restart the application with the new build.
- No automated test change is needed because there is no related pointer-interaction UI test suite.
