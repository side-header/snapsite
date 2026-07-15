# Empty Photo Card Visual Design

## Goal

Make classified cells with an empty `image` visibly appear as empty photo cards instead of displaying an `X` placeholder.

## Classified Cell Rendering

- Keep the editable cell label above the card.
- Render an empty card with the same width, minimum height, border, and corner radius as a populated photo card.
- Use `#F7F9FA`, the omit-area color, as the empty card background.
- Keep the existing `#D8DDE2` card border so the card remains visible inside the omit area.
- Do not render an `X`, filename, image, or other placeholder content inside the empty card.
- Keep the entire empty card slot as a photo drop target.

When the cell receives a photo, the normal white photo card and its existing image and filename content replace the empty card on refresh.

## Scope

This is a presentation-only change to classified-area cells. It does not change manifest data, placement rules, selection behavior, preview behavior, or export output.

## Validation

Verify that the empty-card branch uses the populated card dimensions, the requested background and border colors, contains no placeholder text, remains a drop target, and that Debug and Release builds succeed.
