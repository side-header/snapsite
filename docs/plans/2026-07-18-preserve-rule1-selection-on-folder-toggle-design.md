# Preserve Rule 1 Selection on Folder Toggle Design

## Goal

Keep a folder's Rule 1 photo selection and blue selection styling when its row is collapsed or expanded in the unclassified area.

## Behavior

- Treat folder expansion as presentation state only when Rule 1 selection mode is active.
- Preserve `selectedPhotos` and `isRule1Selection` before rebuilding the unclassified tree.
- Continue clearing ordinary manual photo selection on a folder-row toggle, preserving the existing interaction outside Rule 1 mode.
- Let the existing unclassified-tree refresh reconcile valid Rule 1 photos and repaint folder and photo selection visuals.

## Test Impact

Add the smallest regression coverage for the clear-or-preserve decision, keep the existing folder Rule 1 selection tests, then run the characterization tests and application build.
