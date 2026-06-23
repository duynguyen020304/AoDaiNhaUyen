<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-23 | Updated: 2026-06-23 -->

# assets

## Purpose
Root asset bucket for static images served from `/assets/...`. Holds login imagery and other shared media used by root-level pages or styles.

## Key Files
| File | Description |
|------|-------------|
| `AGENTS.md` | Directory guidance for root static assets |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `login/` | Login page icons and artwork (see `login/AGENTS.md`) |

## For AI Agents
### Working In This Directory
- Keep file names lowercase, descriptive, and stable
- Match existing absolute asset path convention: `/assets/...`
- Add only assets referenced by code or HTML templates

### Testing Requirements
- Verify referenced images load in browser after changes
- Check size and format before adding new media

### Common Patterns
- SVG icons for UI chrome
- Small static image assets for login and shared surfaces

## Dependencies
### Internal
- Root `public/` directory and root HTML/pages that reference `/assets/...`

### External
- Vite/static asset serving

<!-- MANUAL: -->