# Unclassified Photo Card Selection

## Goal

Make an unclassified photo card selected when the user left-clicks anywhere inside the card, including its padding and file-name area. The selected card keeps the existing green background and border.

## Behavior

- Only unassigned photo cards are selectable.
- Clicking the image keeps the existing preview behavior and selects the card.
- Clicking the file name selects the card and keeps the existing rename interaction.
- Clicking card padding selects the card without opening the image preview.
- Dragging a photo keeps the existing drag-and-drop behavior.
- Assigned cards remain dimmed and are not selectable.

## Implementation

1. Centralize the unclassified-photo selection update in a small local helper.
2. Call it from the existing image preview callback.
3. Call it when file-name editing begins because the file-name control handles its own pointer event.
4. Handle left-button presses on the outer card for uncovered padding.
5. Build the application to catch UI wiring and compilation regressions.

## Test Impact

The repository has no automated UI test project. This is a local Avalonia event-wiring change, so validation is limited to the application build and inspection of the affected card interactions. No unrelated tests are added or changed.
