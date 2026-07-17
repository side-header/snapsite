# Unclassified Selection Blue Palette Design

## Goal

Use a blue visual palette for photos selected by ordinary clicks in the unclassified area.

## Design

- Use `#2f80ed` for the selected photo-card border.
- Use `#eaf4ff` for the selected photo-card background.
- Use `#2f80ed` for multi-selection order badges so the selected card uses one consistent palette.
- Reuse the same card palette already used by rule-based selection while preserving the behavioral distinction between ordinary and rule-based selection.
- Keep selection order, click and modifier behavior, dismissal, folder counts, and the lower action panel unchanged.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Confirm no green card-selection color remains in initial rendering or incremental selection refresh.
- Restart the application with the new build.
- No automated test change is needed because this is a presentation-only change without a related UI test suite.
