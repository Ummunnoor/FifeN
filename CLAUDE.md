# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build FifeN.sln
dotnet run --project API/API.csproj            # dev: auto-migrates + seeds demo data
dotnet run --project API/API.csproj -- --seed  # force DbSeeder outside Development
```

### Database Migrations (EF Core)

```bash
dotnet ef migrations add <MigrationName> --project Persistence --startup-project API
dotnet ef database update --project Persistence --startup-project API
```

## Project Overview

FifeN is the backend for **TradeNaija**, a WhatsApp-first discovery marketplace for Nigeria, built
with .NET 9 in a clean 4-layer architecture. The current codebase implements the TradeNaija MVP per
the specs at the repo root — **`BACKEND_SPEC.md` is the implementation source of truth** (entities §1,
EF config §2, endpoints §3, authz §4), with `BRD.md` and `ARCHITECTURE.md` for product/design context.

MVP scope: phone+OTP auth, vendor onboarding/KYC, catalog, discovery/search, interactions (leads),
reviews, reports/trust-safety, admin dashboard, and notifications. **Payments, Shop, and Order are
out of MVP** — that legacy code is parked (excluded from compilation via `<Compile Remove>` in the
`.csproj` files, kept on disk), not deleted.

## Architecture

Dependency direction: `API → Application → Domain`; `Persistence → Domain`. Persistence is injected
at the API layer (composition root).

| Layer | Project | Responsibility |
|---|---|---|
| **API** | `API/` | Controllers (versioned `api/v1/...`), middleware wiring, Serilog, DI composition root |
| **Application** | `Application/` | Feature modules: service interfaces + implementations, DTOs (`record`s), FluentValidation validators, infrastructure **ports** (`Abstractions/`) |
| **Domain** | `Domain/` | Entities (Guid PKs, `...AtUtc` `DateTimeOffset`), enums (persisted as text), value objects (`Money`, `Location`) |
| **Persistence** | `Persistence/` | `FifeNDbContext` (PostgreSQL/Npgsql), EF configurations, feature-module repositories + **port adapters**, seeding |

### Feature-module layout

Both Application and Persistence are organized into parallel feature modules rather than flat
`Services/` folders:

- `Application/Modules/<Feature>/` — `DTOs/`, `Services/{Interfaces,Implementations}/`, `Validators/`
- `Persistence/Modules/<Feature>/` — repositories and adapters implementing that module's ports

Modules: **Identity, Vendors, Catalog, Discovery, Engagement** (interactions + reviews), **TrustSafety**
(reports), **Notifications, Admin**. `Persistence/Modules/Shared/` holds cross-cutting adapters
(`AuditLogger`, `NotificationService`).

### Ports & adapters

The Application layer defines infrastructure **ports** it depends on; Persistence provides the
**adapters**. Cross-cutting ports live in `Application/Abstractions/` (`ISecureHasher`,
`IImageStorageService`, `INotificationService`, `IAuditLogger`); module-specific ports live alongside
the module's interfaces (e.g. Identity's `ITokenService`, `IRefreshTokenStore`, `IUserAccountStore`,
`IPhoneVerificationStore`, `IOtpSender`). Several adapters are dev stubs to be swapped for real
integrations: `LoggingOtpSender` (→ Termii SMS), `DevImageStorageService` (→ Cloudinary),
`DevIdentityVerificationService` (→ real KYC).

### Key patterns

- **DI registration**: `AddApplicationServices()` in `Application/DependencyInjection/ServiceContainer.cs`
  and `AddPersistenceServices()` in **`Persistence/Middleware/DependencyInjection/ServiceContainer.cs`**
  (note the location), both called from `API/Program.cs`.
- **Generic repository**: open generic `IGeneric<>` → `GenericRepository<>` in `Persistence/Repositories/`.
- **Exception handling**: all unhandled exceptions flow through
  `Persistence/Middleware/ExceptionHandlingMiddleware.cs`, which emits **RFC 7807 ProblemDetails**.
  Typed `AppException`s in `Application/Exceptions/AppException.cs` map to HTTP status codes.
- **Validation**: FluentValidation via `IValidationService`; validators auto-registered from the
  `Application` assembly (`ServiceContainerMarker`).
- **Current user**: `ICurrentUserService` / `CurrentUserService` (`Persistence/Services/`) exposes the
  authenticated user's Guid id and role flags.
- **Logging**: Serilog (`IAppLogger<>` → `SerilogLoggerAdapter<>`).

### Auth flow

**Phone + OTP only** — there are no passwords and no email confirmation (the old email/password
Identity stack was replaced). `AuthenticationService` owns the OTP lifecycle (rate limits, 5-min
expiry, attempt lockout, rotation); `TokenService` issues JWTs (claims include `is_owner`/`is_vendor`/
`is_admin`); refresh tokens are persisted via `RefreshTokenStore`. Endpoints: `api/v1/auth`
(`otp/request`, `otp/verify`, `refresh`, `logout`). Nigerian numbers are normalized to `+234`.

Vendors submit a `VendorRequest`; an Admin approves/rejects it. Approval grants the Vendor role and
creates a `VendorProfile`. Listings from probation vendors are pre-moderated; vendors graduate to
Trusted after 3 approved products.

### Roles & authorization

`AppRole` enum (`Domain/Entities/Enums/`): `User`, `Admin`, `Vendor`, `Support`. Roles are seeded at
startup via `RoleSeeder`. Authorization policies (defined in the Persistence DI container):
`RequireVendor`, `RequireAdmin`, `RequireOwner` (the last keys off the `is_owner` claim).

### Domain model summary

- `User : IdentityUser<Guid>` — buyers and vendors share one user table; role determines capability.
- `VendorProfile` / `VendorRequest` — vendor onboarding + KYC/verification status.
- `Category`, `Product`, `ProductImage` — catalog; products have a tsvector/GIN full-text index.
- `Interaction` — a buyer lead (recorded before the `wa.me` handoff); tracks cross-discovery.
- `Review` — interaction-gated (48h cooldown), one per buyer/product, 30-day edit window.
- `Report`, `AuditLog` — trust & safety; auto-flag a vendor at an open-report threshold.
- `Notification` — in-app notification feed.
- `PhoneVerification` / `RefreshToken` — auth infrastructure.

### Seeding

`Persistence/Seeding/DbSeeder.cs` seeds an idempotent demo dataset (owner/admins, categories, Trusted
vendors, listings, interactions, reviews). It runs automatically in Development, or with an explicit
`--seed` flag otherwise. `RoleSeeder` always runs (every environment).

## Configuration

The app reads `appsettings.json` / `appsettings.Development.json`. Required keys:

- `ConnectionStrings:DefaultConnection` — PostgreSQL connection string
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes`

CORS is pre-configured for `http://localhost:5173` (Vite) and `http://localhost:3000` (React).

## Package management

NuGet versions are centralized in `Directory.Packages.props`; build properties (nullable, implicit
usings) in `Directory.Build.props`. **Do not set package versions inside individual `.csproj` files.**
