# Human-Crafted UI Design System (Anti-AI Aesthetic)

This workspace rule enforces modern, human-crafted design standards for desktop applications, avoiding generic "AI-generated" UI patterns.

## 1. Principles: Human Craft vs. "AI-Looking" UI
- **Never use unstyled default controls**: Avoid default gray square checkboxes, default native radio circles, and raw WinForms/stock WPF buttons.
- **Micro-components**:
  - Use **Segmented Controls** (pill containers with smooth switching) instead of grouped RadioButtons.
  - Use **Modern Fluent Toggle Switches** (pill tracks with sliding circular thumbs) instead of square CheckBoxes.
  - Use **Interactive Color Swatches** with white selection rings and live hex chips.
- **Elevation & Surfaces**:
  - Root Canvas: Deep slate `#0D0E12` / `#121318`.
  - Surface Cards: `#16171E` / `#1A1B22` with soft `CornerRadius = 12` to `14`.
  - Borders: 1px subtle borders with low-opacity white (`rgba(255, 255, 255, 0.08)` to `0.14`).
- **Typography & Hierarchy**:
  - Title: 20-22px Bold with accent pill badges.
  - Section Headers: 10-11px Bold UPPERCASE with muted `#9CA3AF`.
  - Body: 12-13px Medium with `#E5E7EB`.
  - Microcopy / Hints: 10-11px `#9CA3AF`.
