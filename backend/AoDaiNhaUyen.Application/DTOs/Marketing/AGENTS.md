<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-19 | Updated: 2026-06-19 -->

# DTOs/Marketing

## Purpose
Marketing subsystem DTOs covering email subscription management, customer behavioral event tracking, and promo campaign performance metrics. Consumed by `ISubscriberService`, `ICustomerEventService`, and admin marketing service interfaces.

## Key Files
| File | Description |
|------|-------------|
| `MarketingDtos.cs` | `SubscribeRequest` (email + source), `TokenRequest` (unsubscribe/confirm token), `SubscribeResultDto` (email, status, message) |
| `CustomerEventDtos.cs` | `TrackCustomerEventRequest` — EventType, AnonymousSessionId, optional entity IDs (Product, Order, Promo, Campaign), UTM fields (Source, Medium, Campaign), MetadataJson, OccurredAt; `TrackCustomerEventResultDto` — Id, EventType, OccurredAt |
| `PromoPerformanceDto.cs` | Promo campaign performance: PromoCodeId, Code, UsageCount, TotalDiscount, ConversionRate |

## For AI Agents

### Working In This Directory
- `TrackCustomerEventRequest.EventType` is a free-form string (e.g., `"product_view"`, `"add_to_cart"`) — check existing event types before adding new ones
- UTM fields (`Source`, `Medium`, `Campaign`) follow standard analytics attribution convention
- `AnonymousSessionId` tracks pre-login visitor behavior for attribution — nullable when user is authenticated
- `SubscribeRequest.Source` captures where the signup originated (footer, popup, checkout, etc.)

## Dependencies
### Internal
- `ISubscriberService` — consumes `SubscribeRequest`, `TokenRequest`; returns `SubscribeResultDto`
- `ICustomerEventService` — consumes `TrackCustomerEventRequest`; returns `TrackCustomerEventResultDto`
- Admin marketing interfaces consume `PromoPerformanceDto`

<!-- MANUAL: -->
