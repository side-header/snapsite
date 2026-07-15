# Page Add Button Auto Width Design

## Goal

Size the classified-area `빈 페이지 추가` and `공종 페이지 추가` buttons from their text instead of fixed widths.

## UI behavior

Keep the existing 26-pixel button height, font, border, order, and four-pixel spacing. Remove the explicit 96- and 130-pixel widths from the two action buttons and apply 12 pixels of horizontal content padding. Avalonia then derives each button width from its text plus equal left and right padding.

The zoom buttons continue to use their existing fixed 34-pixel width and zero padding. Button actions and all application behavior remain unchanged.

## Validation

Verify the action buttons no longer receive fixed widths, run formatting and Debug/Release builds, and start the application once to catch layout initialization errors.
