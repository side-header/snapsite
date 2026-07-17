# Preserve Unclassified Scroll After Drop Design

## Goal

Keep the unclassified folder tree and scroll position stable when one or more photos are dragged into the classified area, while updating only the moved photo cards to their gray assigned appearance.

## Cause

The current drop completion paths call `RefreshAll`. That recreates the unclassified `ScrollViewer` and photo tree, so its offset returns to the top even though only assignment state changed.

## Interaction

- Support both single-photo and multi-photo drops into classified cells or collections.
- Keep the unclassified folder expansion and vertical scroll position unchanged.
- Replace only the moved photo cards with their assigned gray variants.
- Clear the previous photo selection and hide its action panel as before.
- Update folder-level classified counts and the classified-area header count.
- Refresh the classified area so the drop result remains visible.
- If a moved card is not currently rendered, fall back to rebuilding the unclassified area while restoring its prior scroll offset.

## Implementation

1. Add a shared post-assignment refresh method for all unclassified-to-classified drop paths.
2. Locate each moved photo through the existing rendered-card lookup, replace it in its current parent panel with a newly rendered assigned card, and preserve sibling order.
3. Avoid `RefreshCenter` when every moved card is replaced successfully.
4. Update selection visuals, folder status counts, panel headers, the classified area, and the detail view independently.
5. For the fallback path, capture the unclassified scroll offset, call `RefreshCenter`, and restore the offset after layout.

## Verification

- Build the Avalonia application with zero errors.
- Verify a single-photo drop changes only that unclassified card to gray without moving the scroll.
- Verify a multi-photo drop changes every moved card without moving the scroll.
- Verify folder classified counts, the classified header, and the classified destination update.
- Verify selection badges and the bottom selection action disappear after the drop.
- Verify the fallback restoration path does not leave the unclassified area at the top.
