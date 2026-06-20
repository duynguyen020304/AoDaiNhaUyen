<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Controllers/Admin

## Purpose
Admin-only HTTP endpoints. All controllers in this directory require the `RequireAdminRole` authorization policy (JWT bearer or Hermes API key). Controllers are thin: they parse/validate requests, call Application service interfaces, and return `ApiResponseFactory` envelopes. No business logic lives here.

## Key Files
| File | Route | Description |
|------|-------|-------------|
| `AdminProductsController.cs` | `api/admin/products` | Full product CRUD, variant stock updates, status toggle, soft-delete/restore, image upload/delete/primary/visibility. |
| `AdminCategoriesController.cs` | `api/admin/categories` | Category CRUD, soft-delete, restore. |
| `AdminOrdersController.cs` | `api/admin/orders` | Order status update, shipment creation, shipment status update. |
| `AdminUsersController.cs` | `api/admin/users` | Paginated user list, user detail, create/update, soft-delete/restore, role and status patch. |
| `AdminRolesController.cs` | `api/admin/roles` | Role list, create, update, delete. |
| `AdminMediaController.cs` | `api/admin/media` | Paginated media list, detail, delete, stats. |
| `AdminInventoryController.cs` | `api/admin/inventory` | Low-stock alert list. |
| `AdminPromosController.cs` | `api/admin/promos` | Promo CRUD, status toggle, soft-delete/restore, performance stats. |
| `AdminReviewsController.cs` | `api/admin/reviews` | Admin review management. |
| `AdminDashboardController.cs` | `api/admin/dashboard` | Summary, revenue, orders-by-status, recent orders, top products, user growth charts. |
| `AdminMarketingController.cs` | `api/admin/email-templates`, `api/admin/subscribers`, `api/admin/email-jobs`, `api/admin/marketing` | Email template CRUD, subscriber management/import, email job management/retry/cancel, marketing stats. (Four controller classes in one file.) |
| `AdminAiController.cs` | `api/admin/ai` | SSE AI chat streaming, conversation list/detail/delete, pending action confirm/reject, auto-mode toggle/status, store health score. |
| `AdminHermesController.cs` | `api/admin/hermes` | Hermes agent status/runs, report list/detail, event outbox list/detail/retry/cancel, live feed snapshot + SSE stream, monitor-link create/revoke, Hermes chat stream, heartbeat callback, report callback. |
| `AdminLlmLogsController.cs` | `api/admin/llm-logs` | LLM audit log search, stats, detail. |
| `AdminToolRiskController.cs` | `api/admin/tools-risk` | Tool risk config list and update. |
| `AdminAiTryOnFeedbackController.cs` | `api/admin/ai-tryon-feedback` | Admin view of AI try-on feedback submissions. |

## For AI Agents

### Working In This Directory
- All controllers use `[ApiController]`, `[Route(...)]`, primary constructor DI, and `[Authorize(Policy = "RequireAdminRole")]` at class level unless overriding per-action (e.g., Hermes callbacks use `HermesAdminAuthOptions.SchemeName`).
- Extract `Guid adminUserId` via `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`; return 401 if null.
- SSE endpoints set `Response.ContentType = "text/event-stream"`, `Cache-Control: no-cache`, `Connection: keep-alive`, `X-Accel-Buffering: no`; terminate with `data: [DONE]\n\n`.
- Hermes callback endpoints (`/heartbeat`, `/report`) use `[Authorize(AuthenticationSchemes = HermesAdminAuthOptions.SchemeName)]` instead of the JWT policy.

### Common Patterns
- `return Ok(ApiResponseFactory.Success(dto, "Vietnamese message."));`
- `return NotFound(ApiResponseFactory.Failure("...", "not_found", "..."));`
- `return BadRequest(ApiResponseFactory.Failure("...", "invalid_...", ex.Message));`
- Paginated: `return Ok(ApiResponseFactory.PaginatedSuccess(result.Items, result.Page, result.PageSize, result.TotalCount, "..."));`

## Dependencies
### Internal
- `AoDaiNhaUyen.Application.Interfaces.Services.*` — all admin service interfaces
- `AoDaiNhaUyen.Api.Responses.ApiResponseFactory`
- `AoDaiNhaUyen.Api.Authentication.HermesAdminAuthOptions`
### External
- `Microsoft.AspNetCore.Authorization`
- `Microsoft.AspNetCore.RateLimiting`

<!-- MANUAL: -->
