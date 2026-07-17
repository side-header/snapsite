# Unclassified Selection Action Blue Palette Design

## Goal

Make the lower action box shown for ordinary unclassified photo selection use the same light-blue and blue palette as the selected photo cards.

## Design

- Use `#eaf4ff` for the action-box background.
- Use `#b9d8ff` for the action-box border.
- Use `#2f80ed` for the action button.
- Use `#246fd1` for button hover and `#1d5db3` for button press.
- Reuse the existing rule-based selection palette for visual consistency.
- Keep the summary text, visibility, selected-photo grouping, and button behavior unchanged.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Confirm the ordinary selection branch no longer applies the green action palette.
- Restart the application with the new build.
- No automated test change is needed because this is a palette-only change without a related UI test suite.
