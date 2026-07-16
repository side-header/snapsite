# Folder Direct Selection Count Design

## Goal

Show how many currently selected unclassified photos belong directly to each rendered folder row.

## Presentation

Add a small gray `N개 선택됨` text immediately to the right of the folder name in the unclassified tree. Hide the text when the direct selected-photo count is zero. Apply the same presentation to manual green selection and Rule 1 blue selection.

## Count Rule

For every selected relative photo path, derive only its immediate parent folder key. Increment that folder's count once. Do not propagate the count to ancestor folders. A photo directly under the opened base folder contributes to the opened-root row.

## Update Strategy

Keep references to the currently rendered folder-count text blocks. Update their text and visibility from the existing selection-visual refresh path so clicking, toggling, range selection, Rule 1 selection, and selection clearing update immediately without rebuilding the tree or changing expansion and scroll state. Clear and rebuild the reference map during an ordinary center-panel refresh.

## Test Impact

Validate separate direct counts for a parent folder, nested child folders, and the opened root. Confirm zero-count folders are omitted by the derived map, then build the application to validate Avalonia UI integration.
