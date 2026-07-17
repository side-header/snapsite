# Independent Page-Count Paper Settings Design

## Goal

Provide independently persisted paper and photo-table settings for every photos-per-page count from 1 through 10.

## Settings UI

- Replace the `1~3장 기준` and `4~10장 기준` tabs with one page-settings tab displayed as `한 페이지당 [N] 장`.
- Provide a 1-10 `ComboBox` for N.
- Default the selector to 3 when the dialog opens.
- Switch the settings inputs and preview immediately when N changes.
- Render the preview with exactly N photo rows.
- Keep the common paper-template tab and save, reset, and close controls.

## Persistence and Migration

- Store page settings in a count-keyed collection covering 1 through 10.
- Migrate the legacy 3-photo settings to count 3 and the legacy 4-photo settings to count 4.
- Initialize counts 1-2 from independent clones of the legacy 3-photo settings.
- Initialize counts 5-10 from independent clones of the legacy 4-photo settings.
- Normalize and save all ten settings independently.
- Retain legacy JSON fields only for reading old metadata and omit them after normalization.

## Export Behavior

- Resolve DOCX and HWPX settings using the exact normalized photos-per-page count.
- Keep page splitting and partial final-page behavior unchanged.
- Changes made for one N must not affect any other N.

## Preview

- Generate representative phase labels safely for counts 1-10.
- Scale preview rows and photo placeholders to the selected count without indexing beyond fixed 3- or 4-row label arrays.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Verify every 1-10 input set is read, reset, previewed, and persisted independently.
- Verify exact-count settings lookup is used by both exporters.
- Restart the application with the new build.
- No automated test project currently covers these settings and preview paths, so validation is build- and code-path-based.
