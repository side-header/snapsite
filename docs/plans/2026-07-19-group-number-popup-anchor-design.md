# Group Number Popup Anchor Design

## Goal

Anchor the classified group-number action popup by aligning its top-left corner with the number badge's top-right corner.

## Layout

- Define the number badge position and size once: left 2, top 13, size 28.
- Calculate the popup anchor as the badge's top-right corner: left 30, top 13.
- Use that calculated point for the action popup's top-left margin.
- Align the hover-description surface with the moved popup's right edge and top edge.
- Keep popup content, hover behavior, buttons, and reorder handling unchanged.

## Test Impact

This is a visual layout-only change with no direct coverage in the non-UI characterization project. Run the existing tests and build the application.
