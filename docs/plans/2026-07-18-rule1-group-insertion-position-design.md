# Rule 1 Group Insertion Position Design

## Goal

Let users choose where Rule 1-generated category pages are inserted from the expanded blue selection action.

## UI

- Add a final bullet laid out as `• 공종번호 [선택] 위치에 페이지 추가`.
- Populate the combo box with the current page positions `1` through the current page count, followed by `마지막`.
- Select `마지막` by default each time a new Rule 1 selection begins.
- Show this control only for Rule 1 selection; keep the ordinary-selection explanation unchanged.

## Insertion Behavior

- Selecting number `N` inserts before the page currently displayed as number `N`.
- Insert multiple generated category pages consecutively from that index while preserving Rule 1 plan order.
- Selecting `마지막` appends all generated pages, preserving the existing default behavior.
- After insertion, refresh page numbering, assigned-photo badges, selection state, scrolling, highlighting, and autosave through the existing update flow.

## Test Impact

Add characterization coverage for consecutive group insertion before an existing numbered page and append behavior, then run the characterization tests and application build.
