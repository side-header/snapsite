# Classified Card Explicit Height Design

## Goal

Guarantee that classified cells with an empty image path render at exactly the same card height as populated cells.

## Root Cause

Using the same `MinHeight` does not guarantee the same rendered height. A populated card can grow beyond its minimum due to the measured image and themed filename `TextBox`, while an empty card remains at the minimum.

## Design

- Keep the existing content-based shared card-height calculation.
- Apply the calculated value as an explicit `Height` to both empty and populated card surfaces.
- Apply the same explicit `Height` to the hover-control container surrounding the card surface.
- Retain `MinHeight` as a compatible lower bound, while the explicit height guarantees visual equality.
- Preserve labels, photo scaling, hover controls, drag-and-drop, selection, and editing behavior.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Restart the application with the new build.
- No automated test change is needed because this is a visual layout correction without a related UI layout test suite.
