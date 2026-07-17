# Highlight Classified Photos After Drop Design

## Goal

After one or more photos are dragged from the unclassified area into the classified area, briefly highlight every classified photo cell created or updated by that drop.

## Interaction

- For a single-photo drop, highlight the matching classified photo cell.
- For a multi-photo drop, highlight all matching classified photo cells at the same time.
- Show a blue border and pale-blue background, then restore each card's normal white background and gray border within approximately 0.7 seconds.
- Do not move either the unclassified or classified scroll position for this effect.
- Preserve the existing assigned-photo click navigation and highlight behavior.
- If a rendered target cannot be found, skip only that target without showing an error.

## Implementation

1. Keep the normalized assigned-photo paths in the shared post-drop refresh method.
2. Rebuild the classified area as required to show the new assignments.
3. After layout, resolve each assigned path through the existing classified photo-card lookup.
4. Reuse the existing classified photo-card transition for every resolved target.
5. Keep the unclassified partial-card replacement and scroll-preservation behavior unchanged.

## Verification

- Build the Avalonia application with zero errors.
- Verify a single-photo drop highlights only its new classified cell.
- Verify a multi-photo drop highlights every moved photo cell simultaneously.
- Verify all highlighted cells return to normal within approximately 0.7 seconds.
- Verify the effect does not change either panel's scroll position.
- Verify clicking a gray assigned photo still navigates to and highlights the matching classified cell.
