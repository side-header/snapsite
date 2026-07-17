# White Empty Photo Card Design

## Goal

Render classified cells whose `image` is empty with a white photo-card background.

## UI behavior

Change only the empty card surface from `#F7F9FA` to white for both target and omit cells. Keep the existing card dimensions, border, corner radius, label editor, drop target, and hover controls.

The omit collection area itself remains `#F7F9FA`, so a white empty omit card is still visually distinct from the collection background. Target collection backgrounds remain white.

No manifest, classification, or export behavior changes.

## Validation

Verify the empty-card branch uses white while omit collection backgrounds retain `#F7F9FA`, then run formatting, Debug/Release builds, existing classification regression checks, and application startup.
