# Unclassified Selection Dismiss Design

## Goal

Allow a single selected unclassified photo to be deselected by clicking it again, and clear photo selection when the user clicks non-photo content inside the unclassified area. Remove the visible gaps between folder rows at the same time.

## Card Selection

- A normal click on an unselected card keeps the existing single-selection behavior.
- A normal click on the only selected card clears selection and its anchor.
- A normal click on one card while multiple cards are selected keeps the existing behavior of reducing selection to that card.
- Preserve Control/Command toggle selection and Shift range selection.
- A direct card click during Rule 1 selection keeps the existing transition to normal selection rather than immediately deselecting that card.

## Background Dismissal

Mark photo-card visual trees so background handlers can distinguish photo clicks from non-photo clicks. Clear selection on left clicks in unclassified empty space, guidance and owner-label content, and folder rows. Folder-row clicks then continue their normal expand or collapse action. Do not clear before photo-card interactions, scroll-bar interactions, or the bottom create-category action.

Clearing selection must immediately refresh card colors, folder `N개 선택됨` labels, and bottom action visibility without rebuilding the entire tree.

## Folder Spacing

Change the unclassified tree container spacing from 8 to 0 so adjacent folder rows touch. Keep row height, outer tree margin, and photo-wrap-specific margins unchanged.

## Test Impact

Validate single-card re-click deselection, ordinary single replacement, Control/Command toggle, Shift range behavior, and Rule 1 transition behavior. Build the application to validate routed-pointer and visual-tree integration.
