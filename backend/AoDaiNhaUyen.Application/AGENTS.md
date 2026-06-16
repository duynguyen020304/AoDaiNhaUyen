<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-04-19 | Updated: 2026-07-14 -->

# AoDaiNhaUyen.Application

## Purpose
Application contracts and data shapes. Defines DTOs, service/repository interfaces, option classes, exceptions, and small application services. Infrastructure implements most service interfaces.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `DTOs/` | Request/response records for API payloads |
| `Interfaces/` | Service + repository contracts |
| `Options/` | Validated config option classes |
| `Exceptions/` | Domain/app-specific exception types |
| `Services/` | Application-layer services such as catalog/blog/comment cache-facing logic |

## DTO Map
| Area | Directory/files |
|------|-----------------|
| Catalog/AI/chat | root DTOs: product/category/chat/try-on/memory/paging |
| Admin | `DTOs/Admin/` for products, promos, users, Hermes, AI, media, inventory |
| Auth | `DTOs/Auth/` for sessions, users, OAuth info, token validation |
| Blog | `DTOs/BlogPost/` for blog list/detail/create/update/block content |
| Cart/checkout/order | `DTOs/Cart/`, `DTOs/Checkout/`, `DTOs/Order/` |
| Dashboard | `DTOs/Dashboard/` for admin charts/stats |
| Marketing | `DTOs/Marketing/` for templates, subscribers, jobs, metrics |
| Promo | `DTOs/Promo/` for validation and discount results |
| User | `DTOs/User/` for profile/address/order views |

## Interfaces Map
| Area | Interfaces |
|------|------------|
| Repositories | Category, Product, Cart, UserProfile, BlogCategory, BlogPost, Comment |
| Core services | Auth, user, cart, checkout, catalog, blog, comments, reviews, promo |
| AI services | Try-on, stylist chat, image validation, admin agent, audit/risk |
| Admin services | Product/category/order/user/role/media/dashboard/inventory/promo/marketing |
| Infra services | Token/hash/email/storage/cache/events/subscriber/consent |

## Options
| Option | Purpose |
|--------|---------|
| `JwtSettings`, `CookieSettings` | Auth token/cookie behavior |
| `EmailSettings` | SMTP/MailKit |
| `GoogleOAuthSettings`, `FacebookOAuthSettings`, `ZaloOAuthSettings` | Social auth providers |
| `S3StorageSettings` | S3-compatible media storage |
| `HermesSettings` | Admin agent/control-plane config |
| `FusionCacheSettings` | Cache durations/L2/Redis-related settings |

## Local Conventions
- DTOs are usually `sealed record`; keep request/response shapes explicit.
- Shared contracts go here before Infrastructure impl.
- Options use data annotations and are validated on startup in Api registration.
- Result types commonly include `Succeeded`, `Value`, `ErrorCode`, `ErrorMessage`.
- Do not reference Infrastructure from this project.
