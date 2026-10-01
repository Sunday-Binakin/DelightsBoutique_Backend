# Delight's Boutique — .NET 8 Backend: Build Specification

> **Purpose of this document.** This is the single, self-contained brief for building the
> Delight's Boutique backend in ASP.NET Core. It consolidates four source documents — the
> Backend Architecture & Design Document, the current API Contract (which describes the
> *existing* Next.js/localStorage-only app being replaced), and two versions of the Website
> Requirements doc — into one spec, with every conflict between them called out explicitly
> rather than silently resolved. Use this as the prompt/brief for whoever (human or agent)
> implements the backend: it says what to build, in what order, and which open questions
> need an answer from the business owner before — or during — that work.

---

## 0. What exists today, and what this project replaces

There is **no backend yet**. The current site is a Next.js app where every "data store"
(`src/lib/product-store.ts`, `orders.ts`, `customers.ts`, `staff.ts`, `admin-auth.ts`,
`notifications.ts`, `newsletter.ts`, `reports.ts`, `audit-log.ts`, `banners.ts`,
`flash-sale.ts`, `collection-section.ts`, `storefront-copy.ts`, `pages-content.ts`,
`site-settings.ts`, `owner-profile.ts`, `image-upload.ts`) is backed by `localStorage` in
the visitor's own browser. That means: no shared data between visitors, no real
authentication (today's admin-auth accepts any password), and client-submitted prices are
trusted at face value.

This project builds the real backend + database that those modules get replaced with. The
frontend's current field shapes and behavior are the de-facto spec for what must keep
working; this document is the target once the backend exists, and it deliberately tightens
a few things the client-only version got away with (price trust, auth, image storage,
audit integrity).

---

## 1. Engineering principles (non-negotiable, drive every decision below)

| Principle | What it means here |
|---|---|
| **KISS** | No pattern/framework adopted unless this project's actual complexity needs it. Direct EF Core, not Repository+UoW. One deployable API, no message broker. |
| **DRY** | Shared logic (pricing, phone normalization, status transitions) lives in exactly one place — never copy-pasted across controllers. |
| **SoC** | Each layer has one job. A controller never touches the database directly; a domain entity never knows about HTTP. |
| **SOLID** | Single responsibility per handler; new payment method = new strategy, not an edited if-chain; narrow interfaces (`IOrderPricingService`, not `IEverythingService`); layers depend on abstractions wired in `Program.cs`. |
| **YAGNI** | Features marked future work (online payments, real inventory counts, customer accounts) are not pre-built as unused scaffolding — but the architecture must make adding them easy later. |
| **Fail Fast / Defensive by default** | Invalid input rejected at the edge; business rules throw typed domain exceptions rather than silently doing the wrong thing. |
| **Least Privilege** | Staff can only do what their permissions allow, enforced server-side, never trusted from the client. |
| **Single Source of Truth** | Price computed once, server-side, from the product record. Status-transition rules live in one place. |
| **Idempotency** | Any retryable operation (flaky connection resubmitting an order, a double-click) is safe to repeat without duplicate side effects. |

---

## 2. Architecture & tech stack

**Clean Architecture**, four layers, dependencies pointing inward:

| Layer | Contains | Depends on |
|---|---|---|
| **Domain** | Entities, enums, value objects, domain exceptions, domain-level interfaces. No framework references, not even EF Core. | Nothing |
| **Application** | Use-case handlers (one per operation), DTOs, validators, mapping profiles, interfaces the outside world provides (`IOrderRepository`, `IFileStorage`, `IClock`, `INotificationSender`). | Domain only |
| **Infrastructure** | EF Core `DbContext` + entity configs, repository implementations, file-storage client, WhatsApp/email senders, password hashing. | Application + Domain |
| **API (host)** | Controllers, middleware, `Program.cs` composition root, auth scheme, Swagger. | Application + Infrastructure (wiring only, in `Program.cs` — controllers never reference Infrastructure types) |

**Why this earns its keep:** the business explicitly plans to add a real payment gateway,
WhatsApp Business API automation, and (maybe) real inventory later. Clean Architecture
means those arrive as new Infrastructure implementations of existing Application
interfaces (swap `NoopPaymentGateway` for `PaystackGateway`) without touching Domain or
Application code.

### Tech stack

| Concern | Choice | Why |
|---|---|---|
| Runtime | .NET 8 (LTS) | Avoids a forced major-version jump mid-project |
| Web framework | ASP.NET Core Web API, controllers (not Minimal APIs) | ~50 endpoints across 10+ resource groups with shared conventions (pagination, error envelope, permission checks) — controllers + filters express that better than a flat Minimal API file |
| ORM | EF Core 8, code-first | Migrations double as schema change history |
| Database | PostgreSQL | Free, strong `jsonb` support (site-content singletons), identical in dev (Docker) and most hosts. SQL Server is a drop-in swap, isolated to Infrastructure. |
| Auth | ASP.NET Core cookie authentication (not Identity, not JWT) | Contract specifies an httpOnly session cookie, not bearer tokens |
| Password hashing | BCrypt.Net-Next | Simple, reviewed, no Identity schema baggage |
| Validation | FluentValidation | Keeps rules out of controllers/DTOs, testable in isolation |
| Mapping | Mapster | Lighter than AutoMapper; config lives next to the DTOs it maps |
| Use-case orchestration | MediatR (CQRS-lite) | Controllers reduce to "map request → command/query → map result" |
| Logging | Serilog, structured | Makes the audit trail and request correlation actually queryable |
| API docs | Swashbuckle (OpenAPI) | Doubles as a contract-conformance check against this document |
| Testing | xUnit, FluentAssertions, Testcontainers | Testcontainers avoids tests passing against a mock DB and failing against the real one |
| Image storage | Any S3-compatible client behind `IFileStorage` | Contract only requires an absolute HTTPS URL back; provider is a pure Infrastructure decision |
| Background/notifications | `IHostedService` (built-in) for v1 | A boutique's order volume doesn't need a distributed job queue on day one (YAGNI) |

---

## 3. Solution structure

```
DelightsBoutique.sln
+-- src/
|   +-- DelightsBoutique.Domain/
|   |   +-- Entities/        (Product, Category, Order, OrderItem, StaffMember, OwnerProfile, ...)
|   |   +-- Enums/            (OrderStatus, FulfillmentType, PaymentMethod, Permission, Tone, Badge)
|   |   +-- Exceptions/       (DomainException and subtypes)
|   |   +-- Interfaces/       (pure domain contracts, no I/O)
|   |
|   +-- DelightsBoutique.Application/
|   |   +-- Common/           (error envelope types, PagedResult<T>, IClock)
|   |   +-- Abstractions/     (IOrderRepository, IProductRepository, IFileStorage, INotificationSender, IPasswordHasher, ICurrentUser)
|   |   +-- Orders/, Products/, Categories/, Customers/, Staff/, Content/, Reports/, Account/, Notifications/, AuditLog/, Newsletter/, Auth/
|   |   |   +-- Commands/ Queries/ Dtos/ Validators/   (per feature)
|   |   +-- MappingProfiles/
|   |
|   +-- DelightsBoutique.Infrastructure/
|   |   +-- Persistence/      (AppDbContext, Configurations/, Repositories/, Migrations/)
|   |   +-- Auth/             (BcryptPasswordHasher, CookieCurrentUserAccessor, OtpService)
|   |   +-- Storage/          (S3FileStorage)
|   |   +-- Notifications/    (EmailNotificationSender, WhatsAppNotificationSender, CompositeNotificationSender, NoopNotificationSender)
|   |   +-- Idempotency/      (EfIdempotencyStore)
|   |
|   +-- DelightsBoutique.Api/
|       +-- Controllers/      (one per resource group)
|       +-- Middleware/       (ExceptionHandlingMiddleware, CorrelationIdMiddleware)
|       +-- Filters/          (RequirePermissionAttribute, ValidateModelFilter)
|       +-- Auth/             (cookie scheme setup, AuthorizationPolicies)
|       +-- Program.cs        (composition root)
|       +-- appsettings.{Environment}.json
|
+-- tests/
    +-- DelightsBoutique.UnitTests/        (Domain + Application, no I/O)
    +-- DelightsBoutique.IntegrationTests/ (WebApplicationFactory + Testcontainers Postgres)
```

**Enforced reference rules:** Domain → nothing. Application → Domain only. Infrastructure
→ Application + Domain. Api → Application + Infrastructure. Add a CI check so a stray
`using DelightsBoutique.Infrastructure` inside Application fails the build.

---

## 4. Domain model

Conventions: `id: string` (UUID) is the internal primary key everywhere; `slug` (products,
categories) or the opaque order id is the public-facing identifier. Money fields are
`decimal`, never `float`/`double`. Currency is **GHS** throughout; timestamps are ISO 8601
UTC.

### Product
```
id, slug (unique), name, categorySlug (FK), description,
retailPrice: decimal, wholesalePrice: decimal?, wholesaleMoq: int?,
compareAtPrice: decimal?, rating: decimal, reviewCount: int,
tone: enum(rose|cream|charcoal|gold), badge: enum(New|BestSeller)?,
stockStatus: enum(InStock|LowStock|OutOfStock),
sizes: string[]?, colors: string[]?, imageUrls: string[]?,
createdAt, updatedAt
```
`category` (display name) is denormalized onto the DTO from `categorySlug`, not stored
twice.

### Category
```
id, slug (unique), name, tone: enum, imageUrl?, createdAt, updatedAt
```
`EnsureDeletable()`: throws `CategoryInUseException` → 409 if any `Product` still
references this category's slug (tightens the current client, which allows orphaning).

### Order (aggregate root) / OrderItem (child, never fetched independently)
```
OrderItem {
  slug, name,            // product snapshot at order time — survives renames/deletes
  quantity: int,
  unitPrice: decimal,     // server-computed, never client-supplied
  size?, color?
}

Order {
  id,                     // e.g. "DB-7Q3K9X2A" — base36 timestamp + random suffix, opaque
  createdAt,
  status: enum(PendingConfirmation|Confirmed|Processing|OutForDelivery|ReadyForPickup|Completed|Cancelled),
  items: OrderItem[],
  subtotal: decimal,       // server-computed sum(items.quantity * items.unitPrice)
  customerName, phone,      // phone stored raw, normalized on every read/lookup
  fulfillment: enum(Delivery|Pickup),
  address?,                  // required if fulfillment = Delivery
  paymentMethod: enum(BankTransfer|MobileMoney|CashOnDeliveryOrPickup),
  paymentProofUrl?: string,   // ⚠ NEW FIELD — see §9, "Payment-proof upload" gap
  notes?,                      // customer-submitted, visible to admin
  internalNotes?,               // admin-only — no DTO property exposes this publicly
  rowVersion                     // optimistic concurrency token
}
```
`Order.TransitionTo(OrderStatus target)` is a domain method (not a public setter) that
encodes the legal transition table once (see §9) and throws
`InvalidStatusTransitionException` on an illegal move — including the fulfillment-specific
branch (`OutForDelivery` only valid for `Delivery` orders, `ReadyForPickup` only for
`Pickup`).

### CustomerFlag (not a full account/login — see §11 open decision)
Keyed by normalized phone; holds only `wholesale: boolean`. Everything else
(`orderCount`, `totalSpent`, `lastOrderAt`, `name`) is **computed** from `orders` at query
time, not stored — one source of truth, nothing to drift. `totalSpent` sums `subtotal`
only for orders where `status != Cancelled` (resolves the contract's flagged open question
— same predicate used by `SalesSummary.confirmedRevenue`).

### StaffMember
```
id, name, email (unique, case-insensitive), phone?,
permissions: enum[](ManageProducts|ManageOrders|ManageCustomers|ManageContent),
active: bool, passwordHash (never returned by any endpoint),
createdAt, updatedAt
```
No delete endpoint — only `active: false` (preserves attribution on past audit entries).

### OwnerProfile
Singleton row: `name, email, phone?, passwordHash`.

### OrderNotification
`id, orderId, customerName, subtotal, createdAt, read: bool` — auto-created server-side on
every order creation, never client-writable except the `read` flag.

### NewsletterSubscriber
`email (unique), subscribedAt`.

### AuditEntry
`id, timestamp, actor, action` — append-only, written server-side by a cross-cutting
pipeline behavior on every admin mutation, never client-writable directly.

### Content singletons → one `SiteContent` entity
`{ key: enum, value: jsonb }`, one table, not seven near-empty ones — matches the
contract's own suggestion. Keys: `site_settings`, `banners`, `flash_sale`,
`collection_section`, `storefront_copy`, `pages_content`.

- **SiteSettings** — `tagline, whatsappNumber, whatsappMessage, phoneDisplay, address,
  hours, announcement, bankTransferInstructions, mobileMoneyInstructions,
  cashInstructions, facebookUrl, instagramUrl, tiktokUrl, notifyByEmail,
  notificationEmail, notifyByWhatsapp`. The last three are **admin-only** — the public
  DTO (`PublicSiteSettingsDto`) has no properties for them at all, so there is no code
  path that can leak them.
- **BannerSet** — `heroMain, heroSide1, heroSide2, fullWidth`, each a
  `{ eyebrow, title, ctaLabel, href, tone, artCategorySlug, imageUrl?, textColor? }`.
- **FlashSaleContent** — `{ eyebrow, title, endsAt, productSlugs[] }`; the read endpoint
  resolves slugs to full `Product[]` server-side.
- **CollectionSectionContent** — `{ eyebrow, title, newArrivalsLabel, bestSellersLabel,
  wholesalePicksLabel, newArrivalsSlugs[], bestSellersSlugs[], wholesalePicksSlugs[] }`,
  same slug-resolution on read.
- **StorefrontCopy** — trust badges, contact/wholesale/newsletter/cart/checkout copy
  blocks (opaque JSON; shape defined in the existing frontend's `storefront-copy.ts`).
- **PagesContent** — `about, shipping, returns, terms, privacy` (each `{ title, body }`)
  plus `faqs: FaqItem[]` (`{ id, question, answer }`).

### Deliberately excluded from the schema
**Cart and wishlist** — stay client-side (`localStorage`), unchanged. Checkout is a
single-shot submit, not a persistent account cart; revisit only if real customer accounts
are added later.

---

## 5. Database schema notes

| Table | Notes |
|---|---|
| `products`, `categories` | `slug` unique indexed; `id` UUID primary key |
| `orders`, `order_items` | `order_items.order_id` FK, cascade delete; `orders.customer_phone` indexed (normalized form) |
| `customer_flags` | Keyed by normalized phone; only the wholesale boolean — everything else computed |
| `staff` | `email` unique indexed, case-insensitive; `password_hash`, never `password` |
| `owner_profile` | Single row, enforced by check constraint / seed migration |
| `order_notifications` | `order_id` FK; `read` indexed for unread-count query |
| `newsletter_subscribers` | `email` unique indexed |
| `audit_log` | Append-only; indexed on `timestamp desc` |
| `site_content` | One row per key, `jsonb` value column |
| `idempotency_keys` | `(key, endpoint)` indexed; `created_at` indexed for cleanup |

**Migrations:** `dotnet ef migrations add <Name>` committed with the code change that
caused it. `dotnet ef database update` runs automatically on startup in Development only;
an explicit CI/CD step in Staging/Production — never silent auto-migration against prod.

---

## 6. Complete API surface

Base URL `/api`. JSON everywhere except uploads (`multipart/form-data`). Pagination
envelope on every list endpoint: `{ data, page, limit, total }` (`page` 1-based, default 1;
`limit` default 20, max 100). Error envelope, always with a conventional HTTP status:

```json
{ "error": { "code": "VALIDATION_ERROR", "message": "phone is required", "fields": { "phone": "required" } } }
```
Codes: `VALIDATION_ERROR`(400) · `UNAUTHENTICATED`(401) · `FORBIDDEN`(403) ·
`NOT_FOUND`(404) · `CONFLICT`(409) · `RATE_LIMITED`(429) · `INTERNAL_ERROR`(500).

### 6.1 Auth — ⚠ extends the original contract with OTP (see §11 decisions)

| Method | Path | Auth | Notes |
|---|---|---|---|
| POST | `/api/auth/login` | none | `{ email, password }`. Verifies the BCrypt hash. On success: **does not** sign in yet — returns `200 { loginToken }` and sends a 6-digit OTP email synchronously. `401` on bad credentials, `403 { code: "STAFF_DEACTIVATED" }` if inactive. |
| POST | `/api/auth/verify-otp` | none | `{ loginToken, code }`. Correct + unexpired code → `HttpContext.SignInAsync`, sets session cookie, `200 { role, staffId?, email, name }`. Otherwise `401`. |
| POST | `/api/auth/resend-otp` | none | `{ loginToken }`. Rate-limited. Issues a fresh code against the same token. |
| POST | `/api/auth/logout` | session | `204`, clears cookie. |
| GET | `/api/auth/session` | session | `200` current session info, or `401`. |

CSRF: custom header (`X-Requested-With`) required on all mutating requests, checked by
middleware.

### 6.2 Storefront catalog (public, read-only)

| Method | Path | Notes |
|---|---|---|
| GET | `/api/products` | Query: `category` (slug), `search`, `badge`, `sort` (`newest\|price-asc\|price-desc\|rating`), `page`, `limit`. |
| GET | `/api/products/:slug` | `404` if missing. |
| GET | `/api/categories` | Full list, not paginated. |
| GET | `/api/categories/:slug` | `404` if missing. |

### 6.3 Orders & checkout (public)

| Method | Path | Notes |
|---|---|---|
| POST | `/api/orders` | **Requires `Idempotency-Key` header.** Body: `{ items: [{ slug, quantity, size?, color? }], customerName, phone, fulfillment, address?, paymentMethod, notes? }` — no `unitPrice` field exists on this DTO; it is structurally impossible for the client to supply one. Server resolves pricing and stock (§9), `201` with the full `Order` (minus `internalNotes`). `400` on out-of-stock line, qty ≤ 0, missing address for Delivery, or unresolvable slug. Rate-limited by IP. |
| GET | `/api/orders/track?phone=` | Normalizes phone, returns all matching orders newest-first, `internalNotes` stripped. `200 { data: [] }` on no match — never errors, to avoid confirming/denying a phone number ordered. Rate-limited by IP. |
| GET | `/api/orders/:id` | Used right after checkout. Public by design (opaque id = access token), `internalNotes` stripped. `404` if not found. |
| POST | `/api/orders/:id/payment-proof` | ⚠ **New endpoint — see §9 gap.** `multipart/form-data`, field `file`. Only valid while order is `PendingConfirmation`. Stores via `IFileStorage`, sets `Order.paymentProofUrl`. Rate-limited, `413` over 5MB. |

### 6.4 Newsletter (public)

| Method | Path | Notes |
|---|---|---|
| POST | `/api/newsletter/subscribe` | `{ email }`. `201` new subscriber, `200 { duplicate: true }` if already subscribed — not an error. |

### 6.5 Public content (read-only)

| Method | Path | Returns |
|---|---|---|
| GET | `/api/content/site-settings` | Public-safe subset only — excludes `notifyByEmail`, `notificationEmail`, `notifyByWhatsapp`. |
| GET | `/api/content/banners` | `BannerSet` |
| GET | `/api/content/flash-sale` | `FlashSaleContent`, `productSlugs` resolved to full `Product[]` |
| GET | `/api/content/collection-section` | `CollectionSectionContent`, slugs resolved per tab |
| GET | `/api/content/storefront-copy` | `StorefrontCopy` |
| GET | `/api/content/pages/:key` | `key` ∈ `about\|shipping\|returns\|terms\|privacy`. `404` on unknown key. |
| GET | `/api/content/faqs` | `FaqItem[]` |

### 6.6 Admin: Products & Categories — `Manage Products`

| Method | Path | Notes |
|---|---|---|
| POST | `/api/admin/products` | Server generates `slug` from `name` if omitted; `409` on collision. |
| PATCH | `/api/admin/products/:slug` | Partial update. |
| DELETE | `/api/admin/products/:slug` | Hard delete — safe even if referenced, since order lines snapshot `name`/`unitPrice`. |
| POST | `/api/admin/categories` | Create. |
| PATCH | `/api/admin/categories/:slug` | Partial update. |
| DELETE | `/api/admin/categories/:slug` | `409` if any product still references it (`CategoryInUseException`). |
| POST | `/api/admin/products/bulk-upload` | ⚠ CSV/Excel bulk create — required by FR-26, not in the original API contract; define a row schema matching the Product fields above, validate per-row, return a per-row success/error summary rather than all-or-nothing. |

### 6.7 Admin: Orders — `Manage Orders`

| Method | Path | Notes |
|---|---|---|
| GET | `/api/admin/orders` | Query: `status`, `fulfillment`, `paymentMethod`, `categorySlug`, `from`, `to`, `search` (name/phone), `page`, `limit`. Includes `internalNotes`. |
| GET | `/api/admin/orders/:id` | Full order incl. `internalNotes`. |
| PATCH | `/api/admin/orders/:id` | `{ status?, internalNotes? }`. Validates legal transition for the order's fulfillment. `409` on stale `rowVersion`. Writes `AuditEntry`. |

### 6.8 Admin: Customers — `Manage Customers`

| Method | Path | Notes |
|---|---|---|
| GET | `/api/admin/customers` | Aggregated, paginated, sorted by `lastOrderAt` desc. Query: `search`, `wholesaleOnly`. |
| GET | `/api/admin/customers/:phone/orders` | All orders for that normalized phone. |
| PATCH | `/api/admin/customers/:phone` | `{ wholesale: boolean }`. Writes `AuditEntry`. |

### 6.9 Admin: Staff — Owner only

| Method | Path | Notes |
|---|---|---|
| GET | `/api/admin/staff` | Never includes password/hash. |
| POST | `/api/admin/staff` | **Requires `Idempotency-Key` header** (prevents a double-submit creating two accounts). `{ name, email, phone?, permissions }`. Generates + hashes a one-time password, returns it once (`{ member, password }`), **and** emails it to the new staff member automatically (per FR-43/US-17 — the original contract only did the response; this build adds the email send from the same handler). |
| PATCH | `/api/admin/staff/:id` | Update `name, email, phone, permissions, active`. |
| POST | `/api/admin/staff/:id/reset-password` | New password, hashed, returned once: `{ password }`. |

### 6.10 Admin: Notifications — `Manage Orders`

| Method | Path | Notes |
|---|---|---|
| GET | `/api/admin/notifications` | Newest first, capped (e.g. last 50). |
| GET | `/api/admin/notifications/unread-count` | `{ count }` — sidebar bell. |
| PATCH | `/api/admin/notifications/:id/read` | Marks one read. |
| POST | `/api/admin/notifications/mark-all-read` | Marks all read. |

### 6.11 Admin: Content — `Manage Content`

| Method | Path | Notes |
|---|---|---|
| PATCH | `/api/admin/content/site-settings` | Partial `SiteSettings`. |
| PUT | `/api/admin/content/banners/:key` | `key` ∈ `heroMain\|heroSide1\|heroSide2\|fullWidth`. |
| PATCH | `/api/admin/content/flash-sale` | Partial. |
| PATCH | `/api/admin/content/collection-section` | Partial. |
| PATCH | `/api/admin/content/storefront-copy` | Partial. |
| PUT | `/api/admin/content/pages/:key` | `key` ∈ `about\|shipping\|returns\|terms\|privacy`. |
| POST | `/api/admin/content/faqs` | `{ question, answer }` → server assigns `id`. |
| PATCH | `/api/admin/content/faqs/:id` | Edit one. |
| DELETE | `/api/admin/content/faqs/:id` | Remove one. |

Every write here logs an `AuditEntry`.

### 6.12 Admin: Reports — see §11 open decision on permission

| Method | Path | Notes |
|---|---|---|
| GET | `/api/admin/reports` | Query: `from, to, fulfillment, paymentMethod, categorySlug` (all optional). One composite payload: `{ summary, topProducts, statusBreakdown, paymentBreakdown }`. `summary.confirmedRevenue` and `topProducts` exclude `Cancelled` orders (same predicate as `CustomerFlag.totalSpent`, §4). |

### 6.13 Admin: Account (self-service, any authenticated admin)

| Method | Path | Notes |
|---|---|---|
| GET | `/api/admin/account/profile` | Owner → `OwnerProfile` minus password; staff → their own `StaffMember` minus password. |
| PATCH | `/api/admin/account/profile` | Update `name, email, phone`. Re-syncs session's cached name/email. |
| POST | `/api/admin/account/change-password` | `{ currentPassword, newPassword }`. **Verifies `currentPassword` against the stored hash first** — new vs. the placeholder, which never checked it. |

### 6.14 Admin: Audit log — Owner only

| Method | Path | Notes |
|---|---|---|
| GET | `/api/admin/audit-log` | Paginated, newest first. Query: `search` (actor/action text). |

### 6.15 Uploads

| Method | Path | Notes |
|---|---|---|
| POST | `/api/admin/uploads` | `multipart/form-data`, field `file`. Validates image type/size, resizes server-side (max 1000px long edge, JPEG q≈0.82) via `SixLabors.ImageSharp`, stores via `IFileStorage`, returns `{ url }` (absolute HTTPS, never a data URL). Permission matches the calling form (`Manage Products` or `Manage Content`). `413` over 5MB. |

### 6.16 Health

| Method | Path | Notes |
|---|---|---|
| GET | `/api/health` | `200 { ok: true }`, no auth — uptime checks / deploy smoke tests. |

---

## 7. Authorization matrix

Permission-based, not role-based — Staff is four independent capabilities, not one role.
Owner always passes (superset). Enforced as an `IAuthorizationRequirement` +
`AuthorizationHandler`, not scattered role-checks.

| Area | Required |
|---|---|
| `/api/admin/staff/**`, `/api/admin/audit-log` | Owner only |
| `/api/admin/products/**`, `/api/admin/categories/**` | Manage Products |
| `/api/admin/orders/**`, `/api/admin/notifications/**` | Manage Orders |
| `/api/admin/customers/**` | Manage Customers |
| `/api/admin/content/**` | Manage Content |
| `/api/admin/reports/**` | A dedicated **View Reports** permission — see §11, this is a resolved gap, not optional |
| `/api/admin/account/**` | Any authenticated admin, scoped to self |
| `/api/admin/uploads` | Whichever permission owns the calling form |

**Cookie behavior, deliberately diverging from a literal "session store" reading of the
original contract:** ASP.NET Core's cookie auth handler encrypts claims directly into the
cookie — no server-side session table. This means deactivating a staff member doesn't by
itself invalidate an already-issued cookie. Close that gap by having the authorization
handler re-check `StaffMember.active` against the database on **every request**, not just
the cached claim — cheaper than a session table, same practical guarantee (a deactivated
staff member is locked out immediately).

---

## 8. Idempotency & concurrency

**Required** on: `POST /api/orders`, `POST /api/admin/staff` — both create a real-world
side effect (an order, a password) that must not duplicate on retry.

Pattern: client sends `Idempotency-Key` header (UUID, generated once per logical submit
attempt, reused across retries of that same submit). Server stores
`(key, endpoint, requestHash, responseStatus, responseBody, createdAt)` in
`idempotency_keys`. Same key + same body → return the stored response verbatim without
re-running the handler. Same key + different body → `409 CONFLICT`. New key → handler runs
inside a transaction that writes the business result and the idempotency record
atomically. Keys expire after 24–48h (indexed `created_at` + periodic cleanup).
Implemented as a MediatR pipeline behavior over commands marked `IIdempotentCommand` — not
per-handler code.

**Everything else that's a `PATCH`** (order status, content, staff permissions) is
naturally idempotent by HTTP semantics and is instead protected by **optimistic
concurrency**: every mutable entity carries a `RowVersion` token; a `PATCH` against stale
data returns `409 CONFLICT`, not a silent overwrite.

---

## 9. Server-side business rules

| Rule | Where it lives | How |
|---|---|---|
| Never trust client-submitted prices | `IOrderPricingService` | `CreateOrderHandler` receives only `slug + quantity (+ size/color)` per line; looks up the current `Product`, picks `wholesalePrice` if the order's phone is flagged wholesale **and** `quantity >= wholesaleMoq`, else `retailPrice`; computes `subtotal` itself. The request DTO has no `unitPrice` field at all. |
| Stock enforcement | `Product.EnsureAvailable()` | Rejects (`400`) any line where `stockStatus == OutOfStock`. **This is a presence check, not a quantity decrement** — see §11 conflict with NFR-10. |
| Phone normalization | `IPhoneNormalizer`, one implementation, injected everywhere a phone is a lookup key | `+233…`, `233…`, `0…` always resolve to the same customer. |
| Order status transitions respect fulfillment | `Order.TransitionTo(OrderStatus)` | Legal path: `PendingConfirmation → Confirmed → Processing → (OutForDelivery if Delivery \| ReadyForPickup if Pickup) → Completed`; `Cancelled` reachable from any non-terminal status. Mismatched fulfillment/status throws `InvalidStatusTransitionException` → 400 — confirm the exact allowed-cancellation points with the owner before coding the transition table verbatim. |
| Order IDs short, shareable, not guessable | `IOrderIdGenerator` | Base36 timestamp + random suffix, generated once, immutable. |
| Every admin mutation writes an audit entry | `AuditLoggingBehavior` (MediatR pipeline) | Wraps every command implementing `IAuditableCommand`; writes `(actor, action, timestamp)` in the same transaction as the write — structurally impossible to forget, unlike today's scattered client-side `logAction()` calls. |
| Audit log & reports never reachable unauthenticated | Framework `[Authorize]` + permission attributes | Closes the gap where today it's only enforced by "not linking to the page." |
| Category can't be deleted while in use | `Category.EnsureDeletable()` | `409 CONFLICT`, forces reassignment first — tightens today's behavior, which allows orphaning products. |
| Flash-sale / collection-section slugs resolve server-side | `GetFlashSaleQuery`, `GetCollectionSectionQuery` | Stored content holds only slugs; the query resolves to full `ProductDto[]` so the storefront never needs a second round trip. |
| Customer lifetime spend excludes cancelled orders | Customer aggregation query | Same predicate as `SalesSummary.confirmedRevenue` — one shared definition of "spent," not two that can drift. |

**Payment-proof upload gap:** FR-19 requires letting the customer upload a payment
screenshot on the order page, and the admin order-detail view is required to show "proof
of payment (if uploaded)" — but neither the original API contract's `Order` model nor its
endpoint list has a field or route for this. §4 and §6.3 above add
`Order.paymentProofUrl` and `POST /api/orders/:id/payment-proof` to close that gap; confirm
this is an acceptable implementation before building it.

---

## 10. Cross-cutting concerns

**Security.** Rate limiting (`Microsoft.AspNetCore.RateLimiting`) on `POST /api/orders`,
`GET /api/orders/track`, `POST /api/auth/login`, `POST /api/auth/resend-otp`. Secrets from
environment variables / a secrets manager in Staging/Production, never committed. HTTPS
everywhere; `Secure` cookie flag. Parameterized queries only (EF Core default) — no raw SQL
string concatenation anywhere.

**File & image uploads.** `IFileStorage` behind one Infrastructure implementation per
provider. Validate type/size (reject >5MB), resize server-side (max 1000px long edge, JPEG
q≈0.82) via `SixLabors.ImageSharp`, return an absolute HTTPS URL.

**Notifications.** `INotificationSender` → `EmailNotificationSender` +
`WhatsAppNotificationSender`, fanned out by `CompositeNotificationSender` so a new order
always dispatches to both channels together (adding/removing a channel later is a change
to the composite's membership, not every call site). `NoopNotificationSender` is a
Development fallback for WhatsApp only — the email channel always uses a real sandbox-tier
provider (e.g. Mailtrap) even in Development, since OTP login depends on it working.
Delivery runs via an `IHostedService`-backed in-memory queue so a slow notification
provider never blocks the customer's `POST /api/orders` response — **except OTP emails**,
which are sent synchronously (the user is actively waiting on-screen for the code).

New-staff-account emails and new-order admin alerts (email + WhatsApp + on-screen badge,
per FR-21/22/42, US-11/19) all fan out from the same `CompositeNotificationSender`.

**Logging & observability.** Serilog structured logging; every request gets a correlation
id (middleware-assigned, echoed in the response header). `GET /api/health` checks DB
connectivity. Audit log (business "who changed what") and Serilog (technical "what
happened, down to the request") are deliberately two separate mechanisms.

---

## 11. Where this spec overrides or extends the original API Contract

The Backend Architecture & Design Document is the authoritative source for this build.
Where it makes a decision the original API Contract doesn't cover — or explicitly
overrides — that decision is final, not an open question. The list below exists so it's
clear *why* this spec differs from the contract document, not to invite re-litigating any
of it. Only the last two items are genuinely new ground neither source document staked out;
those are this spec's own call, made so the build has an answer, and are flagged only
because they're new rather than because they're undecided.

**Resolved by the architecture document — final:**

1. **Stock stays a presence check, not a quantity decrement.** NFR-10 (requirements doc)
   asks for stock to decrement when an order is confirmed. The architecture doc deliberately
   defers real inventory counts as future work (YAGNI) and keeps `stockStatus` a
   manually-set enum, matching the contract's existing behavior. NFR-10 is out of scope for
   this build.
2. **No customer accounts; "My Orders" is phone-based.** The architecture doc explicitly
   lists customer accounts as deferred future work. `GET /api/orders/track?phone=`
   (unauthenticated, matching the contract) is the entire "My Orders" experience for v1 —
   FR-12/16's "optional registration/login" is not built.
3. **Cookie-only session, no session table.** The architecture doc explicitly chooses
   ASP.NET Core's self-contained encrypted cookie over a literal server-side session store,
   compensating with a live `StaffMember.active` recheck on every request (§7). This
   supersedes the contract's "cookie + session store" phrasing.
4. **OTP login is in scope, extending the contract.** `verify-otp`/`resend-otp` (§6.1) don't
   exist in the original contract; the architecture doc adds them per the newer
   requirements doc (FR-42–45). Built as specified in §6.1.
5. **Reports require a dedicated `View Reports` permission.** The contract left
   `/api/admin/reports` open to any authenticated admin; the architecture doc explicitly
   schedules adding this permission before launch (its own Phase 5). Built as specified in
   §7.

**Not covered by either source document — this spec's own addition, built as specified:**

6. **Payment-proof upload.** FR-19 requires it; neither the contract's `Order` model nor
   its endpoint list has a field or route for it. §4/§6.3/§9 add `Order.paymentProofUrl`
   and `POST /api/orders/:id/payment-proof` to satisfy FR-19 — this is the design, not a
   proposal awaiting approval.
7. **Bulk product upload (CSV/Excel).** FR-26 requires it; absent from the contract.
   §6.6 adds `POST /api/admin/products/bulk-upload` with a per-row success/error summary.
   The one thing actually needed from the business owner here isn't a design decision but
   a data artifact: a sample file, so the exact column order/headers can be nailed down
   before the handler is written.

One item is a straightforward fact, not a decision: the **order status enum** uses the
contract's 7 states (splitting `OutForDelivery`/`ReadyForPickup` by fulfillment type)
rather than the requirements doc's 6, simply because the contract is the more precise
source and matches the live frontend code being replaced.

---

## 12. Non-functional requirements

- **Mobile-first:** most traffic is expected on phones; no NFR here is backend-specific
  beyond making sure responses are small/fast enough for average mobile data.
- **Performance:** image responses already resized server-side (§10); target page loads
  under ~3s on 3G/4G is primarily a frontend concern but constrains upload handling
  (resize before store, never serve originals).
- **Security:** admin panel behind authentication for all admin/staff access; permission
  checks enforced server-side only, never trusted from the client; customer PII (name,
  phone, address, payment proof) stored securely and access-restricted; OTP required on
  every login.
- **Availability target:** 99%+ uptime suitable for a commercial storefront.
- **Scalability:** architecture supports catalog/order growth without redesign — this is
  the direct payoff of Clean Architecture + Postgres over anything more exotic.
- **Maintainability:** new capabilities (online payments, WhatsApp Business API
  automation) addable without rebuilding the core — Strategy pattern on
  `IOrderPricingService`/`IPaymentGateway`, Composite on notifications.
- **Data integrity:** see Conflict #1 above re: stock decrement timing.
- **Compatibility:** standard REST/JSON — no backend constraint beyond correct CORS config
  for the storefront + admin panel origins.

---

## 13. Testing strategy

**Unit tests** (`DelightsBoutique.UnitTests`) — Domain + Application only, no DB, no HTTP:
`Order.TransitionTo()` legal/illegal transitions, `Product.EnsureAvailable()`, pricing
(retail vs. wholesale vs. below-MOQ), handlers tested against mocked
repository/service interfaces. Arrange-Act-Assert, one behavior per test, named
`MethodOrScenario_ExpectedOutcome`.

**Integration tests** (`DelightsBoutique.IntegrationTests`) — full API via
`WebApplicationFactory` against a real Postgres via Testcontainers (never an in-memory EF
provider — it hides real SQL/constraint behavior). One test class per controller: happy
path + contract-mandated edges (stale/invalid slug → 400, duplicate category slug → 409,
staff acting outside permissions → 403, unauthenticated `/api/admin/audit-log` → 401).
Idempotency gets a dedicated test: same key + same body → same response twice; same key +
different body → 409.

**Coverage bar:** 100% of the business rules in §9 covered by at least one unit test —
not a blanket line-coverage percentage.

**Test data builders** (`OrderBuilder`, `ProductBuilder` — fluent, sensible defaults)
replace repetitive literal setup.

---

## 14. Build roadmap

| Phase | Timeframe | Focus | Gate |
|---|---|---|---|
| 0. Setup & scaffolding | Week 1 | Solution/project skeleton, CI pipeline, Docker Compose for local Postgres | — |
| 1. Domain, DB & auth | Weeks 2–3 | Entities, EF Core migrations, seed data, cookie auth + OTP login flow | Auth live end-to-end |
| 2. Catalog & content | Week 4 | Product/category endpoints, public content endpoints | — |
| 3. Orders & idempotency | Weeks 5–6 | Checkout, server-side pricing, stock check, idempotency keys, payment-proof upload | Checkout live end-to-end in Staging |
| 4. Admin & notifications | Weeks 7–8 | Admin order/customer/staff management, email+WhatsApp composite notifications, bulk product upload | — |
| 5. Reports & hardening | Weeks 9–10 | `View Reports` permission, reports, audit log, rate limiting, security review, full integration coverage, deploy | — |

Each gate is go/no-go: checkout isn't "done" until a real end-to-end order (create, price,
stock-check, idempotency-protected) passes in Staging — not just unit tests.

**Environments:** Development (local Docker Compose: API + Postgres), Staging, Production
— each with its own `appsettings.{Environment}.json` + environment-variable secrets. Email
always uses a real sandbox-tier provider even in Development (OTP depends on it);
`NoopPaymentGateway` (future) is fine as the dev/staging default once payments exist.

**CI/CD:** on every PR — restore, build, unit tests, integration tests (Testcontainers
Postgres in the CI runner), OpenAPI-vs-this-document conformance check. On merge to main —
build/push Docker image, run `dotnet ef database update` against Staging, smoke-test
`/api/health`, then promote to Production the same way.

---

## 15. How to use this document as a build prompt

When handing this to an engineer or coding agent to actually implement:

1. §11 is already resolved — build to it as written. The only outstanding input needed
   from the business owner is a sample file for the bulk-upload column schema (§11, item 7).
2. Build phase by phase per §14; each phase should end with something runnable and
   testable, not a pile of unintegrated code.
3. Every endpoint in §6 should exist with exactly the method/path/request/response shape
   given — this is what keeps the OpenAPI output diffable against this document as a
   conformance check.
4. Every rule in §9 needs at least one unit test proving it's enforced server-side, not
   just documented.
5. Don't build anything from "Deliberately excluded" (§4) or treat §1's YAGNI principle as
   optional — cart/wishlist stay client-side, real inventory counts and customer accounts
   stay out of v1 per §11.
