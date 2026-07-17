# Folder Scan Depth Seven Design

## Goal

Increase the folder-open scan limit from three nested folder levels to seven.

## Behavior

- Treat the opened root folder as depth 0.
- Discover folders and files through depth 7.
- Do not scan folders or files at depth 8 or deeper.
- Keep the existing `.newgreen-cache` and `exports` directory exclusions.
- Keep existing image-extension detection and sorting behavior.

## Implementation

Change the single `FileScanner.MaxFolderDepth` constant from `3` to `7`. The existing directory and file enumeration methods already use this shared limit consistently.

## Verification

- Build the Avalonia application with zero errors.
- Confirm the scanner constant is 7.
- Confirm the existing exclusion rules remain unchanged.
- Restart the application so newly opened folders use the updated depth.
