# Rule 1 Combo Box Click Stability Design

## Goal

Allow the Rule 1 insertion-position combo box to open and accept a selection without clearing the unclassified photo selection or hiding its own action panel.

## Behavior

- Treat combo-box interaction as part of the active unclassified selection UI.
- Exclude `ComboBox` controls from the parent area's selection-dismiss pointer handler.
- Preserve the selected Rule 1 photos, expanded details, and current insertion option while the combo box opens.
- Keep existing outside-click dismissal behavior for unclassified-area background clicks.
- Do not change combo-box options or insertion semantics.

## Test Impact

The change is limited to Avalonia routed-pointer classification and has no direct coverage in the current non-UI characterization project. Run the existing selection and insertion tests, then build the application to verify the affected UI code path compiles.
