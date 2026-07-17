# Unclassified Explicit Hover Border Design

## Goal

Ensure both photo cards and permanent filename fields in the unclassified area visibly show a blue border while the pointer is over them.

## Root Cause

- Unclassified photo cards currently have no hover-border handler.
- Permanent filename fields watch `IsPointerOverProperty`, while the Avalonia text control resources still force the pointer-over border brush to transparent.

## Design

- Attach explicit `PointerEntered` and `PointerExited` handlers to unclassified photo cards.
- Show `#2f80ed` on card hover and restore the card's selection-dependent border when the pointer exits.
- Attach explicit `PointerEntered` and `PointerExited` handlers to permanent filename `TextBox` controls.
- Show `#2f80ed` on filename hover and restore a transparent border on exit.
- Set the permanent filename pointer-over border resource to blue so the Fluent theme cannot replace the explicit hover color with transparency.
- Preserve photo selection, rule-based selection, assigned-photo display, focus, editing, and rename behavior.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Restart the application with the new build.
- No automated test change is needed because the behavior is limited to pointer-hover presentation and the repository has no directly related UI test suite.
