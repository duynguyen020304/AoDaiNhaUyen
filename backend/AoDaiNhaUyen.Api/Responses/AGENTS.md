<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# Responses

## Purpose
Defines the standard API response envelope used by every controller in the project. All HTTP responses (success, failure, paginated) must go through `ApiResponseFactory` to guarantee a consistent JSON shape: `{ success, message, data, errors, timestamp }`.

## Key Files
| File | Description |
|------|-------------|
| `ApiError.cs` | Sealed record `ApiError(string Code, string Message)`. Represents one error item in the `errors` array. |
| `ApiResponse.cs` | Sealed generic records `ApiResponse<T>` and `PaginatedApiResponse<T>`. `PaginatedApiResponse` adds `HasNextPage`, `HasPreviousPage`, `TotalPage`, `TotalItem`. |
| `ApiResponseFactory.cs` | Static factory with three methods: `Success<T>(data, message)`, `PaginatedSuccess<T>(data, page, pageSize, totalItem, message)`, `Failure(message, code, errorMessage)`. All set `Timestamp = DateTime.UtcNow`. |

## For AI Agents

### Working In This Directory
- Never return raw anonymous objects from controllers; always use `ApiResponseFactory`.
- `Failure` takes a single error code + message and wraps it in a one-element `errors` list — do not invent multi-error overloads without a design discussion.
- `PaginatedSuccess` calculates `TotalPage` from `totalItem / pageSize`; pass the raw count from the service, not a pre-computed page count.
- Response messages should be in Vietnamese (consistent with existing controllers).

### Common Patterns
```csharp
// Success
return Ok(ApiResponseFactory.Success(dto, "Lấy dữ liệu thành công."));

// Paginated
return Ok(ApiResponseFactory.PaginatedSuccess(items, page, pageSize, total));

// Failure
return BadRequest(ApiResponseFactory.Failure("Không hợp lệ.", "invalid_input", "Chi tiết lỗi."));
return NotFound(ApiResponseFactory.Failure("Không tìm thấy.", "not_found", "..."));
```

## Dependencies
### Internal
- Used by all controllers and `ExceptionHandlingMiddleware`
### External
- None (pure BCL)

<!-- MANUAL: -->
