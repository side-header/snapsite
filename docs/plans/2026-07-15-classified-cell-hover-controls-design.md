# Classified Cell Hover Controls Design

## Goal

Provide direct cell deletion and empty-cell insertion controls on every classified photo or empty card.

## Hover Controls

Each classified card shows a compact bottom-centered overlay while the pointer is over the card or its controls. The overlay does not change card or row dimensions.

The controls are:

- `−`: delete the current cell.
- `←`: insert an empty cell immediately to the current cell's left.
- `→`: insert an empty cell immediately to the current cell's right.

Tooltips describe each action. Controls appear for target and omit cells but not in the detail preview. Button presses do not start photo dragging, preview, or filename editing.

## Cell Operations

Cell mutations use the group id, collection type, and current cell index.

- Deleting a photo cell removes the whole cell and makes its photo unclassified without moving the original file.
- Deleting an empty cell removes the whole cell.
- Inserting left or right creates `{ "image": "", "label": "" }` at the adjacent array position.
- Each successful operation saves once and refreshes once.

Target and omit arrays may remain empty. Their existing minimum-width collection drop areas continue to support adding photos again.

## Presentation

The control overlay uses a compact light surface and clear borders so it remains readable on white populated cards and `#F7F9FA` empty cards. It stays visible when moving the pointer from the card content onto a button.

## Validation

Verify left and right insertion indices, photo and empty-cell deletion, unclassified photo visibility through assigned-set changes, target and omit behavior, drag suppression from buttons, persistence, existing drop behavior, and Debug/Release builds.
