<!-- Parent: ../../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# frontend-admin/src/components/admin

## Purpose
Domain-specific CRUD modals and forms for admin entities. Each modal handles create and edit in one component, using controlled form state and calling the appropriate store action on submit.

## Key Files
| File | Description |
|------|-------------|
| `ModalOverlay.tsx` | Shared backdrop/z-index wrapper used by all modals in this directory |
| `CategoryFormModal.tsx` | Create/edit category modal |
| `DeleteConfirmModal.tsx` | Generic delete confirmation dialog (title + message + confirm/cancel) |
| `EmailTemplateFormModal.tsx` | Create/edit email template modal with subject, preheader, and HTML body fields |
| `PromoFormModal.tsx` | Create/edit promo code modal |
| `RoleFormModal.tsx` | Create/edit role modal |
| `UserFormModal.tsx` | Create/edit admin user modal |

## For AI Agents
### Working In This Directory
- All modals receive `open: boolean` and `onClose: () => void` props; the parent page controls visibility.
- Use `ModalOverlay` for consistent backdrop, z-index (`z-50`), and close-on-backdrop-click behavior.
- Form submission calls the relevant store action (e.g. `categoryStore.create()`); on success, call `onClose()`.
- Errors from store actions are displayed inline in the modal, not via toast.
- `DeleteConfirmModal` is generic — pass `title`, `message`, `onConfirm`, `isLoading` props.

### Common Patterns
- Edit mode: detect via presence of an `initialData` prop; use it to pre-fill form state.
- Prevent modal body scroll bleed: `ModalOverlay` handles `overflow-hidden` on mount.
- All labels and button text are in Vietnamese.

## Dependencies
### Internal
- `@/stores/*` — mutation actions
- `@/components/ui/*` — Button, Input, Label, Select, Textarea primitives
- `@/types/*` — entity DTO shapes for form state

### External
- lucide-react (close icon)

<!-- MANUAL: -->
