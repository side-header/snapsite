# Classified Page Preview Selection Design

## Goal

Select a classified category page for preview when the user left-clicks anywhere within that page.

## Behavior

- Observe left-button pointer presses at the classified page root using tunnel routing and handled events.
- Select the page for preview before child controls process the same click.
- Preserve title editing, combo-box interaction, buttons, photo-cell actions, and number-badge reorder dragging.
- Skip redundant preview refresh when the clicked page is already selected.
- Keep blank pages excluded from category preview through the existing selection guard.
- Remove the older short-click handler that excluded text boxes, buttons, and combo boxes.

## Test Impact

The change affects Avalonia routed-pointer wiring and has no direct coverage in the non-UI characterization project. Run the existing characterization tests and build the application.
