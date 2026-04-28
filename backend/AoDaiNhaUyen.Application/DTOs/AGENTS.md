<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# DTOs

## Purpose
Data Transfer Objects for all API request/response payloads. All DTOs immutable `sealed record` types. Organized by domain: Auth, Cart, Checkout, User, plus root catalog/chat/AI DTOs.

## Root DTOs
| File | Description |
|------|-------------|
| `AiTryOnAccessoryImageDto.cs` | Accessory image data for try-on: Id, byte content, ContentType |
| `AiTryOnCatalogDto.cs` | Try-on catalog response: garment and accessory lists |
| `AiTryOnCatalogItemDto.cs` | Single catalog item: ProductId, Name, Slug, AssetUrl, AssetKind |
| `AiTryOnRequestDto.cs` | Original try-on request with person/garment images |
| `AiTryOnResultDto.cs` | Try-on result: generated image URL, status, processing time |
| `CatalogAiTryOnRequestDto.cs` | Catalog try-on request: GarmentId/ProductId, person image bytes, accessory images |
| `CategoryDto.cs` | Flat category: Id, Parent, Name, Slug, Description, ImageUrl, SortOrder, IsActive |
| `CategoryTreeDto.cs` | Tree node: Id, Name, Slug, SortOrder, Children (list of CategoryTreeChildDto) |
| `ChatAttachmentDto.cs` | Chat attachment: Id, Kind, FileUrl, MimeType, OriginalFileName, FileSizeBytes |
| `ChatMessageDto.cs` | Chat message: Id, Role, Content, Intent, ClientMessageId, CreatedAt, Attachments, StructuredPayload |
| `ChatRecommendationItemDto.cs` | Stylist product recommendation: ProductId, Name, Slug, ImageUrl, Price, SalePrice, MatchReason |
| `ChatStructuredPayloadDto.cs` | Structured response: Recommendations, TryOnResult, ReasoningText |
| `ChatThreadDetailDto.cs` | Full thread: Id, Status, CreatedAt, Messages (list), Memory state |
| `ChatThreadSummaryDto.cs` | Thread list item: Id, LastMessagePreview, MessageCount, CreatedAt, UpdatedAt |
| `ImageReferenceDto.cs` | Image reference for validation: URL/source metadata before AI processing |
| `ImageValidationResultDto.cs` | Image validation result: status, reason, normalized image metadata |
| `IncomingChatAttachmentDto.cs` | Client upload: Kind, FileName, ContentType, byte[] Content |
| `IntentClassificationDto.cs` | Classification result: Intent, Scenario, BudgetCeiling, ColorFamily, MaterialKeyword, ProductIds, RequiresPersonImage |
| `PagedResult.cs` | Generic paginated container: Items (IReadOnlyList<T>), TotalCount, Page, PageSize |
| `ProductDetailDto.cs` | Full product detail: all fields + Variants list + Images list |
| `ProductImageDto.cs` | Product image: ImageUrl, AltText, SortOrder, IsPrimary |
| `ProductListItemDto.cs` | Product list item: Id, Name, Slug, ProductType, Status, price info, CategorySlug, stock, primary image |
| `ProductVariantDto.cs` | Variant: Id, Sku, VariantName, Size, Color, Price, SalePrice, StockQty, IsDefault, Status |
| `SseChatEvent.cs` | Server-sent chat event envelope for streaming chat responses |
| `ThreadMemoryStateDto.cs` | Chat memory: Scenario, BudgetCeiling, ColorFamily, MaterialKeyword, ShortlistedProductIds, SelectedGarmentProductId, SelectedAccessoryProductIds, LatestPersonAttachmentId, PendingTryOnRequirements |

## Auth/ Subdirectory
| File | Description |
|------|-------------|
| `AuthResult.cs` | Auth result: Succeeded, Value (AccessToken + RefreshToken + User), ErrorCode, ErrorMessage |
| `AuthSessionDto.cs` | Session data: Token, ExpiresAt |
| `AuthUserDto.cs` | Auth user: Id, FullName, Email, Phone, Roles |
| `FacebookUserInfoDto.cs` | Facebook user: Subject, Email, EmailVerified, Name, Picture |
| `GoogleUserInfoDto.cs` | Google user: Subject, Email, EmailVerified, Name, Picture |
| `TokenValidationResult.cs` | Token validation status enum and result types for email verification and password reset |

## Cart/ Subdirectory
| File | Description |
|------|-------------|
| `AddCartItemDto.cs` | Add to cart: VariantId, Quantity |
| `CartDto.cs` | Cart response: Id, Items list, Total |
| `CartItemDto.cs` | Cart item: Id, VariantId, ProductName, Size, Color, Price, Quantity, LineTotal, ImageUrl |
| `UpdateCartItemDto.cs` | Update cart item: Quantity |

## Checkout/ Subdirectory
| File | Description |
|------|-------------|
| `CheckoutAddressDto.cs` | Shipping address: RecipientName, Phone, Province, District, Ward, AddressLine |
| `CheckoutRequestDto.cs` | Checkout request: Address, Items, Note |
| `CheckoutResultDto.cs` | Checkout result: OrderCode, OrderId |

## User/ Subdirectory
| File | Description |
|------|-------------|
| `CreateAddressDto.cs` | Create address request: RecipientName, Phone, Province, District, Ward, AddressLine, IsDefault |
| `OrderItemDto.cs` | Order item: ProductName, Sku, Size, Color, UnitPrice, Quantity, LineTotal |
| `UserAddressDto.cs` | User address: Id, RecipientName, Phone, Province, District, Ward, AddressLine, IsDefault |
| `UserOrderDto.cs` | User order with items: OrderCode, Status, amounts, items list, dates |
| `UserProfileDto.cs` | Profile read/update: FullName, Phone, Gender, DateOfBirth, AvatarUrl |

## For AI Agents
### Working In This Directory
- All DTOs are `sealed record` types -- use positional or nominal record syntax
- DTOs are serialization boundary -- map to/from entity objects in services
- Result types (AuthResult, etc.) follow standard pattern: `Succeeded`, `Value`, `ErrorCode`, `ErrorMessage`
- PagedResult<T> used with PaginatedApiResponse<T> in API layer
- ThreadMemoryStateDto is in-memory representation of ChatThreadMemory entity
- IntentClassificationDto drives chat intent routing in StylistChatService