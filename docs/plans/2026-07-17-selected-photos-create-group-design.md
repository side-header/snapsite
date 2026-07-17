# Selected Photos Create Group Design

## Goal

When one or more unclassified photos are selected, show a persistent action area at the bottom of the unclassified panel. Its button creates a normal work-category page and assigns the selected photos in selection order.

## Interaction

- Keep the action area hidden while no unclassified photos are selected.
- Show guidance and a `선택된 사진으로 공종 추가하기` button as soon as the selection becomes non-empty.
- Keep the action area docked below the scrollable unclassified tree so it stays visible while browsing photos.
- Clicking the button creates the same default group as `공종 페이지 추가`, selects that group, clears the unclassified selection, persists the manifest, and refreshes the UI.

## Placement Rule

Reuse the exact-cell multi-photo drop behavior at the first target cell. The default `전`, `중`, and `후` target cells are filled first in selection order. Remaining photos flow into the omit collection, reusing empty omit cells before appending new ones. Four selected photos therefore become `전`, `중`, `후`, and the first `나머지` photo.

## Test Impact

The behavior delta is limited to selection-driven action visibility and group creation through the existing placement rule. Validate the placement rule with focused state-level checks where practical, then build the application to catch Avalonia UI and integration errors.
