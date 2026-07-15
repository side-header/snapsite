# Unclassify Removes Cell Design

## Goal

Dragging a classified photo back to the unclassified area removes the entire classified cell instead of only clearing its `image`.

## Cell Removal

- Remove every target or omit cell containing a dropped photo path.
- Remove the cell label and position together with the cell.
- Apply the same behavior to initial `전`, `중`, `후`, and `나머지` cells and dynamically created cells.
- A multi-photo unclassification removes each matching cell in one save and refresh cycle.

## Persistence

Normal saved groups may contain any number of target or omit cells, including zero. Cell normalization cleans paths and labels but does not recreate missing cells.

Default cells are created only when a user creates a new group. Legacy phase manifests create their migration cells once during conversion. Saving and reopening a current manifest preserves empty target or omit arrays.

## Empty Collection Drop

The classified target and omit areas retain their minimum drop width even when their cell list is empty.

- A single-photo drop on a zero-cell area creates one unlabeled cell in that collection.
- A multi-photo drop on a zero-cell area places the first photo in a new cell in that collection, then applies the existing empty-cell-first automatic placement to the remaining photos.

## Export

Export behavior remains unchanged: non-empty target cells are exported in order, while omit cells and empty collections are excluded.

## Validation

Verify target and omit cell removal, removal of initial labeled cells, persistence of zero-cell arrays, single and multi drop recovery for zero-cell areas, existing source-cell behavior for moves within classified groups, export behavior, and Debug/Release builds.
