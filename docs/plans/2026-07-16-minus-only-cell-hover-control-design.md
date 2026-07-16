# Minus-Only Cell Hover Control Design

## Goal

Show only the remove control on classified photo and empty-cell cards.

## UI behavior

Remove the visible `←` and `→` buttons from the classified-card hover overlay. Keep one centered `−` button with its existing tooltip and pointer handling. The overlay remains hidden until hover and does not change card dimensions.

Clicking `−` keeps the existing behavior: remove the exact target or omit cell, and return its photo to the unclassified area when the cell contains an image.

## Scope

Keep `AppState.InsertEmptyCell` and its domain validation in place. Only the hover UI entry points for left and right insertion are removed. Update current requirements and architecture documentation so they describe the minus-only control.

## Validation

Verify the classified-card overlay constructs only one control, keep the existing cell-removal regression coverage, and run formatting, Debug/Release builds, and application startup.
