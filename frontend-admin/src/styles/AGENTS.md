<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/styles

## Purpose
Global Tailwind v4 stylesheet. Defines the design token layer: custom CSS properties for brand colors, typography, and theme variables consumed by Tailwind utilities and component classes throughout the admin SPA.

## Key Files
| File | Description |
|------|-------------|
| `globals.css` | Tailwind v4 `@theme` block with brand color tokens (burgundy, wine, ink, primary, etc.), base resets, and any global element styles |

## For AI Agents
### Working In This Directory
- This is the single source of truth for brand colors — add new tokens here, then reference them via Tailwind utilities (e.g. `text-primary`, `bg-wine`).
- Tailwind v4 uses `@theme { --color-*: ...; }` syntax — not `tailwind.config.js` extend.
- Do not add component-specific styles here; use Tailwind utilities inline in components.
- Do not import CSS Modules or create additional `.css` files; `globals.css` is the only stylesheet.
- `globals.css` is imported once in `main.tsx`.

### Common Patterns
- Adding a new brand color: add `--color-<name>: <value>;` inside `@theme {}`, then use `text-<name>` / `bg-<name>` in components.
- Dark mode: if added, use the Tailwind v4 `@variant dark` approach, not separate class overrides.

## Dependencies
### Internal
- Imported by `src/main.tsx`

### External
- `@tailwindcss/vite` (Vite plugin processes this file)
- tw-animate-css (animation utilities referenced in theme)

<!-- MANUAL: -->
