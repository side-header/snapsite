# Photos Per Page 1-10 Design

## Goal

Allow each classified work page to use any photos-per-page value from 1 through 10 and honor that value in DOCX and HWPX exports.

## Behavior

- Show values 1 through 10 in the classified work-page selector.
- Persist the selected value and normalize loaded metadata to the inclusive 1-10 range.
- Split classified target cells into export pages using the selected count.
- Render one table row per exported cell, including user-created empty cells.
- On a partial final page, render only the remaining cells. For example, 23 cells with a value of 10 produce pages with 10, 10, and 3 rows.
- Do not synthesize empty rows to fill the final page.

## Export Layout

- Keep the existing 3-photo export settings as the base profile for counts 1-3.
- Keep the existing 4-photo export settings as the base profile for counts 4-10.
- Preserve existing 3- and 4-photo output behavior.
- For counts 5-10, reduce the calculated table-row height as needed so all selected rows fit on one page.
- Apply the same page splitting and row-count behavior to DOCX and HWPX.
- Rename the settings tabs to `1~3장 기준` and `4~10장 기준` to describe the reused profiles accurately.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Verify all photos-per-page normalization and export-limit call sites use the 1-10 range.
- Restart the application with the new build.
- No automated test project currently covers this behavior, so validation is build- and code-path-based.
