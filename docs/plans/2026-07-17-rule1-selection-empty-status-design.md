# Rule 1 Empty Selection Status Design

## Goal

Distinguish between an opened folder that has no Rule 1 file names and one whose Rule 1 photos all already belong to classified categories.

## Status Rules

- Count every scanned photo whose normalized extensionless name exactly matches Rule 1 before excluding assigned paths.
- If at least one unclassified match remains, keep the existing selection and selected-count status.
- If no unclassified match remains but the total Rule 1 match count is greater than zero, show `규칙1에 해당하는 사진이 N개가 있으나, 모두 분류된 상태입니다.`
- If the total Rule 1 match count is zero, show `규칙1에 해당하는 사진이 없습니다.`

Do not change selection styling, grouping, category creation, or folder expansion state.

## Test Impact

Validate all three branches: selectable matches, all matches assigned, and no matches. Then build the application.
