<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# DTOs

## Purpose
Data Transfer Objects for all API request/response payloads. All DTOs are immutable `sealed record` types. Organized by domain: Auth, Cart, Checkout, User, and root-level catalog/chat/AI DTOs.

## Root DTOs
| File | Description |
|------|-------------|
| `AiTryOnAccessoryImageDto.cs` | Accessory image data for try-on: Id, byte content, ContentType |
| `AiTryOnCatalogDto.cs` | Try-on catalog response: lists of garments and accessories |
| `AiTryOnCatalogItemDto.cs` | Single catalog item: ProductId, Name, Slug, AssetUrl, AssetKind |
| `AiTryOnRequestDto.cs` | Original try-on request with person/garment images |
| `AiTryOnResultDto.cs` | Try-on result: generated image URL, status, processing time |
| `CatalogAiTryOnRequestDto.cs` | Catalog-based try-on request: GarmentId/ProductId, person image bytes, accessory images |
| `CategoryDto.cs` | Flat category: Id, Parent, Name, Slug, Description, ImageUrl, SortOrder, IsActive |
| `CategoryTreeDto.cs` | Hierarchical tree node: Id, Name, Slug, SortOrder, Children (list of CategoryTreeChildDto) |
| `ChatAttachmentDto.cs` | Chat attachment: Id, Kind, FileUrl, MimeType, OriginalFileName, FileSizeBytes |
| `ChatMessageDto.cs` | Chat message: Id, Role, Content, Intent, ClientMessageId, CreatedAt, Attachments, StructuredPayload |
| `ChatRecommendationItemDto.cs` | Product recommendation from stylist: ProductId, Name, Slug, ImageUrl, Price, SalePrice, MatchReason |
| `ChatStructuredPayloadDto.cs` | Structured response: Recommendations, TryOnResult, ReasoningText |
| `ChatThreadDetailDto.cs` | Full thread: Id, Status, CreatedAt, Messages (list), Memory state |
| `ChatThreadSummaryDto.cs` | Thread listing item: Id, LastMessagePreview, MessageCount, CreatedAt, UpdatedAt |
| `IncomingChatAttachmentDto.cs` | Upload from client: Kind, FileName, ContentType, byte[] Content |
| `IntentClassificationDto.cs` | Classification result: Intent, Scenario, BudgetCeiling, ColorFamily, MaterialKeyword, ProductIds, RequiresPersonImage |
| `PagedResult.cs` | Generic paginated container: Items (IReadOnlyList<T>), TotalCount, Page, PageSize |
| `ProductDetailDto.cs` | Full product detail: all fields + Variants list + Images list |
| `ProductImageDto.cs` | Product image: ImageUrl, AltText, SortOrder, IsPrimary |
| `ProductListItemDto.cs` | Product listing item: Id, Name, Slug, ProductType, Status, price info, CategorySlug, stock, primary image |
| `ProductVariantDto.cs` | Variant: Id, Sku, VariantName, Size, Color, Price, SalePrice, StockQty, IsDefault, Status |
| `ThreadMemoryStateDto.cs` | Chat memory: Scenario, BudgetCeiling, ColorFamily, MaterialKeyword, ShortlistedProductIds, SelectedGarmentProductId, SelectedAccessoryProductIds, LatestPersonAttachmentId, PendingTryOnRequirements |

## Auth/ Subdirectory
| File | Description |
|------|-------------|
| `AuthResult.cs` | Auth result: Succeeded, Value (AccessToken + RefreshToken + User), ErrorCode, ErrorMessage |
| `AuthSessionDto.cs` | Session data: Token, ExpiresAt |
| `AuthUserDto.cs` | Authenticated user: Id, FullName, Email, Phone, Roles |
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
- DTOs are the serialization boundary -- they map to/from entity objects in services
- Result types (AuthResult, etc.) follow a standard pattern: `Succeeded`, `Value`, `ErrorCode`, `ErrorMessage`
- PagedResult<T> is used with PaginatedApiResponse<T> in the API layer
- ThreadMemoryStateDto is the in-memory representation of ChatThreadMemory entity
- IntentClassificationDto drives the chat intent routing in StylistChatService
