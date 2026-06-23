<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-23 | Updated: 2026-06-23 -->

# public

## Purpose
Vite static assets for admin SPA. Served directly at build root and used by admin HTML entry and favicons.

## Key Files
| File | Description |
|------|-------------|
| `favicon.svg` | Browser tab icon |
| `logo.svg` | Brand logo asset |
| `icons.svg` | Shared icon sprite or static icon sheet |

## For AI Agents
### Working In This Directory
- Keep assets small and optimized
- Prefer SVG for logos and icons
- Do not put app code here

### Testing Requirements
- Verify favicon/logo display in browser after changes
- Confirm file paths stay valid in `index.html`

### Common Patterns
- Simple SVG brand assets
- Static build-root files

## Dependencies
### Internal
- `index.html` and admin app branding usage

### External
- Vite static asset pipeline

<!-- MANUAL: -->