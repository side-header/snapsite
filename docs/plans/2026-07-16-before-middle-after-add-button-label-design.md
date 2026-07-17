# Before/Middle/After Add Button Label Design

## Goal

Make the classified-area action describe the page shape it creates. Replace the user-facing `공종 추가` wording with `전/중/후 페이지 추가`.

## Behavior

The action continues to call the existing `AddPhotoGroup` flow. It still creates a normal group with the default `전`, `중`, and `후` target cells and one empty omit cell. No manifest, classification, or export behavior changes.

Set the header button width to 130 pixels so the longer label is not clipped. Update the empty classified-area guide to `전/중/후 페이지 추가를 눌러 시작하세요.` and align the current README, architecture, and requirements documentation with the new visible wording.

## Validation

Verify the old user-facing wording no longer remains in active UI or current documentation, run formatting and Debug/Release builds, and start the application once to catch initialization regressions.
