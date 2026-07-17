# Insert Classified Cell Before Populated Destination Design

## Goal

When an already-classified photo cell A is dragged onto another classified cell B, move A immediately before B.

## Interaction

- Move the complete source `PhotoCell`, including its image, label, and other properties.
- Remove A from its original collection and insert it immediately before B.
- Apply the same insertion rule whether B contains a photo or is empty.
- Example: dragging `중` onto `전` in `전 / 중 / 후 / 빈` produces `중 / 전 / 후 / 빈`.
- Example: dragging `전` onto `후` in `전 / 중 / 후 / 빈` produces `중 / 전 / 후 / 빈`.
- Keep a self-drop as a no-op.
- Keep the existing empty-collection behavior: A becomes the first cell.
- Keep unclassified-photo behavior unchanged: an unclassified photo fills an existing empty destination cell.
- Continue highlighting the moved classified photo cell for approximately 0.7 seconds.

## Implementation

1. Retain references to source A and destination B.
2. Remove A from its source collection.
3. Resolve B's current index after removal.
4. Insert A at B's current index for both populated and empty destinations.
5. Preserve the existing rollback, self-drop, empty-collection, and unclassified fallback handling.

## Verification

- Verify `전 / 중 / 후 / 빈` with `중` dropped on `전` becomes `중 / 전 / 후 / 빈`.
- Verify a left-to-right move inserts the source immediately before the destination.
- Verify cross-collection moves between 대상 and 나머지 preserve the whole source cell.
- Verify an empty destination remains present and shifts right.
- Verify unclassified photos still fill an existing empty cell.
- Build the Avalonia application with zero errors.
