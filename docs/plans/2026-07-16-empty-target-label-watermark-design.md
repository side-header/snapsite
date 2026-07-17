# Empty Target Label Watermark Design

## Goal

Remove the `X` guide character from empty target/image label inputs.

## UI behavior

Set the label editor watermark to an empty string for both target and omit cells. A cell whose actual label is empty therefore shows no guide character. Existing non-empty labels such as `전`, `중`, and `후` continue to display normally and remain editable.

This changes only the empty-input guide. It does not modify `PhotoCell.Label`, manifest serialization, drag-and-drop placement, or DOCX/HWPX output.

## Validation

Verify the label editor no longer contains the `X` watermark branch, keep existing label persistence and export checks, and run formatting, Debug/Release builds, and application startup.
