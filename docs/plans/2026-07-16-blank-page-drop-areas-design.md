# Blank Page Drop Areas Design

## Goal

Show the normal target and omit areas for an inferred blank page while preserving the manifest shape `{ "title": "", "target": [], "omit": [] }` until content is added.

## UI behavior

Render a blank page with the same full group-row layout used by normal work types instead of the compact dedicated row. Keep the title empty-state guide `빈 페이지로 출력됩니다.` while the item satisfies the blank-page predicate. Each zero-cell target or omit collection retains the existing one-slot-width background as a visible drop area; no placeholder `PhotoCell` is persisted merely for display.

## Drop behavior

Allow single and ordered multi-photo drops into either empty collection of a blank page. A drop creates empty-label cells only in the chosen target or omit collection, preserving selection order. Because at least one collection is then non-empty, the item stops satisfying the blank-page predicate and uses normal group behavior. Target photos participate in export; omit photos remain excluded.

Cell-level operations remain unavailable before the first drop because no actual cells exist. The existing title edit, removal, group reorder, and page-count controls use the normal group-row behavior.

## Validation

- A newly added blank page still saves with an empty title and empty arrays before any drop.
- Single and multi-photo drops populate only the chosen zero-cell collection.
- The first drop changes the item from a blank page to a normal group.
- Blank-page DOCX/HWPX output remains empty before a drop; target content exports normally after a drop and omit content stays excluded.
- Formatting, Debug/Release builds, and application startup remain clean.
