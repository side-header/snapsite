# Ordered Multi-Photo Selection and Drag

## Goal

Support ordered Shift multi-selection in the unclassified area and move the selected photos to one work group in a single drag-and-drop operation.

## Selection Contract

- A normal click replaces the selection with one photo.
- Shift-click appends an unselected photo or removes an already selected photo.
- Removing a photo compacts the remaining selection order.
- Shift-click does not open image preview or file-name editing.
- Every selected card uses the existing green selection background and border.
- When at least two photos are selected, every selected card shows its 1-based order in a green top-right badge.
- A single selected photo does not show a badge.

## Drag Contract

The drag payload distinguishes an ordered photo list from the existing single-photo and group-reorder payloads.

- Dragging any selected card when multiple photos are selected moves the full selection in order.
- Dragging an unselected card moves only that card.
- A multi-photo payload dropped anywhere inside a normal work-group row uses group-level automatic placement, regardless of the phase cell under the pointer.
- Single-photo drops keep the existing phase and slot behavior.
- Blank-page rows reject photo drops.

## Automatic Placement

The target group's photo count is evaluated once before moving any selected photo.

- Empty target: photo 1 goes to `Before`, photo 2 to `Processing`, photo 3 to `After`, and photo 4 onward to `Other`.
- Non-empty target: every selected photo goes to `Other`.
- Photos append in selection order within each destination phase.
- The operation clears selection after a successful drop, saves once, and refreshes once.

## Implementation

1. Replace the single selected-photo field with an ordered list and card-view registry.
2. Update image, file-name, and card pointer handling to respect Shift without disrupting preview or rename behavior.
3. Add order badges and update all selection visuals without rebuilding the editing control.
4. Add a prefixed JSON multi-photo drag payload with backward-compatible single-photo parsing.
5. Add a domain-level ordered group assignment method and route multi-photo drops from phase cells, slots, and group headers through it.
6. Preserve selection paths during file and folder renames.

## Validation

The repository has no automated UI test project. Use a temporary script to validate empty/non-empty group placement and payload-independent domain ordering, then run formatting checks and Debug/Release builds. Existing export behavior is unchanged because the result uses the same phase lists.
