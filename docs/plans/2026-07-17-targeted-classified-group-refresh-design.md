# Targeted Classified Group Refresh Design

## Goal

When a classified cell moves, rebuild only the source and destination groups instead of rebuilding every classified group.

## Scope

- Apply to classified-cell movement within one group and between two groups.
- Keep the existing full refresh behavior for unrelated operations in this change.
- Keep the 200-millisecond debounced automatic save unchanged.

## Affected Groups

- Use the source group ID from the classified-cell drag payload.
- Use the destination group ID from the drop target.
- Deduplicate the IDs so movement within one group refreshes one group only.

## View Replacement

1. Capture the classified area's vertical scroll offset.
2. Capture each affected group's internal horizontal photo-scroll offset.
3. Capture the affected groups' photo paths before the state move.
4. Move the `PhotoCell` in application state.
5. Remove only affected group entries from the group, highlight, photo-card, and photo-scroll registries.
6. Recreate each affected `GroupRow` and replace its existing control at the same `rightPanel` child index.
7. Leave every unrelated group control instance untouched.
8. Refresh the selected detail panel and panel-header counts.

## Scroll Restoration

- Restore the classified area's vertical offset after the replacement layout pass.
- Restore affected groups' internal horizontal offsets after their new scroll viewers are attached.
- Clamp restored values to each new extent.

## Fallback

- If an affected group control is missing, its state index is invalid, or its panel position does not match, use the existing full `RefreshRight()` path.
- The fallback preserves correctness while targeted replacement handles the normal drag path.

## Existing Feedback

- Keep moved-photo highlighting after the new group rows are registered.
- Keep selected-group and detail-panel behavior.
- Keep the current status message and queued automatic save.

## Verification

- Move a cell within one group and verify only that group is recreated.
- Move a cell between groups and verify only the source and destination groups are recreated.
- Move a cell between 대상 and 나머지 in the same group.
- Verify unrelated group controls and their state remain untouched.
- Verify classified vertical scroll and affected horizontal scroll positions are restored.
- Verify moved-photo highlighting and debounced persistence still run.
- Build the Avalonia application with zero errors.
