# Insert Classified Cell Before Empty Destination Design

## Goal

When an already-classified photo cell A is dragged onto an existing empty classified cell B, insert A at B's position and shift B to the right instead of replacing B.

## Interaction

- Remove A's complete `PhotoCell` from its original collection.
- Insert A immediately before the existing empty B cell.
- Preserve A's image, label, and properties.
- Preserve B's empty state, label, and properties while shifting it one position to the right.
- Keep populated-destination behavior unchanged: insert A immediately after populated B.
- Keep empty-collection behavior unchanged: add A as the first cell.
- Keep unclassified-photo behavior unchanged: fill existing empty B while retaining B's label.
- Continue highlighting moved A for approximately 0.7 seconds.

## Implementation

1. Retain references to source A and destination B.
2. Remove A from its source collection and resolve B's updated index.
3. If B is empty, insert A at B's index instead of replacing B.
4. If B is populated, insert A at `B index + 1` as before.
5. Retain current empty-collection, rollback, self-drop, and unclassified fallback handling.

## Verification

- Build the Avalonia application with zero errors.
- Verify A dropped on empty B appears immediately before B.
- Verify B remains empty with its original label and shifts right.
- Verify same-collection moves work from either side of B.
- Verify populated destinations and empty collections retain their current behavior.
- Verify unclassified photos still fill existing empty cells.
