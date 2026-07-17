# Created Page Area Highlight Design

## Goal

Briefly identify every newly created classified page by highlighting the whole page area instead of its individual photo cells.

## Interaction

- Apply the highlight after the bottom `선택된 사진으로 공종 추가하기` action.
- When Rule 1 creates multiple work pages, highlight every page created by that action at the same time.
- Apply the same highlight after the classified header's `↓ 공종 페이지` and `↓ 빈 페이지` actions.
- Show a blue outline and translucent pale-blue fill over each newly created page, then return it to its normal appearance within approximately 0.7 seconds.
- The highlight must not intercept pointer or keyboard input.
- Remove the individual photo-cell highlight behavior.

## Scrolling

- Keep the bottom creation action aligned to the first created work page with its center at approximately 40% of the classified viewport height.
- Keep the existing header-button scrolling behavior for newly created work and blank pages.
- Highlighting and scrolling remain independent so applying the visual effect does not change positioning rules.

## Implementation

1. Render an input-transparent highlight overlay as part of every classified page root.
2. Track the overlay by `PhotoGroup.Id` together with the existing page view lookup.
3. Pass all IDs created by each action to one shared post-layout reveal function.
4. Fade the relevant overlays from a blue outline and pale-blue fill to transparent within approximately 0.7 seconds.
5. Delete the populated photo-card lookup and photo-card transition logic that supported the previous design.

## Verification

- Build the Avalonia application with zero errors.
- Verify normal bottom creation highlights the whole new work page and retains its 40% scroll position.
- Verify Rule 1 highlights all newly created work pages while scrolling to the first one.
- Verify `↓ 공종 페이지` and `↓ 빈 페이지` highlight the whole newly created page and retain their existing scrolling behavior.
- Verify title fields, controls, photos, drag-and-drop, and scrolling remain interactive during the effect.
- Verify the page returns to its normal appearance within approximately 0.7 seconds and photo cells are no longer highlighted individually.
