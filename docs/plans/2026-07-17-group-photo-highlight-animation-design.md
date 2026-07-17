# Group Photo Highlight Animation Design

## Goal

When a user clicks a group number, briefly highlight every photo assigned to that group in the unclassified area, then animate the cards back to their assigned gray appearance within one second.

## Interaction

- Keep the existing behavior that expands all relevant folders and scrolls to the highest matching photo.
- Highlight every rendered photo belonging to the clicked group at the same time.
- Use a blue border and pale blue background for the highlight.
- Transition back to the assigned gray border and background over approximately 0.7 seconds.
- Do not change actual unclassified photo selection or show the selection action panel.
- Preserve the existing blue hover border while the pointer is over a card.
- A later group-number click starts a new highlight for that group's photos.

## Implementation

1. After the unclassified tree is refreshed, locate all rendered cards whose normalized paths belong to the clicked group.
2. Attach brush transitions for the card border and background.
3. Set the highlight brushes immediately, then schedule the assigned resting brushes on the next UI render so Avalonia animates the transition.
4. Keep the existing card hover handlers responsible for the border while the pointer is over the card.
5. Scroll the first matching card into view as part of the same post-layout callback.

## Edge Cases

- Groups without currently scanned photos do nothing.
- Missing or empty image paths are ignored.
- Repeated clicks must not modify photo selection state.
- A card under the pointer keeps its blue hover border and returns to gray after pointer exit.

## Verification

- Build the Avalonia project with zero errors.
- Verify all photos in a single-folder and multi-folder group highlight simultaneously.
- Verify cards return to assigned gray styling in approximately 0.7 seconds.
- Verify folder expansion, scrolling, hover styling, and selection state remain correct.
