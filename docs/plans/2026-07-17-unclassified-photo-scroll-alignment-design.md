# Unclassified Photo Scroll Alignment Design

## Goal

When navigation from the classified area reveals a photo in the unclassified area, position the target photo slightly above the viewport center instead of merely making it visible near the bottom.

## Interaction

- Align the target photo card's vertical center at approximately 40% of the unclassified photo viewport height.
- If the target is near the beginning or end of the content, clamp scrolling to the available range.
- Group-number navigation continues to use the highest matching photo in visual tree order.
- Folder expansion and blue-to-gray highlight animation remain unchanged.
- Direct selection inside the unclassified area does not alter scrolling.

## Implementation

1. Retain a reference to the `ScrollViewer` that hosts the unclassified photo tree during `RefreshCenter`.
2. After the refreshed tree completes layout, transform the target card's origin into the scroll content coordinate space.
3. Calculate the desired vertical offset as `card center Y - viewport height * 0.4`.
4. Clamp the offset between zero and `Extent.Height - Viewport.Height`, preserving the current horizontal offset.
5. Fall back to `BringIntoView()` if the scroll viewer, content visual, or coordinate transform is unavailable.
6. Apply the existing highlight animation after positioning the target.

## Edge Cases

- Small content that does not scroll stays at offset zero.
- Targets near the top or bottom use the closest valid alignment.
- A missing rendered target leaves the scroll position unchanged.
- Window size and photo zoom changes are handled by using the current viewport and card bounds.

## Verification

- Build the Avalonia application with zero errors.
- Verify classified photo-cell navigation places the target center near 40% of the viewport.
- Verify group-number navigation aligns the highest matching photo the same way.
- Verify top and bottom targets clamp correctly.
- Verify direct unclassified selection and highlight animation remain unchanged.
