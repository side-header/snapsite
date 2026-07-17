# New Classified Page Scroll Design

## Goal

After the user presses the classified-area `↓ 빈 페이지` or `↓ 공종 페이지` header button, automatically scroll the classified area to the newly appended page.

## Interaction

- The newly created page should be aligned with the top of the classified viewport.
- If there is not enough content below the final page, scroll to the maximum available offset.
- A new work page remains selected as it is today.
- A new blank page does not create a work-page selection.
- Only the two classified header buttons trigger this automatic scrolling.
- Existing insert controls beside a group number keep their current behavior.

## Implementation

1. Keep a reference to the classified area's `ScrollViewer`.
2. Track rendered page root controls by their `PhotoGroup.Id` during `RefreshRight`.
3. Capture the newly returned group or blank-page ID in `AddPhotoGroup` and `AddBlankPage`.
4. After `RefreshAll` and layout, locate the rendered page control by ID.
5. Transform its top coordinate into the `rightPanel` content coordinate space.
6. Set the classified scroll offset to that coordinate, clamped between zero and the maximum vertical offset.
7. Fall back to `BringIntoView` if layout or coordinate information is unavailable.

## Verification

- Build the Avalonia application with zero errors.
- Verify both header buttons scroll to their newly appended page.
- Verify the page header is positioned at the top when scroll range permits.
- Verify the final page clamps to the bottom when necessary.
- Verify per-group insert controls and existing page selection behavior remain unchanged.
