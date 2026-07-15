# Empty Default Omit Label Design

## Goal

New omit cells use an empty default label and do not show the `X` label watermark.

## New Cells

- A newly created group's initial omit cell is `{ "image": "", "label": "" }`.
- Omit cells inserted with the classified-card arrow controls already use empty image and label values and keep that behavior.
- Automatic photo placement that appends omit cells continues to create empty labels.

Target defaults remain `전`, `중`, and `후`. Target label watermark behavior does not change.

## Label Editor

The classified cell label editor receives the collection type. Empty omit labels use an empty watermark, while target cells keep the existing `X` watermark.

## Existing and Legacy Manifests

Current manifests are not rewritten. An existing explicit omit label such as `나머지` remains unchanged.

During legacy phase conversion:

- An existing `otherLabels` value is preserved.
- A missing other label falls back to an empty string instead of `나머지`.

## Export

Omit cells remain excluded from DOCX and HWPX output, so this change does not affect export content.

## Validation

Verify new-group defaults, inserted omit cells, legacy conversion with and without an explicit other label, preservation of current manifest labels, target defaults, omit watermark configuration, manifest round-trip, and Debug/Release builds.
