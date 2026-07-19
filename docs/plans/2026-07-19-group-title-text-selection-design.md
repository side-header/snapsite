# Group Title Text Selection Design

## Goal

Allow mouse text selection inside a classified group title without starting group reorder drag-and-drop.

## Behavior

- Exclude `TextBox` controls from group reorder drag initiation alongside buttons, combo boxes, and images.
- Inspect the pointer source's visual ancestry so clicks on internal text-rendering elements still resolve to their owning `TextBox`.
- Keep group reordering available from the remaining header area.
- Do not change title editing, saving, row selection, or reorder drop behavior.

## Test Impact

The change affects Avalonia routed-pointer ancestry and has no direct coverage in the current non-UI characterization project. Run the existing characterization tests and build the application.
