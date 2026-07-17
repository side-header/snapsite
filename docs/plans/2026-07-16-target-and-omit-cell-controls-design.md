# Target and Omit Cell Controls Design

## Goal

Correct the classified-card hover controls so target/image cells expose editing controls while omit cells expose removal only.

## UI behavior

- Target cells show `−`, `←`, and `→` in the centered bottom hover overlay.
- Omit cells show only `−` in the centered bottom hover overlay.
- `←` inserts an empty unlabeled target cell immediately before the current cell.
- `→` inserts an empty unlabeled target cell immediately after the current cell.
- `−` keeps the existing exact-cell removal and unclassification behavior in both collections.

Build the overlay conditionally from the existing `omit` argument rather than duplicating the target and omit card implementations. Every visible control uses the stabilized normal, pointer-over, and pressed resources, and the overlay continues to follow the root card's aggregate `IsPointerOver` state.

## Scope

Restore the UI calls to the existing `AppState.InsertEmptyCell` domain method only for target cards. Manifest and export contracts remain unchanged. Update current requirements, architecture, and manifest documentation to describe the collection-specific controls. This design supersedes the earlier all-cards minus-only UI design.

## Validation

- Target overlay constructs three controls with the correct insertion indices.
- Omit overlay constructs only the remove control.
- Remove and insert domain behavior remains covered.
- Formatting, Debug/Release builds, and application startup remain clean.
