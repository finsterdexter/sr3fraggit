# SR3 Character Generator Design System

## Intent
**Who:** Tabletop gamers building Shadowrun 3rd Edition characters. Familiar with crunchy RPG systems, comfortable with dense data.
**Task:** Allocate resources across multiple interdependent pools (attributes, skills, nuyen, spell points) while respecting priority constraints.
**Feel:** Dense like a spreadsheet. Clinical like a character sheet. Cyberpunk edge without being garish.

## Direction
Dark, dense interface inspired by 90s cyberpunk sourcebooks and modern data tools. Information-rich layouts that respect the player's intelligence. Every pixel earns its place.

## Palette

### Surfaces (Dark Mode)
- `--surface-base`: `#0d0d0f` — App canvas, true dark
- `--surface-100`: `#141416` — Primary panels
- `--surface-200`: `#1a1a1d` — Elevated cards, dropdowns
- `--surface-300`: `#222225` — Hover states, active items
- `--surface-inset`: `#0a0a0b` — Recessed areas, inputs

### Foreground
- `--ink-primary`: `#e8e8eb` — Primary text
- `--ink-secondary`: `#a0a0a8` — Secondary text, labels
- `--ink-muted`: `#606068` — Disabled, placeholder
- `--ink-faint`: `#404048` — Decorative, dividers

### Borders
- `--border-default`: `rgba(255, 255, 255, 0.08)`
- `--border-subtle`: `rgba(255, 255, 255, 0.04)`
- `--border-strong`: `rgba(255, 255, 255, 0.12)`

### Accents
- `--accent-cyber`: `#00d4ff` — Cyberware, mundane tech, primary actions
- `--accent-mana`: `#c084fc` — Magic, spells, awakened content
- `--accent-nuyen`: `#fbbf24` — Money, resources
- `--accent-karma`: `#22c55e` — Karma, success states

### Semantic
- `--destructive`: `#ef4444`
- `--warning`: `#f59e0b`
- `--success`: `#22c55e`

## Typography

**Font Stack:** `"JetBrains Mono", "Cascadia Code", ui-monospace, monospace` for data, `Inter, system-ui, sans-serif` for UI labels

### Scale
- `--text-xs`: 10px — Tiny labels, counts
- `--text-sm`: 11px — Secondary data, metadata
- `--text-base`: 12px — Primary data, body
- `--text-lg`: 14px — Section headers
- `--text-xl`: 16px — Page titles
- `--text-2xl`: 20px — Major headings (rare)

### Weights
- Regular (400) for body
- Medium (500) for labels
- SemiBold (600) for emphasis
- Bold (700) for key numbers

## Spacing
Base unit: **4px**

- `--space-1`: 4px — Micro gaps
- `--space-2`: 8px — Element spacing
- `--space-3`: 12px — Component padding
- `--space-4`: 16px — Section gaps
- `--space-6`: 24px — Major separation

## Depth Strategy
**Borders-only** with subtle surface shifts. No shadows. This is a data tool.

- Cards: 1px border at `--border-default`
- Sections: No border, just surface color shift
- Focus: `--accent-cyber` ring at 1px

## Border Radius
Minimal. This is technical UI.

- `--radius-sm`: 2px — Inputs, small buttons
- `--radius-md`: 4px — Cards, panels
- `--radius-lg`: 6px — Modals (rare)

## Component Patterns

### Resource Bar (Header)
Horizontal strip showing all pools. Each pool displays:
- Label (muted)
- Current value (bold, accent color if depleted)
- Max value (secondary)
- Optional micro progress indicator

### Data Row
Tight horizontal layout for list items:
- Name (primary text, left-aligned)
- Key stat (accent color, fixed width)
- Secondary stats (muted, right side)
- Compact height (24-28px)

### Compact Input
- Height: 24px
- Padding: 4px 8px
- Border: 1px `--border-default`
- Background: `--surface-inset`

### Section Header
- Text: `--text-lg`, weight 600
- Color: `--ink-secondary`
- Bottom border: 1px `--border-subtle`
- Margin bottom: `--space-2`

### Tab Bar
Horizontal, compact tabs with:
- Text: `--text-sm`
- Active: `--accent-cyber` underline
- Inactive: `--ink-muted`

## Layout Principles

1. **No wasted space** — Every margin earns its place
2. **Scannable columns** — Align data for quick comparison
3. **Resources always visible** — Header bar persists
4. **Progressive disclosure** — Details expand, don't navigate
5. **Keyboard-first** — Tab order matters, focus states clear

## Signature Elements

1. **Priority Matrix** — 5x5 grid showing current allocation, swappable
2. **Essence Tracker** — Shows 6.0 depleting as cyber is added
3. **Dual-list Pickers** — Available | Controls | Selected pattern
4. **Inline Calculations** — Show formulas alongside values

## Animation
Minimal. 100ms transitions on hover/focus. No spring, no bounce.
