# New Group Empty Omit Design

## Goal

Create new work-page groups without a persisted omit cell.

## Creation behavior

`AppState.AddGroup` continues to create three empty target cells labeled `전`, `중`, and `후`, but explicitly initializes `Omit` to an empty list. The resulting group shape is:

```json
{
  "title": "",
  "target": [
    { "image": "", "label": "전" },
    { "image": "", "label": "중" },
    { "image": "", "label": "후" }
  ],
  "omit": []
}
```

Keep the `PhotoGroup` property initializer and legacy migration behavior unchanged so manifests that omit the current `omit` field and older phase-based manifests retain their established compatibility behavior. Existing saved omit cells are never removed.

The zero-cell omit collection still renders its one-slot-width drop area. Dropping photos there creates omit cells through the existing placement flow.

## Validation

- `AddGroup` creates three target cells and zero omit cells.
- Save and reload preserve the empty omit array.
- Existing explicit omit cells and legacy omit migration remain unchanged.
- A drop into a zero-cell omit collection creates the first cell.
- Formatting, Debug/Release builds, the focused persistence harness, and application startup remain clean.
