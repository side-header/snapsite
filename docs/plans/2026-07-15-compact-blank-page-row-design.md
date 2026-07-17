# Compact Blank Page Row

## Goal

Reduce the classified-area blank-page row from 96 pixels to 48 pixels and use a white background.

## Behavior

- Keep the sequence number, `빈페이지` label, and remove button.
- Keep drag-to-reorder available across the full row height.
- Keep persistence and DOCX/HWPX export behavior unchanged.

## Implementation and Validation

Set the row border minimum height, content height, and drag handle height to 48 pixels. Replace the gray content background with white. Run formatting checks and build the application; no automated test changes are needed for this visual-only adjustment.
