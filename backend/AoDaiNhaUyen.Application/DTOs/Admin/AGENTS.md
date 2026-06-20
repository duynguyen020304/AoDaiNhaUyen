<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/Admin

## Purpose
Admin panel DTOs for the full range of back-office operations: product and category management, promo codes, user and role administration, Hermes AI agent interaction, event outbox, feed, monitoring, LLM audit logs, AI try-on feedback, and marketing campaigns. All types are `sealed record`.

## Key Files
| File | Description |
|------|-------------|
| `AdminProductDTOs.cs` | `AdminProductListItemResponse`, `AdminProductDetailResponse`, `AdminVariantResponse`, `AdminImageResponse`, `CreateProductRequest`, `UpdateProductRequest`, `UpdateVariantStockRequest`, `ToggleProductStatusRequest` |
| `AdminCategoryDTOs.cs` | `AdminCategoryListItemResponse`, `AdminCategoryDetailResponse`, `CreateCategoryRequest`, `UpdateCategoryRequest` |
| `AdminPromoDTOs.cs` | Promo code list/detail responses, create/update requests, toggle status |
| `AdminUserListItemDto.cs` | User list item for admin user management grid |
| `CreateUserRequest.cs` | Admin create-user payload: email, password, roles |
| `UpdateUserRequest.cs` | Admin update-user payload: profile fields |
| `UpdateUserRoleRequest.cs` | Assign/remove roles for a user |
| `UpdateUserStatusRequest.cs` | Activate/deactivate a user account |
| `CreateRoleRequest.cs` | Create new role: name, description |
| `UpdateRoleRequest.cs` | Update existing role |
| `RoleDto.cs` | Role read model: Id, Name, Description |
| `AdminMutationResult.cs` | Standard mutation result: `Succeeded`, `ErrorCode?`, `ErrorMessage?`; factory methods `Success()` / `Failure(code, message)` |
| `AdminToolResult.cs` | Tool execution result for Hermes agent tool calls |
| `HermesAgentDtos.cs` | `HermesChatRequest`, `HermesHeartbeatRequest`, `HermesStatusResponse`, `HermesRunSummaryResponse`, `HermesReportRequest`, `HermesReportSearchRequest`, `HermesReportListItemResponse`, `HermesReportResponse`, `HermesStreamChunk` |
| `HermesEventOutboxDtos.cs` | Outbox event list/detail responses for admin outbox monitor |
| `HermesFeedDtos.cs` | Hermes live feed event shapes for SSE/polling |
| `HermesMonitorDtos.cs` | System monitor status DTOs for the Hermes control plane |
| `AdminAiDTOs.cs` | Admin AI try-on management: catalog admin responses, generation requests |
| `AdminAiTryOnFeedbackDtos.cs` | Admin view of AI try-on feedback submissions |
| `AdminMarketingDtos.cs` | Campaign list/detail, subscriber stats, send-job status |
| `LlmAuditLogDTOs.cs` | LLM call audit log list/detail for compliance review |

## For AI Agents

### Working In This Directory
- `AdminMutationResult` is the standard return type for all admin write operations — use `AdminMutationResult.Success()` / `AdminMutationResult.Failure(code, message)` factory methods
- Hermes DTOs map 1:1 to the Hermes API server protocol — do not change field names without coordinating with the runner
- Request DTOs with `[Required]` / `[MaxLength]` annotations are validated by ASP.NET model binding before reaching the controller action

### Common Patterns
```csharp
// Mutation result usage:
return AdminMutationResult.Failure("NOT_FOUND", "Product not found");

// Hermes stream chunk (SSE):
yield return new HermesStreamChunk("text", content);
```

## Dependencies
### Internal
- Consumed by: `IAdminProductService`, `IAdminCategoryService`, `IAdminUserService`, `IHermesAgentService`, and related admin service interfaces
- Returned by: `AoDaiNhaUyen.Api` admin controllers

<!-- MANUAL: -->
