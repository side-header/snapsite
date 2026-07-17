# Created Groups Scroll and Photo Highlight Design

## Goal

After the bottom `선택된 사진으로 공종 추가하기` action creates classified work pages, scroll to the first created page and briefly highlight the populated photo cells in every newly created page.

## Interaction

- For a normal selection, navigate to the single created work page.
- For a Rule 1 selection, navigate to the first created work page and highlight photo cells across all work pages created by that action.
- Position the first created page's vertical center at approximately 40% of the classified viewport height.
- Clamp scrolling near the beginning or end of the content.
- Immediately show populated photo cells with a blue border and pale blue background.
- Transition those cells back to their normal white background and gray border within approximately 0.7 seconds.
- Do not highlight empty cells.
- Do not alter actual selection state, title editors, label editors, or filename editors.
- Do not apply this highlight to the classified header's blank-page or work-page add buttons.

## Implementation

1. Track rendered populated photo-card surfaces by `PhotoGroup.Id` while building classified rows.
2. Capture every created group ID in the manual-selection and Rule 1 creation flows.
3. After `RefreshAll` and layout, find the first created page and align its center to 40% of the classified viewport.
4. Collect populated photo-card surfaces for all created IDs and apply brush transitions matching the existing 0.7-second highlight style.
5. Keep the existing header-button page scrolling path separate so it retains top alignment without photo-cell highlighting.

## Verification

- Build the Avalonia application with zero errors.
- Verify normal creation scrolls to and highlights its single work page.
- Verify Rule 1 creation scrolls to the first page and highlights populated cells in all created pages.
- Verify empty cells remain unchanged.
- Verify cells return to normal styling within approximately 0.7 seconds.
- Verify header add buttons retain their existing behavior.
