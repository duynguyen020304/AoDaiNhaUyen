<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# styles

## Purpose
Global CSS files load in `main.tsx` via `import './styles/global.css'`. Provides design tokens (CSS custom properties), CSS reset, typography classes, texture overlays, transition utilities. No Tailwind -- all styling raw CSS with PostCSS (nested, autoprefixer, cssnano).

## Key Files
| File | Description |
|------|-------------|
| `variables.css` | Design tokens as CSS custom properties on `:root`. Defines brand color palette: `--burgundy` (#721311), `--deep-red` (#730707), `--panel-red` (#870e0b), `--wine` (#4f0d0c), `--bright-red` (#e3302c), `--gold` (#ffd400), `--cream` (#f0f0f0), `--ink` (#101828), `--muted` (#4a5565). Also AI page colors, gold gradient, home drum animation variables. Sets base font-family to Inter |
| `reset.css` | Minimal CSS reset: box-sizing border-box, smooth scrolling, body margin 0 with deep-red background, link/button/img resets, `.sr-only` utility class for screen readers |
| `global.css` | Import hub: imports variables.css, reset.css, texture.css, typography.css, transitions.css in order |
| `texture.css` | `.red-texture` class: red gradient overlay with floral pattern background and radial gradient pseudo-element for decorative sections |
| `typography.css` | `.script-title` class for large decorative headings using Italianno font (152px, gold gradient text), `.gold` utility class for gold gradient text effect |
| `transitions.css` | `.hover-lift` utility class: 180ms transform transition on hover with 1px upward lift |

## For AI Agents
### Design System Tokens (from variables.css)
- **Primary palette**: burgundy, deep-red, panel-red, wine, bright-red
- **Accent**: gold (#ffd400) with `--gold-gradient` (multi-stop gold gradient)
- **Neutrals**: ink (#101828), muted (#4a5565), cream (#f0f0f0)
- **AI page tokens**: ai-page-bg (#f9fafb), ai-badge-bg (#eff6ff), ai-upload-border, ai-placeholder-text, ai-helper-text, ai-upload-label, ai-btn-bg, ai-active-border

### CSS Loading Order
1. `variables.css` -- tokens load first
2. `reset.css` -- normalize browser defaults
3. `texture.css` -- decorative texture classes
4. `typography.css` -- font and text utility classes
5. `transitions.css` -- hover/transition utilities

### Usage in Components
- Reference tokens via `var(--burgundy)`, `var(--gold)`, etc.
- Use `.red-texture` class for decorative red gradient sections
- Use `.script-title` for large hero headings
- Use `.hover-lift` for interactive elements with lift effect
- Component-specific styles live in `.module.css` files alongside each component