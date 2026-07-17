# Shared Compact Combo Box Design

## Goal

Give the Rule 1 insertion selector and the classified-page photo-count selector the same visible compact design while sizing each for its content.

## Shared Design

- White background with a one-pixel black border in normal, pointer-over, pressed, and focused states.
- Black selected text and black drop-down glyph in normal, focused, and pressed states.
- Light-gray pointer-over and pressed backgrounds.
- Height 28, font size 13, consistent padding, and corner radius 4.
- Use one code helper so future state styling remains identical.

## Widths

- Rule 1 insertion selector: 96 pixels for numbered values and `마지막`.
- Photos-per-page selector: 64 pixels for its single-digit values and the reserved glyph column.

## Test Impact

This is a visual-only change with no direct coverage in the non-UI characterization project. Run the existing tests and build the application to validate both call sites and Avalonia resource keys.
