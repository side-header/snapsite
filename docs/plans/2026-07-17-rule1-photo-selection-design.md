# Rule 1 Photo Selection Design

## Goal

Add a `규칙1 기준 선택` workflow that selects unclassified photos whose extensionless file names are an exact `number + 전|중|후` combination, previews that selection with distinct blue styling, and creates one work category per folder-and-number combination.

## Selection

- Place `규칙1 기준 선택` immediately left of the unclassified zoom-out button.
- Match names such as `1전.jpg`, `23중.png`, and `101후.jpeg` with the exact extensionless pattern `^(number)(전|중|후)$`.
- Exclude photos already assigned to any category.
- Replace the current manual selection with the matching paths, but preserve the current folder expansion state. Do not automatically expand folders.
- Keep rule-selected paths from collapsed folders in the internal selection across ordinary center-panel refreshes.
- Show visible rule-selected cards with a blue border and pale-blue background. Suppress selection-order badges.
- A direct manual card selection exits Rule 1 mode and returns to the existing green ordered-selection behavior.

## Action Area

Reuse the bottom selection action area with a pale-blue background and blue `선택된 사진으로 공종 추가하기` button. Explain that only exact number-and-phase file names were selected and that categories will be created by folder and number. Show the selected-photo count and planned-category count.

## Category Creation

- Group selected paths by immediate parent folder and parsed numeric prefix.
- Sort groups by folder path and numeric prefix. Use phase order `전`, `중`, `후` inside each group.
- Name each category `{folder name} {number}구역`.
- Place the first matching `전`, `중`, and `후` photo into their corresponding default target cells. Leave missing phases empty.
- If multiple files map to the same folder, number, and phase, keep the first path in the phase cell and preserve additional paths in the omit collection.
- Save once after all categories are created, select the first created category, clear Rule 1 selection, and refresh the UI.

## Empty and Error Behavior

If no unclassified photo matches, clear the selection, keep folders as they are, hide the action area, and report that no Rule 1 photos were found. A failure to produce a valid group leaves state unchanged for that group.

## Test Impact

Validate exact name matching, rejection of near matches, assigned-photo exclusion, numeric ordering, folder-and-number grouping, missing phases, duplicate phases, preservation of collapsed selections, manual-mode transition, and four-photo existing manual behavior. Build the full application after focused checks.
