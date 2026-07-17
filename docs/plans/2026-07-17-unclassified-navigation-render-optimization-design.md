# Unclassified Navigation Render Optimization Design

## Goal

Avoid rebuilding the explorer and entire unclassified photo tree when classified-area navigation targets folders that are already expanded.

## Current Problem

`RevealPhotosInUnclassifiedArea` currently calls `RefreshLeft` and `RefreshCenter` for every group-number or classified-photo click. This recreates already expanded folder rows, photo cards, thumbnails, and layout, producing visible flicker and unnecessary work.

## Behavior

- If every required ancestor folder is already expanded in both shared expansion sets, keep the current UI tree intact.
- Use the existing rendered photo cards directly for scrolling and highlighting.
- If any required folder is closed, add only the missing expansion keys and rebuild once.
- Preserve all other expanded folders.
- Preserve the existing 40% scroll alignment and blue-to-gray highlight animation.
- If expansion state says the target should be visible but no rendered card exists, rebuild once as a recovery path.

## Implementation

1. Collect scanned target photo paths and their ancestor folder keys as before.
2. Track whether adding the root or any ancestor key changes either `expandedExplorerPaths` or `expandedUnclassifiedPaths`.
3. Extract rendered-card lookup, scrolling, and highlighting into a helper that returns whether at least one target card was found.
4. When expansion state is unchanged, call the helper immediately against the current card list and return on success.
5. When expansion changed, or the current card lookup unexpectedly fails, call `RefreshLeft` and `RefreshCenter` once and navigate after layout.

## Edge Cases

- Multi-folder groups rebuild only when at least one target folder was closed.
- Inconsistent explorer and unclassified expansion sets are synchronized and trigger one rebuild.
- Missing or stale rendered cards trigger one recovery rebuild rather than silently doing nothing.
- Empty or unscanned paths remain ignored.

## Verification

- Build the Avalonia application with zero errors.
- Verify repeated clicks for an already visible photo do not recreate the unclassified tree.
- Verify a closed target folder still opens and navigates correctly.
- Verify multi-folder groups open all missing folders with one rebuild.
- Verify scrolling, highlighting, selections, zoom, and existing expanded folders remain unchanged.
