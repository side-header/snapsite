# Group Number Reorder Handle Design

## Goal

Start classified-group vertical reordering only from the visible group-number badge.

## Behavior

- Use the 28-by-28 group-number badge as the only reorder drag handle.
- Preserve the existing six-pixel movement threshold so a simple number click still reveals the group's photos.
- Keep title editing, text selection, the photo-count combo box, header whitespace, and insert buttons outside the reorder start area.
- Keep the full classified group card as the drop target so reordering remains easy to complete.
- Clear pending number-click navigation when a reorder drag starts.

## Test Impact

The change affects Avalonia routed-pointer wiring and has no direct coverage in the non-UI characterization project. Run the existing characterization tests and build the application.
