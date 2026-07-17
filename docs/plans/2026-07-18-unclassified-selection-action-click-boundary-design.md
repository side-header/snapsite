# Unclassified Selection Action Click Boundary Design

## Goal

Keep the unclassified selection action visible for every click inside its blue panel and dismiss it only for clicks outside that panel.

## Behavior

- Mark the outer blue action border as a selection-dismiss exclusion boundary.
- When a click bubbles to the unclassified area, inspect its visual ancestry.
- Preserve selection if any ancestor is the blue action boundary, covering text, whitespace, toggle controls, combo boxes, and future child controls.
- Keep existing outside-click dismissal behavior unchanged.
- Do not change selection, insertion, expansion, or action-button semantics.

## Test Impact

The change affects Avalonia routed-pointer ancestry and has no direct coverage in the current non-UI characterization project. Run the existing selection and insertion tests, then build the application.
