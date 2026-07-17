# Render-First Debounced Autosave Design

## Goal

Make visible UI changes appear before automatic metadata persistence, while preserving changes safely across rapid edits, folder changes, and application exit.

## Scope

- Apply to automatic saves triggered by photo moves, photo assignment or removal, group and blank-page changes, cell changes, counts, titles, and labels.
- Keep the settings dialog save action synchronous.
- Keep the manual save action synchronous.

## Interaction Order

1. Mutate the in-memory state.
2. Refresh the affected UI immediately.
3. Schedule metadata persistence 200 milliseconds later.
4. If another automatic change occurs before the delay expires, replace the pending save with the latest state and message.

This allows Avalonia to render the updated controls before metadata sanitization, serialization, and file replacement run.

## Pending Save Management

- Store one pending automatic-save message and incrementing version.
- Use a one-shot dispatcher timer for the 200-millisecond delay.
- A newer request invalidates older timer callbacks.
- When the current callback runs, clear the pending request and call the existing synchronous metadata save without triggering a scan or UI refresh.

## Durability

- Flush a pending automatic save synchronously before opening another root folder.
- Flush a pending automatic save synchronously when the window closes.
- Flush a pending automatic save before the manual save action.
- The settings dialog continues to save immediately and does not enter the debounce queue.
- Preserve current error reporting when persistence fails.

## Refresh Behavior

- Move existing refresh calls before the automatic-save request.
- Text fields that already display their changed value can queue the automatic save without an additional full refresh.
- Keep current full-area refresh behavior in this change; partial classified-area rendering is a separate optimization.

## Verification

- Verify a classified cell visually moves before automatic persistence runs.
- Verify several changes within 200 milliseconds produce one final save.
- Verify pending changes are persisted before root-folder replacement and application exit.
- Verify manual and settings saves remain immediate.
- Verify automatic-save failures still surface through the current error path.
- Build the Avalonia application with zero errors.
