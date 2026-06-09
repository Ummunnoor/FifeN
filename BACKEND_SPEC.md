<!-- Backend Implementation Specification for TradeNaija/FifeN MVP. Companion to BRD v2.1 and ARCHITECTURE.md. -->

# TradeNaija / FifeN — Backend Implementation Specification

**Target stack:** .NET 9, ASP.NET Core, EF Core 9 (Npgsql), PostgreSQL, ASP.NET Identity + JWT/refresh, FluentValidation, AutoMapper, Serilog.
**Architecture:** Modular monolith (single `FifeNDbContext`, one deployable, module-organized folders) per ARCHITECTURE.md.
**Companion to:** BRD v2.1.

## Conventions (apply throughout)
- **Async + CancellationToken:** every async method — controllers, services, repositories — takes a `CancellationToken ct` and forwards it to EF Core (`ToListAsync(ct)`, `SaveChangesAsync(ct)`, etc.). Controller actions accept `CancellationToken ct` (ASP.NET binds the request-abort token automatically).
- **Keys:** all entities use `Guid` primary keys; `User : IdentityUser<Guid>`. Guids are sequential (`Guid.CreateVersion7()`) to keep index locality.
- **Timestamps:** UTC, suffixed `...AtUtc`, type `DateTimeOffset`.
- **Enums:** persisted as text via `.HasConversion<string>()` for readability and safe evolution.
- **DTOs:** request/response are `record` types. Entities are never returned directly; map via AutoMapper.
- **Errors:** RFC 7807 `ProblemDetails`, produced centrally by `ExceptionHandlingMiddleware`. Shape:
  ```json
  { "type": "...", "title": "...", "status": 400, "detail": "...",
    "errors": { "field": ["message"] }, "traceId": "..." }
  ```
  Mapping: validation → 400; unauthenticated → 401; forbidden (authn ok, authz fail) → 403; missing → 404; business-rule violation → 422; concurrency/duplicate → 409; rate limit → 429.
- **Soft delete:** `Product` and `Review` use a status enum (no physical delete); a global query filter hides non-visible rows from public reads. `AuditLog` is **append-only** (insert only).
- **Money:** value object `Money(decimal Amount, string Currency = "NGN")`, mapped as an owned type.
- **Location:** value object `Location(NigerianState State, string City)`, mapped as an owned type.

---

## 1. Entity Definitions

### 1.1 Enums
```csharp
public enum AppRole { User, Vendor, Admin, Support } // "Buyer" == base User role

public enum UserStatus { Active, Suspended, Deleted }

public enum VerificationMethod { Nin, Cac }            // BVN intentionally excluded (BRD v2.1)
public enum VerificationStatus { Pending, Verified, Failed }
public enum TrustTier { Pending, Probation, Trusted }   // Probation = first 3 listings pre-moderated

public enum VendorRequestStatus { Pending, Approved, Rejected }
public enum RejectionReason { NameMismatch, UnreadableDocument, IneligibleBusiness, SuspectedFraud, Other }

public enum PriceType { Fixed, Negotiable, ContactForPrice }
public enum ProductCondition { New, Used, Sealed }      // Sealed/New only for consumable categories
public enum ListingStatus { Live, Unavailable, Archived, Removed }

public enum LeadStatus { New, Viewed, Closed }
public enum ReviewStatus { Visible, Hidden, Removed }

public enum ReportTargetType { Listing, Vendor, Review }
public enum ReportReason { Prohibited, Counterfeit, Scam, Offensive, Spam, WrongCategory, Other }
public enum ReportStatus { Open, Actioned, Dismissed }

public enum OtpChannel { WhatsApp, Sms }
public enum OtpStatus { Pending, Verified, Expired, Locked }

public enum NotificationType {
    VendorApproved, VendorRejected, ListingApproved, ListingTakenDown,
    NewReview, NewLead, ReviewNudge, NewInCategory, Security
}
public enum NotificationChannel { InApp, WhatsApp, Sms }

public enum NigerianState { Abia, Adamawa, AkwaIbom, Anambra, Bauchi, Bayelsa, Benue, Borno,
    CrossRiver, Delta, Ebonyi, Edo, Ekiti, Enugu, Fct, Gombe, Imo, Jigawa, Kaduna, Kano,
    Katsina, Kebbi, Kogi, Kwara, Lagos, Nasarawa, Niger, Ogun, Ondo, Osun, Oyo, Plateau,
    Rivers, Sokoto, Taraba, Yobe, Zamfara }
```

### 1.2 Value objects
```csharp
public sealed record Money(decimal Amount, string Currency = "NGN");
public sealed record Location(NigerianState State, string City);
```

### 1.3 Entities
```csharp
public class User : IdentityUser<Guid>
{
    // Inherited: Id, UserName, PhoneNumber, PhoneNumberConfirmed, Email (optional/unused), etc.
    public string FirstName { get; set; } = default!;
    public string LastName  { get; set; } = default!;
    public string? SecondaryPhoneNumber { get; set; } // recovery only, verified
    public bool IsVendor { get; set; }
    public bool IsAdmin  { get; set; }
    public bool IsOwner  { get; set; }                 // founder; can manage admins
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastActiveAtUtc { get; set; }

    public VendorProfile? VendorProfile { get; set; }  // 1:1, optional
}

public class VendorProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }                   // unique FK
    public User User { get; set; } = default!;
    public string BusinessName { get; set; } = default!; // unique, case-insensitive (citext)
    public string WhatsAppNumber { get; set; } = default!;
    public VerificationMethod VerificationMethod { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public string? VerifiedName { get; set; }          // returned by KYC provider
    public bool NameMatch { get; set; }
    public decimal? NameMatchConfidence { get; set; }
    public string? KycReference { get; set; }          // provider token only — NO raw NIN/CAC
    public TrustTier TrustTier { get; set; } = TrustTier.Pending;
    public int ApprovedListingCount { get; set; }      // drives probation -> trusted graduation
    public uint Version { get; set; }                  // xmin concurrency token
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class VendorRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string BusinessName { get; set; } = default!;
    public string WhatsAppNumber { get; set; } = default!;
    public VerificationMethod VerificationMethod { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public bool NameMatch { get; set; }
    public string? KycReference { get; set; }
    public VendorRequestStatus Status { get; set; } = VendorRequestStatus.Pending;
    public RejectionReason? RejectionReason { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public uint Version { get; set; }                  // optimistic concurrency for admin actions
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;       // unique
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsConsumable { get; set; }             // forces Condition = New/Sealed
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class Product
{
    public Guid Id { get; set; }
    public Guid VendorProfileId { get; set; }
    public VendorProfile Vendor { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;
    public string Title { get; set; } = default!;       // <= 80 chars
    public string Description { get; set; } = default!;  // <= 1000 chars
    public Money Price { get; set; } = default!;         // owned
    public PriceType PriceType { get; set; } = PriceType.Fixed;
    public ProductCondition Condition { get; set; }
    public Location Location { get; set; } = default!;   // owned (State + City)
    public ListingStatus Status { get; set; } = ListingStatus.Live;
    public Dictionary<string, string> Attributes { get; set; } = new(); // jsonb
    public int ViewCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class ProductImage
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string CloudinaryPublicId { get; set; } = default!;
    public string Url { get; set; } = default!;
    public bool IsCover { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class Interaction // a lead
{
    public Guid Id { get; set; }
    public Guid BuyerUserId { get; set; }
    public Guid VendorProfileId { get; set; }
    public Guid ProductId { get; set; }
    public string? BuyerMessage { get; set; }
    public decimal? OfferPrice { get; set; }
    public LeadStatus LeadStatus { get; set; } = LeadStatus.New;
    public bool IsCrossDiscovery { get; set; }          // buyer had no prior interaction w/ vendor
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class Review
{
    public Guid Id { get; set; }
    public Guid AuthorUserId { get; set; }
    public Guid ProductId { get; set; }
    public Guid VendorProfileId { get; set; }           // denormalized for vendor roll-up
    public Guid InteractionId { get; set; }             // the gating interaction
    public int Rating { get; set; }                     // 1..5
    public string? Text { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Visible;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? EditedAtUtc { get; set; }
}

public class Report
{
    public Guid Id { get; set; }
    public Guid ReporterUserId { get; set; }
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Note { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public Guid? ResolvedByUserId { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class AuditLog // append-only
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = default!;      // e.g. "VendorRequest.Approve"
    public string ObjectType { get; set; } = default!;
    public Guid ObjectId { get; set; }
    public string? Reason { get; set; }
    public string? MetadataJson { get; set; }           // jsonb before/after snapshot
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class PhoneVerification // OTP, transient
{
    public Guid Id { get; set; }
    public string PhoneNumber { get; set; } = default!;
    public OtpChannel Channel { get; set; }
    public string CodeHash { get; set; } = default!;    // never store plaintext OTP
    public DateTimeOffset ExpiresAtUtc { get; set; }    // +5 min
    public int Attempts { get; set; }
    public OtpStatus Status { get; set; } = OtpStatus.Pending;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientUserId { get; set; }
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Title { get; set; } = default!;
    public string Body { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? SentAtUtc { get; set; }
}
```

### 1.4 Relationship summary
- `User 1—0..1 VendorProfile` · `User 1—* VendorRequest`
- `VendorProfile 1—* Product` · `Category 1—* Product`
- `Product 1—* ProductImage` · `Product 1—* Review` · `VendorProfile 1—* Review` (denormalized)
- `Product 1—* Interaction` · `User(buyer) 1—* Interaction` · `Interaction 1—0..1 Review`
- `User 1—* Report` (polymorphic target via `TargetType`+`TargetId`)
- `User 1—* RefreshToken` · `User 1—* Notification`

---

## 2. Database

### 2.1 EF Core mapping configuration
One `FifeNDbContext`; configurations live in `Persistence/Modules/{Context}/Configurations` and are applied via `modelBuilder.ApplyConfigurationsFromAssembly(...)`. Postgres extensions in `OnModelCreating`: `HasPostgresExtension("citext")`.

```csharp
public class VendorProfileConfiguration : IEntityTypeConfiguration<VendorProfile>
{
    public void Configure(EntityTypeBuilder<VendorProfile> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.BusinessName).HasColumnType("citext").IsRequired();
        b.HasIndex(x => x.BusinessName).IsUnique();            // case-insensitive via citext
        b.Property(x => x.VerificationMethod).HasConversion<string>();
        b.Property(x => x.VerificationStatus).HasConversion<string>();
        b.Property(x => x.TrustTier).HasConversion<string>();
        b.Property(x => x.Version).IsRowVersion();             // xmin concurrency
        b.HasOne(x => x.User).WithOne(u => u.VendorProfile)
            .HasForeignKey<VendorProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.UserId).IsUnique();
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(80).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Condition).HasConversion<string>();
        b.Property(x => x.PriceType).HasConversion<string>();
        b.Property(x => x.Status).HasConversion<string>();
        b.Property(x => x.Attributes).HasColumnType("jsonb");

        b.OwnsOne(x => x.Price, m => {
            m.Property(p => p.Amount).HasColumnName("PriceAmount").HasColumnType("numeric(18,2)");
            m.Property(p => p.Currency).HasColumnName("PriceCurrency").HasMaxLength(3);
        });
        b.OwnsOne(x => x.Location, l => {
            l.Property(p => p.State).HasColumnName("State").HasConversion<string>();
            l.Property(p => p.City).HasColumnName("City").HasMaxLength(80);
        });

        b.HasOne(x => x.Vendor).WithMany(v => v.Products).HasForeignKey(x => x.VendorProfileId);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId);

        // Full-text search (Npgsql generated tsvector + GIN), avoids a separate search engine.
        b.HasGeneratedTsVectorColumn(p => p.SearchVector!, "english", p => new { p.Title, p.Description })
         .HasIndex(p => p.SearchVector!).HasMethod("GIN");

        // Partial index for the hot "active listing" read path.
        b.HasIndex(x => new { x.CategoryId, x.CreatedAtUtc })
         .HasFilter("\"Status\" = 'Live'");

        // Public reads only see Live listings from approved-active vendors (enforced in queries).
    }
}
// NOTE: add `public NpgsqlTsVector? SearchVector { get; set; }` to Product for the FTS column.

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>();
        b.HasIndex(x => new { x.AuthorUserId, x.ProductId }).IsUnique(); // one review per buyer per product
        b.HasIndex(x => x.VendorProfileId);
        b.HasQueryFilter(r => r.Status == ReviewStatus.Visible);          // public default
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.MetadataJson).HasColumnType("jsonb");
        b.HasIndex(x => new { x.ObjectType, x.ObjectId });
        // Append-only enforced at the repository layer + a DB rule revoking UPDATE/DELETE in prod.
    }
}
```
Other configs follow the same pattern: enum→string conversions, FK relationships, unique indexes (`Category.Slug`, `PhoneVerification.PhoneNumber+Status`), and `Interaction` indexes on `(BuyerUserId, VendorProfileId)` (cross-discovery lookups) and `(VendorProfileId, LeadStatus)` (vendor leads).

### 2.2 EF Core migrations
- Single `FifeNDbContext`. Generate incrementally per feature slice:
  ```bash
  dotnet ef migrations add Init_Identity --project Persistence --startup-project API
  dotnet ef migrations add Add_VendorsAndKyc --project Persistence --startup-project API
  dotnet ef migrations add Add_CatalogAndImages --project Persistence --startup-project API
  dotnet ef migrations add Add_InteractionsReviewsReports --project Persistence --startup-project API
  dotnet ef migrations add Add_AuditAndNotifications --project Persistence --startup-project API
  dotnet ef database update --project Persistence --startup-project API
  ```
- **Apply policy:** auto-migrate on startup in Development only; in Production run `database update` (or `migrations bundle`) as a deliberate release step. Add a startup check that fails fast if there are pending model changes without a migration.
- Migrations that add the `citext` extension and the tsvector/GIN index are generated automatically from the configuration above; verify them in the generated migration before applying.

### 2.3 Seeding strategy
Idempotent `DbSeeder.SeedAsync(IServiceProvider, CancellationToken ct)` invoked at startup (Development) or via a one-off `--seed` flag (Production). Order: roles → founder/admins → categories → sample vendors → sample products → a few interactions/reviews. Each step checks existence before inserting.

```csharp
public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
{
    var roleMgr = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var r in Enum.GetNames<AppRole>())
        if (!await roleMgr.RoleExistsAsync(r))
            await roleMgr.CreateAsync(new IdentityRole<Guid>(r));
    // ... founder (IsOwner+IsAdmin), categories, sample vendors/products (see table) ...
}
```

**Sample data (seeded):**

| Type | Examples |
|---|---|
| Roles | User, Vendor, Admin, Support |
| Founder | 1 owner-admin (phone-verified) + 2 admins |
| Categories | Fashion & Clothing, Shoes, Home & Beddings (consumable=false); Food & Groceries, Beauty & Personal Care (consumable=true) |
| Vendors (pilot) | 5 Trusted vendors across categories/cities (Lagos, Abuja, Ibadan) |
| Products | ~10 (e.g., Ankara dress ₦12,000 Negotiable; men's loafers ₦9,500; bedding set ₦18,000; beans powder 1kg ₦3,500 New; liquid soap 5L ₦4,000 New; toilet wash ₦1,800 New) |
| Interactions | A handful, some flagged cross-discovery |
| Reviews | 1–2 per sampled product, each tied to a seeded interaction |

---

## 3. API Endpoints

Base path `/api/v1`. Auth column: **Anon** = anonymous; **Auth** = any authenticated user; role/policy noted otherwise. All actions accept `CancellationToken ct`. Standard error responses (`400/401/403/404/422/429`) follow the contract in Conventions; only endpoint-specific cases are listed.

### 3.1 Authentication & Identity
```csharp
public record OtpRequestRequest(string PhoneNumber);
public record OtpVerifyRequest(string PhoneNumber, string Code);
public record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc, UserSummary User);
public record UserSummary(Guid Id, string DisplayName, bool IsVendor, bool IsAdmin);
public record RefreshRequest(string RefreshToken);
```

| Method | Route | Auth | Request | Success | Rules & errors |
|---|---|---|---|---|---|
| POST | `/auth/otp/request` | Anon | `OtpRequestRequest` | `200` | Phone must be valid NG (+234). Rate limit 3/15min, 5/hr, 10/day per phone. WhatsApp-first→SMS fallback. Errors: `400` bad phone, `429` rate limited. |
| POST | `/auth/otp/verify` | Anon | `OtpVerifyRequest` | `200 AuthResponse` | Creates the user on first verify. Errors: `401` wrong code, `410` expired, `423` locked after 3 failed attempts, `429`. |
| POST | `/auth/refresh` | Anon | `RefreshRequest` | `200 AuthResponse` | Rotates refresh token. Errors: `401` invalid/expired/revoked. |
| POST | `/auth/logout` | Auth | `RefreshRequest` | `204` | Revokes the refresh token. |
| POST | `/auth/secondary-phone` | Auth | `{ string PhoneNumber }` | `202` | Sends OTP to add a verified recovery number. Errors: `409` already in use. |
| POST | `/auth/mfa/enroll` | Admin | — | `200 { string OtpAuthUri }` | TOTP enrollment. |
| POST | `/auth/mfa/verify` | Admin | `{ string Code }` | `204` | Activates MFA. Errors: `401` bad code. |

### 3.2 Vendor Onboarding & Profile
```csharp
public record CreateVendorRequestRequest(
    string BusinessName, string WhatsAppNumber, VerificationMethod Method, string IdentifierToken); // NIN/CAC sent to KYC, never persisted raw
public record VendorRequestResponse(Guid Id, VendorRequestStatus Status, bool NameMatch, DateTimeOffset CreatedAtUtc);
public record VendorPublicResponse(Guid Id, string BusinessName, bool Verified, double AverageRating, int ReviewCount);
public record UpdateVendorProfileRequest(string WhatsAppNumber);
```

| Method | Route | Auth | Request | Success | Rules & errors |
|---|---|---|---|---|---|
| POST | `/vendor/requests` | Auth | `CreateVendorRequestRequest` | `201 VendorRequestResponse` | Runs KYC; name must match. `BusinessName` unique (citext). Errors: `409` duplicate name / already a vendor, `422` KYC name mismatch, `400` invalid input. |
| GET | `/vendor/requests/me` | Auth | — | `200 VendorRequestResponse` | Latest request for caller. `404` none. |
| PATCH | `/vendor/profile` | Vendor | `UpdateVendorProfileRequest` | `200` | WhatsApp number validated reachable. |
| GET | `/vendors/{id}` | Anon | — | `200 VendorPublicResponse` | Public shop header. `404` if missing/suspended. |
| GET | `/vendors/{id}/products` | Anon | — | `200 PagedResponse<ProductSummary>` | Shop page: only Live listings; hidden if vendor suspended/unapproved. |

**Admin vendor moderation**

| Method | Route | Auth | Request | Success | Rules & errors |
|---|---|---|---|---|---|
| GET | `/admin/vendor-requests?status=Pending` | Admin | — | `200 PagedResponse<...>` | Queue. |
| POST | `/admin/vendor-requests/{id}/approve` | Admin | — | `200` | Grants Vendor role, creates `VendorProfile` (Probation), audits. Optimistic concurrency. Errors: `404`, `409` already actioned. |
| POST | `/admin/vendor-requests/{id}/reject` | Admin | `{ RejectionReason Reason }` | `200` | Notifies applicant with reason; re-apply allowed. `409` already actioned. |
| POST | `/admin/vendors/{id}/suspend` | Admin | `{ string Reason }` | `200` | Hides listings; reviews retained; audited. |
| POST | `/admin/vendors/{id}/reinstate` | Admin | — | `200` | Reverses suspension; audited. |

### 3.3 Catalog (Listings)
```csharp
public record CreateProductRequest(string Title, string Description, decimal PriceAmount,
    PriceType PriceType, ProductCondition Condition, Guid CategoryId, NigerianState State, string City,
    Dictionary<string,string>? Attributes);
public record UpdateProductRequest(string Title, string Description, decimal PriceAmount,
    PriceType PriceType, ProductCondition Condition, Guid CategoryId, NigerianState State, string City);
public record ChangeStatusRequest(ListingStatus Status); // Unavailable / Live / Archived
public record ProductSummary(Guid Id, string Title, decimal PriceAmount, string Currency, PriceType PriceType,
    string? CoverUrl, string City, NigerianState State, Guid VendorId, string VendorName, bool VendorVerified, double AverageRating);
public record ProductDetail(Guid Id, string Title, string Description, decimal PriceAmount, string Currency,
    PriceType PriceType, ProductCondition Condition, string CategoryName, NigerianState State, string City,
    IReadOnlyList<string> ImageUrls, Guid VendorId, string VendorName, bool VendorVerified,
    double AverageRating, int ReviewCount, ListingStatus Status, DateTimeOffset CreatedAtUtc);
```

| Method | Route | Auth | Request | Success | Rules & errors |
|---|---|---|---|---|---|
| POST | `/products` | Vendor | `CreateProductRequest` | `201 ProductDetail` | Vendor must be approved. Title ≤80, desc ≤1000, price >0. Condition must be valid for category (`New`/`Sealed` only if `Category.IsConsumable`). Max 50 active listings. Probation vendors' first 3 enter pre-publish moderation (`Status=Unavailable` until approved). Errors: `403` not approved, `422` business rule (cap/condition), `400`. |
| PUT | `/products/{id}` | Vendor (owner) | `UpdateProductRequest` | `200 ProductDetail` | Owner only; untrusted/reported items re-moderated. Errors: `403`, `404`. |
| PATCH | `/products/{id}/status` | Vendor (owner) | `ChangeStatusRequest` | `200` | Mark Unavailable/reactivate/Archive. `403`,`404`,`422` illegal transition. |
| DELETE | `/products/{id}` | Vendor (owner) / Admin | — | `204` | Soft delete → `Archived`. |
| POST | `/products/{id}/images` | Vendor (owner) | multipart | `201 { IReadOnlyList<string> Urls }` | 1–5 images, JPEG/PNG/WebP, ≤5MB; server validates after client compression. Errors: `413` too large, `415` type, `422` >5 images. |
| GET | `/products/{id}` | Anon | — | `200 ProductDetail` | Increments view (async, non-blocking). Hidden listings → `404`. |
| POST | `/admin/products/{id}/moderate` | Admin | `{ ListingStatus Status, string Reason }` | `200` | Approve (Probation), hide, or remove; audited; notifies vendor. |

### 3.4 Discovery & Search
```csharp
public record ProductQuery(string? Q, Guid? CategoryId, NigerianState? State, string? City,
    decimal? MinPrice, decimal? MaxPrice, ProductCondition? Condition, string Sort = "recent",
    int Page = 1, int PageSize = 20);
public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
```

| Method | Route | Auth | Request | Success | Rules & errors |
|---|---|---|---|---|---|
| GET | `/products` | Anon | `ProductQuery` (query string) | `200 PagedResponse<ProductSummary>` | FTS on `Q` (tsvector + `pg_trgm` typo tolerance). Location filter = State→City only (no geo). Sort: `recent` (default), `price_asc`, `price_desc`, `rating`. Excludes non-Live and suspended/unapproved vendors. `PageSize` capped at 50. Errors: `400` bad params. |
| GET | `/products/new` | Anon | `{ Guid? CategoryId }` | `200 PagedResponse<ProductSummary>` | "New this week" fresh-supply feed. |
| GET | `/categories` | Anon | — | `200 IReadOnlyList<CategoryResponse>` | Active categories only. |

### 3.5 Interactions & Leads
```csharp
public record ExpressInterestRequest(string? Message, decimal? OfferPrice);
public record InterestResponse(Guid InteractionId, string WhatsAppUrl); // wa.me link, pre-filled
public record LeadResponse(Guid Id, Guid ProductId, string ProductTitle, string BuyerDisplayName,
    string? Message, decimal? OfferPrice, LeadStatus Status, bool IsCrossDiscovery, DateTimeOffset CreatedAtUtc);
public record UpdateLeadRequest(LeadStatus Status);
```

| Method | Route | Auth | Request | Success | Rules & errors |
|---|---|---|---|---|---|
| POST | `/products/{id}/interest` | Auth | `ExpressInterestRequest` | `201 InterestResponse` | Records `Interaction` (the lead) **before** building the `wa.me` URL with pre-filled message. Sets `IsCrossDiscovery` = no prior interaction with this vendor. Repeat taps deduped for metrics. Errors: `404`, `409` product not Live. |
| GET | `/vendor/leads?status=New` | Vendor | — | `200 PagedResponse<LeadResponse>` | Caller's leads. |
| PATCH | `/vendor/leads/{id}` | Vendor (owner) | `UpdateLeadRequest` | `200` | Mark Viewed/Closed. `403`,`404`. |

### 3.6 Reviews
```csharp
public record CreateReviewRequest(int Rating, string? Text);
public record UpdateReviewRequest(int Rating, string? Text);
public record ReviewResponse(Guid Id, string AuthorDisplayName, int Rating, string? Text,
    bool ContactedVendor, DateTimeOffset CreatedAtUtc);
```

| Method | Route | Auth | Request | Success | Rules & errors |
|---|---|---|---|---|---|
| POST | `/products/{id}/reviews` | Auth | `CreateReviewRequest` | `201 ReviewResponse` | **Eligibility:** caller must have an `Interaction` with this product/vendor at least 48h old. Rating 1–5. One review per buyer per product. Badged "Reviewer contacted this vendor." Errors: `403` not eligible (no qualifying interaction / cooldown), `409` already reviewed, `400` rating range. |
| PUT | `/reviews/{id}` | Auth (author) | `UpdateReviewRequest` | `200 ReviewResponse` | Editable ≤30 days. Errors: `403` not author / window passed, `404`. |
| GET | `/products/{id}/reviews` | Anon | — | `200 PagedResponse<ReviewResponse>` | Visible reviews only. |
| PATCH | `/admin/reviews/{id}` | Admin | `{ ReviewStatus Status, string Reason }` | `200` | Hide/remove; audited. |

### 3.7 Reporting & Trust/Safety
```csharp
public record CreateReportRequest(ReportTargetType TargetType, Guid TargetId, ReportReason Reason, string? Note);
public record ResolveReportRequest(ReportStatus Status, string? Note);
```

| Method | Route | Auth | Request | Success | Rules & errors |
|---|---|---|---|---|---|
| POST | `/reports` | Auth | `CreateReportRequest` | `201` | Target must exist. Tracks report counts per vendor; threshold auto-flags. Errors: `404` target, `400`. |
| GET | `/admin/reports?status=Open` | Admin | — | `200 PagedResponse<...>` | Moderation queue. |
| PATCH | `/admin/reports/{id}` | Admin | `ResolveReportRequest` | `200` | Action/dismiss; audited. `409` already resolved. |

### 3.8 Admin Dashboard
```csharp
public record DashboardResponse(int RegisteredUsers, int ActiveUsers30d, int ActiveListings,
    double OtpSuccessRate, int Interactions30d, double CrossDiscoveryRate,
    IReadOnlyList<NameCount> TopCategories, IReadOnlyList<NameCount> TopLocations,
    int PendingApprovals, int OpenReports);
public record NameCount(string Name, int Count);
```

| Method | Route | Auth | Request | Success | Rules |
|---|---|---|---|---|---|
| GET | `/admin/dashboard` | Admin | — | `200 DashboardResponse` | Read models; "active" per BRD §9 (30-day windows); cross-discovery is the headline metric. |

### 3.9 Notifications
```csharp
public record NotificationResponse(Guid Id, NotificationType Type, string Title, string Body, bool IsRead, DateTimeOffset CreatedAtUtc);
```

| Method | Route | Auth | Request | Success | Rules |
|---|---|---|---|---|---|
| GET | `/notifications` | Auth | — | `200 PagedResponse<NotificationResponse>` | Caller's in-app notifications. |
| PATCH | `/notifications/{id}/read` | Auth (owner) | — | `204` | `403`,`404`. |

---

## 4. Authentication & Authorization

### 4.1 Mechanism
- Phone + OTP issues a **JWT access token** (short-lived, ~15 min) + **refresh token** (rotated, hashed at rest). Stateless JWT enables horizontal scaling.
- **Policies** (registered in `Program.cs`):
  - `RequireVendor` → role `Vendor` **and** `VendorProfile.VerificationStatus == Verified`.
  - `RequireAdmin` → role `Admin` (or `Support` where read-only), **and** MFA satisfied (`amr` claim contains `mfa`).
  - `RequireOwner` → `User.IsOwner` (admin management).
  - Resource-based handler `ResourceOwnerHandler` for "owner-only" routes (vendor editing own products/leads, author editing own review).

### 4.2 Access matrix
| Area | Anonymous | Authenticated (User/Buyer) | Vendor | Admin |
|---|---|---|---|---|
| Browse/search, product detail, categories | ✅ | ✅ | ✅ | ✅ |
| Public vendor shop page | ✅ | ✅ | ✅ | ✅ |
| OTP request/verify, refresh | ✅ | ✅ | ✅ | ✅ |
| Express interest / get WhatsApp link | ❌ | ✅ | ✅ | ✅ |
| Create/edit review (eligibility-gated) | ❌ | ✅ | ✅ | ✅ |
| Submit a report | ❌ | ✅ | ✅ | ✅ |
| Submit vendor request | ❌ | ✅ | ❌ (already vendor) | — |
| Create/edit/delete own listings, view own leads | ❌ | ❌ | ✅ (owner + verified) | ✅ (override) |
| Vendor metrics, update vendor profile | ❌ | ❌ | ✅ | ✅ |
| Approve/reject vendors, moderate content, dashboard | ❌ | ❌ | ❌ | ✅ (MFA) |
| Manage admins | ❌ | ❌ | ❌ | ✅ Owner only |
| Notifications (own) | ❌ | ✅ | ✅ | ✅ |

### 4.3 Cross-cutting guards
- **Rate limiting** middleware on OTP and auth endpoints (per phone + per IP), plus a global per-IP limiter.
- **Audit logging** middleware/decorator writes an append-only `AuditLog` row for every Admin mutation (approve/reject/suspend/moderate/manage-admin).
- **HTTPS** enforced; security headers; CORS limited to the PWA origins.
- **NDPA:** KYC identifiers are sent to the provider and never persisted; only results are stored.

---

## Assumptions & open items for implementation
- `Guid` keys and `User : IdentityUser<Guid>` (state explicitly if your current context uses `string` keys — adjust configs accordingly).
- KYC provider and Cloudinary are accessed behind interfaces (`IIdentityVerificationService`, `IImageStorageService`) so the concrete vendor can be chosen later (BRD dependency).
- `Order/OrderItem/PaymentTransaction` and geospatial code remain parked (feature-flagged out) per BRD v2.1 — not represented here.
- Search uses Postgres FTS + `pg_trgm`; no external search engine at MVP.
