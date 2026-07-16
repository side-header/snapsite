# Unclassified Photo Owner Label Design

## Goal

Make each unclassified photo-card group clearly identify the folder that directly contains those photos.

## Presentation

Insert one left-aligned text line immediately above every non-empty photo-card wrap. Do not add a background, border, badge, or separate container styling. Render the owning folder name in sky blue and the remaining count text in the normal dark text color:

`0.경계석ok 폴더의 사진 총 23개`

Indent the text by the same folder depth as its photo-card wrap so ownership remains clear in nested folders.

## Count Rule

Count only the photos directly contained by that folder. Descendant-folder photos receive their own owner label and count when their card group is rendered. Folders with no direct photos do not show an owner label.

## Behavior and Test Impact

Keep folder expansion, card order, selection, preview, drag-and-drop, and classification behavior unchanged. Validate that parent and child labels use separate direct-photo counts and build the full application to catch Avalonia inline-text integration issues.
