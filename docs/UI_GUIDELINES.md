# UI Guidelines

## Design Principles

1. **Piano-first** — Grand Staff is the default and primary view.
2. **Score over chrome** — Maximise canvas area; minimise toolbar clutter.
3. **Responsive** — Panels dock and collapse; zoom from 25% to 800%.
4. **Fast** — Layout computes < 50ms; no jank during note entry.
5. **Accessible** — Full keyboard navigation, screen reader labels, colorblind modes.

---

## Colour Palette (Dark Theme)

| Token | Hex | Usage |
|---|---|---|
| `SurfaceBackground` | `#1E1E1E` | Window background |
| `EditorBackground` | `#252526` | Score canvas background |
| `PanelBackground` | `#2D2D2D` | Side panels (Mixer, Properties) |
| `ToolbarBackground` | `#333333` | Toolbars |
| `MenuBackground` | `#2C2C2C` | Menus |
| `StatusBarBackground` | `#007ACC` | Status bar |
| `CardBackground` | `#3A3A3A` | Cards within panels |
| `ForegroundColor` | `#D4D4D4` | Primary text |
| `ForegroundDimColor` | `#858585` | Secondary / dim text |
| `AccentColor` | `#007ACC` | Active state, buttons |
| `SelectionColor` | `#264F78` | Selected note, active item |
| `BorderBrush` | `#464646` | Dividers, borders |

---

## Score Rendering Colours

| Element | Dark | Light |
|---|---|---|
| Staff lines | `#A0A0A0` | `#404040` |
| Note heads | `#FFFFFF` | `#000000` |
| Right hand | `#6495ED` (cornflower) | `#1565C0` |
| Left hand | `#CD5C5C` (Indian red) | `#B71C1C` |
| Selection | `#6495EDAA` | `#1565C080` |
| Barlines | `#8C8C8C` | `#333333` |
| Ledger lines | `#8C8C8C` | `#555555` |

---

## Typography

- **Score font**: Bravura (SMuFL) or Leland (for notation glyphs)
- **UI font**: Inter (via Avalonia.Fonts.Inter)
- **Monospace**: JetBrains Mono (for settings/git history)

---

## Layout

- **Main toolbar height**: 44px
- **Note input toolbar height**: 40px
- **Piano keyboard height**: 120px (collapsible)
- **Mixer panel width**: 220px (collapsible)
- **Status bar height**: 24px

---

## Accessibility

- All interactive controls must have `ToolTip.Tip`.
- Use `AutomationProperties.Name` on custom controls.
- Keyboard: Tab order follows visual reading order (left-to-right, top-to-bottom).
- Contrast ratio ≥ 4.5:1 (WCAG AA) for all text.
- Colorblind mode: replace hand colours with patterns or symbols.
