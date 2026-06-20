<!-- Parent: ../../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/components/blog

## Purpose
Blog CMS editor and preview components used by `BlogFormPage`. `BlockEditor` provides a structured block-based content editor; `BlogPreview` renders the saved content as a styled read-only preview.

## Key Files
| File | Description |
|------|-------------|
| `BlockEditor.tsx` | Block-based rich content editor: add/remove/reorder content blocks (text, heading, image, etc.) |
| `BlogPreview.tsx` | Read-only preview of blog post content; renders markdown or block structures with styles matching the customer frontend |

## For AI Agents
### Working In This Directory
- Blog content is stored as a structured block array (see `@/types/blog`), not raw HTML.
- `BlockEditor` receives `value` and `onChange` props from `BlogFormPage`; it owns no store state.
- `BlogPreview` is purely presentational — pass it the same block array and it renders a styled view.
- AI-generated blog drafts are handed off via `sessionStorage` (key `AI_BLOG_DRAFT_STORAGE_KEY` from `@/types/blog`); `BlogFormPage` reads this on mount to pre-populate the editor.

### Common Patterns
- New block types: add to the block type union in `@/types/blog`, handle in both `BlockEditor` (edit view) and `BlogPreview` (render view).
- Use `react-markdown` + `remark-gfm` for any markdown block rendering inside `BlogPreview`.

## Dependencies
### Internal
- `@/types/blog` — block content types, `AI_BLOG_DRAFT_STORAGE_KEY`, `AiBlogDraft`
- `@/components/ui/*` — Button, Input, Textarea primitives

### External
- react-markdown + remark-gfm (markdown block rendering)
- lucide-react (block action icons)

<!-- MANUAL: -->
