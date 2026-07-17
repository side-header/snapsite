# Rule 1 Selection Bullet Details Design

## Goal

Replace the expanded Rule 1 selection explanation with three concise bullet points.

## Content

The expanded details show:

- `• 규칙1: 사진 파일 이름이 {숫자} + "전" | "중" | "후"인 경우`
- `• 공종 페이지 N개 추가`
- `• 공종 제목: "{폴더 이름} {숫자}구역"`

`N` is replaced with the actual number of work pages currently planned from the selected Rule 1 photos. The brace placeholders in the rule and title format remain literal explanatory text.

## Behavior

- Keep the existing collapsed summary text and expand/collapse interaction.
- Remove the previous long sentence-based Rule 1 details.
- Keep the current colors, button, selection behavior, and planned-group calculation.
- Do not change the normal manual-selection details.

## Verification

- Build the Avalonia application with zero errors.
- Verify expanded Rule 1 details contain exactly three bullet lines.
- Verify the second bullet displays the current planned work-page count.
- Verify manual-selection details remain unchanged.
