<!-- Solution Architecture for the TradeNaija/FifeN MVP. Companion to BRD v2.1. -->

# TradeNaija / FifeN — Solution Architecture Options

**Author:** Solution Architect
**Companion to:** BRD v2.1
**Context that shapes every decision below:**
- **Existing asset:** a working .NET 9 clean-architecture monolith (API → Application → Domain; Persistence → Domain), PostgreSQL via EF Core, ASP.NET Identity + JWT/refresh, FluentValidation, AutoMapper, Serilog. This is not greenfield.
- **Team:** ~3 people, .NET-literate, partly non-technical, building with AI assistance.
- **Budget:** ₦150k–700k for 6 months. Termii (OTP/WhatsApp) is the main variable cost.
- **Timeline:** MVP launch end of Month 3.
- **Scale (MVP target):** ~1,000 users, ~500 listings, ~50 vendors. Small.
- **Binding NFRs:** ≤3s load at p75 on slow-4G; PWA/low-bandwidth; 99.5% uptime; daily backups (PITR preferred, RPO ≤24h, RTO ≤4h); HTTPS, RBAC, rate limiting, admin MFA, immutable audit log; NDPA (no raw NIN/BVN stored); i18n-ready.

The bounded contexts from the BRD are the same in every option; only how they are packaged and deployed changes:
**Identity & Access · Vendors & Verification (KYC) · Catalog · Discovery & Search · Interactions & Leads · Reviews · Trust & Safety (Reports, Audit, Moderation) · Notifications · Admin & Analytics.**

---

## Option A — Modular Monolith (evolve the existing FifeN codebase) — RECOMMENDED

### 1. Architecture style
A single deployable ASP.NET Core application, internally partitioned into modules with enforced boundaries. This keeps the existing 4-layer structure and adds feature-module organization inside it.

### 2. Project structure & module boundaries
Keep `API / Application / Domain / Persistence`, and organize each by module so a feature lives in one vertical slice:
```
Application/Modules/{Identity, Vendors, Catalog, Discovery, Interactions, Reviews, TrustSafety, Notifications, Admin}
Domain/Modules/{...}          // entities + enums per context
Persistence/Modules/{...}     // EF configs, repositories per context
API/Controllers/{...}         // thin controllers per context
```
- Modules talk to each other only through published interfaces (or MediatR requests), never by reaching into another module's repositories. This is the discipline that earns the "modular" in modular monolith and gives a clean extraction path later.
- The existing `Order / OrderItem / PaymentTransaction` and geospatial code is **parked behind a feature flag / kept out of the DI graph** (per BRD v2.1 — discovery-only MVP), not deleted.
- New modules to add: Interactions & Leads, Trust & Safety (Reports + immutable AuditLog), KYC under Vendors, Discovery read models (incl. the public Vendor Shop Page), Notifications worker.

### 3. Database choice & schema approach
- **Single managed PostgreSQL** (Render/Neon/Supabase Postgres). The domain is highly relational (User → VendorProfile → Product → Review/Interaction), so relational is the right fit and you already use it.
- **One database, logical separation by schema or table-prefix per module**; EF Core migrations (already in place).
- Use Postgres-native features to avoid extra infrastructure: **full-text search (`tsvector` + GIN)** and **`pg_trgm`** for typo-tolerant search instead of a separate search engine; **JSONB** for flexible product attributes; partial/expression indexes for "active listing" queries.
- **AuditLog** is an append-only table (no updates/deletes). KYC stores only verification *results* — no raw NIN/BVN.
- Reliability via the managed provider's **point-in-time recovery**.

### 4. API design approach
- **REST/JSON**, versioned (`/api/v1`), cursor or page-based pagination for listings. Simple, cacheable, ideal for a low-tech PWA with straightforward screens.
- Public GET endpoints (browse, search, shop page) are cache-friendly and CDN-frontable; mutation endpoints require auth.
- **Inbound webhooks** as REST endpoints for Termii (OTP delivery status) and the KYC provider callback.
- No GraphQL/gRPC — they add surface area and tooling cost with no payoff at this scale.

### 5. Authentication & authorization
- **Phone + OTP** (Termii, WhatsApp-first → SMS fallback) issues **JWT access + refresh** (already built). Stateless JWT means the app scales horizontally without sticky sessions.
- **RBAC** via ASP.NET Identity roles (Buyer/User, Vendor, Admin, Support). **TOTP MFA** required for Admin.
- Anonymous allowed for browse/search/shop-page; login gates Interest/Lead and Reviews.
- Cross-cutting **middleware**: rate limiting (OTP + auth endpoints), HTTPS enforcement, and **audit logging of admin actions** (centralized — easy in a monolith).

### 6. Deployment model
- **One Docker container** on Render (web service) + **managed PostgreSQL** + **Cloudinary** (images, `f_auto,q_auto`, CDN) + **Termii** + **Sentry**.
- **One background worker** (a second small Render worker or a hosted-service queue) for async/scheduled work: notification fan-out, stale-listing nudges, the ~48h review nudge, image post-processing, dashboard read-model refresh. This keeps request latency low without splitting the app.
- **GitHub Actions → Render** CI/CD. Single region (Frankfurt is the closest practical Render region to Nigeria) with a **CDN (e.g., Cloudflare)** in front of public GETs and static assets to fight latency.


