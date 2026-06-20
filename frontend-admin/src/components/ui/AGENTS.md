<!-- Parent: ../../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/components/ui

## Purpose
shadcn/ui-style primitive components. These are the foundational building blocks used across all admin components and pages. They wrap HTML elements with consistent Tailwind styling, variant support via `class-variance-authority`, and accessible defaults.

## Key Files
| File | Description |
|------|-------------|
| `button.tsx` | `Button` component; variants: `default`, `destructive`, `outline`, `ghost`, `link`; sizes: `default`, `sm`, `lg`, `icon` |
| `card.tsx` | `Card`, `CardHeader`, `CardTitle`, `CardContent`, `CardFooter` composable card primitives |
| `input.tsx` | `Input` — styled `<input>` with focus ring and disabled state |
| `label.tsx` | `Label` — styled `<label>` with `htmlFor` forwarding |
| `textarea.tsx` | `Textarea` — styled `<textarea>` |
| `select.tsx` | `Select`, `SelectTrigger`, `SelectValue`, `SelectContent`, `SelectItem` — custom select using Radix or native |
| `badge.tsx` | `Badge` — small status/label chip; variants: `default`, `secondary`, `destructive`, `outline` |
| `table.tsx` | `Table`, `TableHeader`, `TableBody`, `TableRow`, `TableHead`, `TableCell` — semantic table wrappers |
| `sheet.tsx` | `Sheet`, `SheetContent`, `SheetHeader`, `SheetTitle`, `SheetDescription` — slide-over drawer panel |
| `checkbox.tsx` | `Checkbox` — styled checkbox with `checked`/`onCheckedChange` |
| `feedback.tsx` | `FeedbackProvider` — context provider for toast notifications and confirm dialogs |
| `feedbackContext.ts` | `FeedbackContext`, `useFeedback()` hook, `ConfirmOptions` type |

## For AI Agents
### Working In This Directory
- Import from `@/components/ui/<name>` — never copy-paste primitives into feature components.
- Use `cn()` from `@/lib/utils` to merge variant classes with custom overrides: `className={cn(buttonVariants({ variant }), className)}`.
- `useFeedback()` returns `{ toast, confirm }` — use `toast(message, 'success'|'error'|'info')` and `await confirm({ title, message, confirmText, cancelText, destructive })`.
- Toast auto-dismisses after 3.5 s; `confirm` returns a `Promise<boolean>`.
- `FeedbackProvider` must wrap the app in `main.tsx` — do not nest a second provider.
- `Sheet` z-index is lower than `FeedbackProvider` confirm dialog (`z-[80]`) — keep this hierarchy.

### Common Patterns
- Adding a new variant to `Button`: add to the `cva` definition in `button.tsx`; do not create a new component.
- Form fields: always pair `Input`/`Select`/`Textarea` with a `Label` using matching `id`/`htmlFor`.
- Destructive confirm: pass `destructive: true` to `confirm()` to get a red confirm button.

## Dependencies
### Internal
- `@/lib/utils` — `cn()` for class merging

### External
- class-variance-authority (`cva`, `VariantProps`)
- clsx, tailwind-merge (via `cn()`)
- lucide-react (icons used inside primitives)

<!-- MANUAL: -->
