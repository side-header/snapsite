# Unclassified Selected Count Color Design

## Goal

Highlight the `N개 선택됨` portion of each unclassified folder status in sky blue while retaining the existing gray treatment for classified counts.

## Behavior

- Render the selected count with the existing selection blue `#2f80ed`.
- Keep the separator and `N개 분류됨` text in the existing gray `#7d8790`.
- Preserve the current wording, count calculation, visibility rules, and folder layout.
- Support selected-only, classified-only, and combined status values without empty separators.

## Test Impact

Keep count calculation behavior unchanged and validate the three status combinations through the existing characterization test project, then build the application.
