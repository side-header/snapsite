# Cell Remove Hover Stability Design

## Goal

Keep the classified-card remove button visible and visually stable when the pointer moves from the card onto the button.

## Interaction

Drive overlay visibility from the card root's aggregate `IsPointerOver` property instead of unconditional pointer-enter and pointer-exit handlers. Descendant hover therefore remains part of the card hover state, so entering the remove button does not hide its own overlay.

## Visual states

Give the `−` button explicit Avalonia theme resources for normal, pointer-over, and pressed states. Preserve a dark foreground and the existing border in every state. Use the same subtle light pointer-over and pressed backgrounds as the classified-area header buttons.

The control size, centered bottom position, tooltip, pointer handling, and remove behavior remain unchanged.

## Validation

Verify the overlay follows `IsPointerOver`, verify all button-state resources are present, keep cell-removal regression coverage, and run formatting, Debug/Release builds, and application startup.
