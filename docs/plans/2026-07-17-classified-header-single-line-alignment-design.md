# Classified Header Single-Line Alignment Design

## Goal

Align the classified group number, title, photos-per-page label, count selector, and remove button on one shared vertical center line.

## Design

- Keep the classified header height at 54 pixels.
- Place the group number host inside the header's first column instead of overlaying it from the outer two-row layout.
- Vertically center the number border, title editor outline, photos-per-page stack, selector, and remove button at the header midpoint.
- Remove the title's top alignment and top margin.
- Keep the number hover menu anchored to the number's lower-right side and allow its host to expand downward without moving the centered number.
- Preserve header column widths, title editing, page-count selection, group deletion, group selection, and drag reordering.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Confirm all five header elements are children of the same header row and use the same vertical center.
- Confirm the number hover menu remains below and to the right of the number.
- Restart the application with the new build.
- No automated test change is needed because there is no related UI layout test suite.
