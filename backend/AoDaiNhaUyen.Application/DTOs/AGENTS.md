<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs

## Purpose
Data Transfer Objects for all API request/response payloads. All DTOs are `sealed record` types — immutable, serialization-friendly shapes used at the API boundary. Organized by domain area: root DTOs cover catalog, AI try-on, chat, and cross-cutting concerns; subdirectories cover Admin, Auth, Blog, Cart, Checkout, Dashboard, Marketing, Order, Promo, and User.

## Root DTOs
| File | Description |
|------|-------------|
| `AiTryOnAccessoryImageDto.cs` | Accessory image for try-on: Id, byte content, ContentType |
| `AiTryOnCatalogDto.cs` | Try-on catalog response: garment and accessory lists |
| `AiTryOnCatalogItemDto.cs` | Single catalog item: ProductId, Name, Slug, AssetUrl, AssetKind |
| `AiTryOnFeedbackDtos.cs` | Feedback request/response for AI try-on results |
| `AiTryOnRequestDto.cs` | Raw try-on request with person/garment images |
| `AiTryOnResultDto.cs` | Try-on result: generated image URL, status, processing time |
| `CatalogAiTryOnRequestDto.cs` | Catalog-level try-on: GarmentId/ProductId, person image bytes, accessory images |
| `CategoryDto.cs` | Flat category: Id, Parent, Name, Slug, Description, ImageUrl, SortOrder, IsActive |
| `CategoryTreeDto.cs` | Tree node: Id, Name, Slug, SortOrder, Children (IReadOnlyList of CategoryTreeChildDto) |
| `ChatAttachmentDto.cs` | Chat attachment: Id, Kind, FileUrl, MimeType, OriginalFileName, FileSizeBytes |
| `ChatMessageDto.cs` | Chat message: Id, Role, Content, Intent, ClientMessageId, CreatedAt, Attachments, StructuredPayload |
| `ChatRecommendationItemDto.cs` | Stylist product recommendation: ProductId, Name, Slug, ImageUrl, Price, SalePrice, MatchReason |
| `ChatStructuredPayloadDto.cs` | Structured response envelope: Recommendations, TryOnResult, ReasoningText |
| `ChatThreadDetailDto.cs` | Full thread: Id, Status, CreatedAt, Messages list, memory state |
| `ChatThreadSummaryDto.cs` | Thread list item: Id, LastMessagePreview, MessageCount, CreatedAt, UpdatedAt |
| `CommentDto.cs` | Public comment: Id, AuthorName, Content, CreatedAt |
| `CreateCommentRequest.cs` | Create comment request: ProductId, Content |
| `CreateReviewRequest.cs` | Create review request: ProductId, Rating, Content |
| `FusionCacheSettings.cs` | Cache duration settings DTO (used for runtime configuration) |
| `ImageReferenceDto.cs` | Image reference before AI processing: URL/source metadata |
| `ImageValidationResultDto.cs` | Image validation result: status, reason, normalized metadata |
| `IncomingChatAttachmentDto.cs` | Client upload: Kind, FileName, ContentType, byte[] Content |
| `IntentClassificationDto.cs` | Classification result: Intent, Scenario, BudgetCeiling, ColorFamily, MaterialKeyword, ProductIds, RequiresPersonImage |
| `LowStockAlertDto.cs` | Low-stock alert payload: ProductId, VariantId, Sku, StockQty, Threshold |
| `PagedResult.cs` | Generic paged container: Items (IReadOnlyList\<T\>), TotalCount, Page, PageSize |
| `ProductDetailDto.cs` | Full product detail: all fields + Variants + Images + ReviewSummary |
| `ProductImageDto.cs` | Product image: ImageUrl, AltText, SortOrder, IsPrimary |
| `ProductImageVisibilityDto.cs` | Image visibility state: ImageId, IsPublic, PublicObjectKey |
| `ProductListItemDto.cs` | Product list item: Id, Name, Slug, type, status, price, CategorySlug, stock, image, rating |
| `ProductVariantDto.cs` | Variant: Id, Sku, VariantName, Size, Color, Price, SalePrice, StockQty, IsDefault, Status |
| `ReviewDto.cs` | Review: Id, AuthorName, Rating, Content, CreatedAt |
| `ReviewSummaryDto.cs` | Aggregated review stats: AverageRating, TotalReviews |
| `SseChatEvent.cs` | Server-sent event envelope for streaming chat responses |
| `ThreadMemoryStateDto.cs` | Chat memory: Scenario, BudgetCeiling, ColorFamily, MaterialKeyword, ShortlistedProductIds, SelectedGarmentProductId, SelectedAccessoryProductIds, LatestPersonAttachmentId, PendingTryOnRequirements |
| `UploadedFileResult.cs` | Result of a file upload: FileUrl, OriginalFileName, ContentType, FileSizeBytes |
| `UserImageDto.cs` | User-uploaded image reference: Id, FileUrl, CreatedAt |

## Subdirectories
| Directory | Domain |
|-----------|--------|
| `Admin/` | Admin panel — products, categories, promos, users, Hermes agent, AI, marketing, LLM audit |
| `Auth/` | Authentication — JWT results, session data, OAuth user info, token validation |
| `BlogPost/` | Blog — list items, full posts, block content, create/update requests, image uploads |
| `Cart/` | Shopping cart — add/update/remove items, cart view |
| `Checkout/` | Order placement — address, request, result |
| `Dashboard/` | Admin analytics — revenue, order distribution, top products, user growth |
| `Marketing/` | Subscribers, customer event tracking, promo performance metrics |
| `Order/` | Order management — status updates, shipment creation/updates |
| `Promo/` | Promo code validation results and apply requests |
| `User/` | User-facing — profile, addresses, order history |

## For AI Agents

### Working In This Directory
- All DTOs are `sealed record` — use positional syntax for simple read-only shapes, init-property syntax when `DataAnnotations` validation is needed
- DTOs are serialization boundaries — map to/from Domain entities inside services, never expose entities directly
- Result types follow the pattern `{ Succeeded, Value?, ErrorCode?, ErrorMessage? }` (see `AuthResult`, `AdminMutationResult`)
- `PagedResult<T>` is the standard paged response wrapper used with the API layer's `PaginatedApiResponse<T>`
- `ThreadMemoryStateDto` is the in-memory representation of `ChatThreadMemory` entity; `IntentClassificationDto` drives chat routing in `StylistChatService`

### Common Patterns
```csharp
// Simple positional record:
public sealed record CategoryDto(Guid Id, string Name, string Slug, int SortOrder);

// Init-property record with validation:
public sealed record CreateProductRequest
{
    [Required, MaxLength(300)]
    public required string Name { get; init; }
}
```

## Dependencies
### Internal
- Consumed by: `AoDaiNhaUyen.Api` controllers and `AoDaiNhaUyen.Application.Services`
- Mapped from: `AoDaiNhaUyen.Domain` entities inside service implementations

<!-- MANUAL: -->
