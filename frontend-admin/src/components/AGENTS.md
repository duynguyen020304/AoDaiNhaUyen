<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-08 | Updated: 2026-06-08 -->

# frontend-admin/src/components

## Purpose
Admin UI components: shell, forms, tables, modals, AI chat, shadcn-style primitives.

## Where To Look
| Directory/File Area | Purpose |
|---------------------|---------|
| `ui/` | Primitive Button/Card/Input/Table/Badge/Sheet-like components |
| `admin/` | Admin-specific modals/forms/confirm flows |
| `ai/` | Admin AI chat/message/tool components |

## Local Conventions
- Tailwind v4 utilities; use shared primitives from `ui/` before creating new visual variants.
- Keep components presentational where possible; page/store owns data loading.
- Use `cn`/class merge helper from `lib/` if present.
- Accessibility: buttons need labels, modals need focus/close affordances, tables need useful headings.

## Anti-Patterns
- Do not bring CSS Modules from customer frontend.
- Do not embed API calls in reusable components unless component is intentionally smart.
- Avoid one-off colors; use theme tokens/classes.
