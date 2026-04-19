<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-04-19 -->

# AoDaiNhaUyen.Application

## Purpose
Application layer containing DTOs, service interfaces, service implementations, custom exceptions, and configuration options. Defines the business contract between the API and Infrastructure layers.

## Key Files
| File | Description |
|------|-------------|
| `AoDaiNhaUyen.Application.csproj` | Project file -- references Domain layer |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `DTOs/` | Data transfer objects for all API request/response payloads (see `DTOs/AGENTS.md`) |
| `Exceptions/` | Custom exception types for domain-specific error scenarios |
| `Interfaces/` | Repository and service interfaces (see `Interfaces/AGENTS.md`) |
| `Options/` | Configuration option classes bound via .NET Options pattern |
| `Services/` | Application service implementations (currently CatalogService) |

## DTOs
### Root DTOs
| File | Description |
|------|-------------|
| `AiTryOnAccessoryImageDto.cs` | DTO for accessory images in AI try-on requests |
| `AiTryOnCatalogDto.cs` | Catalog listing for AI try-on products |
| `AiTryOnCatalogItemDto.cs` | Individual item in the try-on catalog |
| `AiTryOnRequestDto.cs` | Original AI try-on request |
| `AiTryOnResultDto.cs` | AI try-on result with generated image |
| `CatalogAiTryOnRequestDto.cs` | Catalog-based try-on request (product ID + person image) |
| `CategoryDto.cs` | Flat category DTO |
| `CategoryTreeDto.cs` | Hierarchical category tree DTO with children |
| `ChatAttachmentDto.cs` | Chat attachment DTO |
| `ChatMessageDto.cs` | Chat message DTO |
| `ChatRecommendationItemDto.cs` | Product recommendation item from stylist chat |
| `ChatStructuredPayloadDto.cs` | Structured response payload for chat messages |
| `ChatThreadDetailDto.cs` | Full thread detail with messages |
| `ChatThreadSummaryDto.cs` | Thread listing summary |
| `IncomingChatAttachmentDto.cs` | Incoming attachment from client upload |
| `IntentClassificationDto.cs` | Result of intent classification (intent, scenario, color, budget, etc.) |
| `PagedResult.cs` | Generic paginated result container |
| `ProductDetailDto.cs` | Full product detail with variants and images |
| `ProductImageDto.cs` | Product image DTO |
| `ProductListItemDto.cs` | Product listing item with price, image, stock |
| `ProductVariantDto.cs` | Product variant DTO |
| `ThreadMemoryStateDto.cs` | Chat thread memory state (scenario, budget, color, shortlisted products) |

### Auth/ DTOs
| File | Description |
|------|-------------|
| `AuthResult.cs` | Authentication result with access/refresh tokens |
| `AuthSessionDto.cs` | Session data DTO |
| `AuthUserDto.cs` | Authenticated user DTO |
| `FacebookUserInfoDto.cs` | Facebook OAuth user info |
| `GoogleUserInfoDto.cs` | Google OAuth user info |
| `TokenValidationResult.cs` | Email/password token validation result types |

### Cart/ DTOs
| File | Description |
|------|-------------|
| `AddCartItemDto.cs` | Add item to cart request |
| `CartDto.cs` | Cart with items DTO |
| `CartItemDto.cs` | Individual cart item DTO |
| `UpdateCartItemDto.cs` | Update cart item quantity request |

### Checkout/ DTOs
| File | Description |
|------|-------------|
| `CheckoutAddressDto.cs` | Shipping address for checkout |
| `CheckoutRequestDto.cs` | Checkout request with address and items |
| `CheckoutResultDto.cs` | Checkout result with order code |

### User/ DTOs
| File | Description |
|------|-------------|
| `CreateAddressDto.cs` | Create user address request |
| `OrderItemDto.cs` | Order item DTO |
| `UserAddressDto.cs` | User address DTO |
| `UserOrderDto.cs` | User order with items DTO |
| `UserProfileDto.cs` | User profile read/update DTO |

## Exceptions
| File | Description |
|------|-------------|
| `AiTryOnConfigurationException.cs` | Thrown when Vertex AI is not configured (maps to 503) |
| `AiTryOnProviderException.cs` | Thrown when Vertex AI call fails (maps to 502) |
| `GoogleOAuthExchangeException.cs` | Thrown when Google OAuth code exchange fails |
| `FacebookOAuthExchangeException.cs` | Thrown when Facebook OAuth code exchange fails |

## Options
| File | Description |
|------|-------------|
| `CookieSettings.cs` | Cookie names for access_token and refresh_token |
| `EmailSettings.cs` | SMTP configuration with URI validation |
| `FacebookOAuthSettings.cs` | Facebook AppId, AppSecret, RedirectUri |
| `GoogleOAuthSettings.cs` | Google ClientId, ClientSecret, RedirectUri |
| `JwtSettings.cs` | JWT SecretKey, Issuer, Audience |

## Services
| File | Description |
|------|-------------|
| `CatalogService.cs` | Implements ICatalogService -- maps categories and products to DTOs, handles pagination logic |

## For AI Agents
### Working In This Directory
- This project defines the **contracts** (interfaces) and **data shapes** (DTOs) for the entire application
- DTOs are immutable records -- use `sealed record` for new DTOs
- All interfaces are in `Interfaces/` with `I` prefix convention
- Options classes use data annotations for validation (Required, URL validation)
- When adding a new feature: define DTOs here, add interface in `Interfaces/Services/`, implement in Infrastructure
- CatalogService is the only service implemented in Application layer -- all others are in Infrastructure
- Exception types are caught specifically in controllers and mapped to appropriate HTTP status codes
