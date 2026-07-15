# Page Add Button Names Design

## Goal

Clarify and standardize the two classified-area page creation actions:

- `빈페이지 추가` becomes `빈 페이지 추가`.
- `전/중/후 페이지 추가` becomes `공종 페이지 추가`.

## Scope

Change only user-facing text. Keep the existing actions, button order, and widths: 96 pixels for the blank-page action and 130 pixels for the work-page action. Update the empty classified-area guide to reference `공종 페이지 추가`, and align the current README, architecture, and requirements documentation.

No manifest, classification, drag-and-drop, or export behavior changes.

## Validation

Check that the retired names no longer appear in active UI or current documentation, then run formatting, Debug/Release builds, and an application startup smoke check.
