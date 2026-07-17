# Unclassified Selection Action Collapsible Details Design

## Goal

Keep the lower unclassified selection action compact by default while allowing users to reveal the full guidance on demand.

## Collapsed State

- Ordinary selection: `사진 N장이 선택되었습니다`
- Rule-based selection: `사진 N장이 선택되었습니다 (규칙1 기준 선택 적용)`
- Show a right-pointing `▸` icon beside the summary.
- Keep the add-work button visible on the right.

## Expanded State

- Clicking either the summary text or icon toggles expansion.
- Change the icon to `▾` while expanded.
- Show the existing detailed placement and automatic-title guidance below the summary.
- Clicking the header again collapses the details.

## State and Layout

- Use a dedicated header button containing the summary and icon for a single accessible click target.
- Keep the current light-blue action background and blue add-work button palette.
- Preserve expansion while the selected count changes.
- Reset expansion when the selection becomes empty.
- Preserve photo selection, rule-based selection, grouping, and add-work behavior.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Verify both ordinary and rule-based summary strings and their detailed text paths.
- Restart the application with the new build.
- No automated test change is needed because there is no related UI interaction test suite.
