# Classified Empty Cell Height Design

## Goal

Make classified cells with an empty image path the same vertical size as cells containing a photo.

## Root Cause

The empty card only uses the existing scaled minimum height. A populated card grows beyond that minimum because its image, filename field, spacing, padding, and border require more vertical space.

## Design

- Calculate a shared classified card minimum height from the larger of:
  - the existing scaled card minimum; and
  - the scaled image height plus the filename field height, content spacing, vertical padding, and border thickness.
- Apply that shared minimum height to empty cards, populated cards, and their hover-control container.
- Keep the cell label, drop behavior, hover controls, and classified photo scaling behavior unchanged.

## Validation

- Build the Avalonia application.
- Check the diff for whitespace errors.
- Restart the application with the new build.
- No automated test change is needed because this is a classified-card layout correction and there is no related UI layout test suite.
