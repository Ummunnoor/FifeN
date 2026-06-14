using Domain.Entities.Catalog;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Entities.Interactions;
using Domain.Entities.Reviews;
using Domain.Entities.Vendors;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.Seeding
{
    /// <summary>
    /// Idempotent development/demo seed: RBAC roles, a founder + two admins, the five launch categories,
    /// a pilot cohort of verified ("Trusted") vendors with live listings, and a handful of interactions
    /// and reviews — several flagged cross-discovery — so discovery feeds and the admin dashboard have
    /// real data to show. Every step checks for existence first, so it is safe to run on every startup.
    /// </summary>
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
        {
            var roles = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var users = sp.GetRequiredService<UserManager<User>>();
            var db = sp.GetRequiredService<FifeNDbContext>();

            await SeedRolesAsync(roles);
            await SeedAdminsAsync(users);
            var categories = await SeedCategoriesAsync(db, ct);
            var vendors = await SeedVendorsAsync(users, db, ct);
            var buyers = await SeedBuyersAsync(users);
            await SeedCatalogAndEngagementAsync(db, categories, vendors, buyers, ct);
        }

        private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roles)
        {
            foreach (var role in Enum.GetNames<AppRole>())
                if (!await roles.RoleExistsAsync(role))
                    await roles.CreateAsync(new IdentityRole<Guid>(role));
        }

        // Fixed authenticator secret (Base32) shared by all seeded admins so the admin surface is
        // reachable in demos/tests without a manual /auth/mfa/enroll round-trip. Add this secret to an
        // authenticator app (issuer "TradeNaija") to generate the 6-digit TOTP codes the admins need at
        // login. DEMO/DEV ONLY — a real deployment must enroll each admin with a per-user secret.
        internal const string DemoAdminAuthenticatorKey = "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";

        // The internal token coordinates ASP.NET Identity uses to persist an authenticator key
        // (AspNetUserTokens). There is no public setter that takes a value, so we write it directly.
        private const string IdentityLoginProvider = "[AspNetUserStore]";
        private const string AuthenticatorKeyTokenName = "AuthenticatorKey";

        private static async Task SeedAdminsAsync(UserManager<User> users)
        {
            await EnsureUserAsync(users, "+447928785373", "Khaleelah", "Founder", nameof(AppRole.Admin), isAdmin: true, isOwner: true);
            await EnsureUserAsync(users, "+2348070928156", "Sherif", "Akinyemi", nameof(AppRole.Admin), isAdmin: true);
            await EnsureUserAsync(users, "+2348071074683", "Raqeeb", "Akinyemi", nameof(AppRole.Admin), isAdmin: true);
        }

        /// <summary>
        /// Enrolls a seeded admin with the shared demo authenticator key and enables 2FA, so OTP+TOTP
        /// login and the amr=mfa gate work out of the box. Idempotent: only writes when the stored key
        /// differs (which also backfills admins seeded before this enrollment existed).
        /// </summary>
        private static async Task EnsureAdminMfaAsync(UserManager<User> users, User user)
        {
            var current = await users.GetAuthenticatorKeyAsync(user);
            if (current != DemoAdminAuthenticatorKey)
                await users.SetAuthenticationTokenAsync(
                    user, IdentityLoginProvider, AuthenticatorKeyTokenName, DemoAdminAuthenticatorKey);

            if (!await users.GetTwoFactorEnabledAsync(user))
                await users.SetTwoFactorEnabledAsync(user, true);
        }

        private static async Task<IReadOnlyDictionary<string, Category>> SeedCategoriesAsync(
            FifeNDbContext db, CancellationToken ct)
        {
            var seed = new (string Name, string Slug, bool Consumable, int Sort)[]
            {
                ("Fashion & Clothing", "fashion-clothing", false, 1),
                ("Shoes", "shoes", false, 2),
                ("Home & Beddings", "home-beddings", false, 3),
                ("Food & Groceries", "food-groceries", true, 4),
                ("Beauty & Personal Care", "beauty-personal-care", true, 5),
            };

            foreach (var (name, slug, consumable, sort) in seed)
            {
                if (!await db.Categories.AnyAsync(c => c.Slug == slug, ct))
                    db.Categories.Add(new Category
                    {
                        Id = Guid.CreateVersion7(),
                        Name = name,
                        Slug = slug,
                        IsActive = true,
                        IsConsumable = consumable,
                        SortOrder = sort,
                        CreatedAtUtc = DateTimeOffset.UtcNow
                    });
            }
            await db.SaveChangesAsync(ct);

            return await db.Categories.AsNoTracking().ToDictionaryAsync(c => c.Slug, ct);
        }

        private static async Task<IReadOnlyList<VendorProfile>> SeedVendorsAsync(
            UserManager<User> users, FifeNDbContext db, CancellationToken ct)
        {
            var seed = new (string Phone, string First, string Last, string Business,
                NigerianState State, string City, VerificationMethod Method)[]
            {
                ("+2348000000021", "Lagos", "Ankara", "Lagos Ankara House", NigerianState.Lagos, "Lagos", VerificationMethod.Cac),
                ("+2348000000022", "Step", "Right", "StepRight Footwear", NigerianState.Lagos, "Lagos", VerificationMethod.Cac),
                ("+2348000000023", "Comfy", "Home", "ComfyHome Beddings", NigerianState.Fct, "Abuja", VerificationMethod.Cac),
                ("+2348000000024", "Fresh", "Mart", "FreshMart Groceries", NigerianState.Oyo, "Ibadan", VerificationMethod.Cac),
                ("+2348000000025", "Glow", "Care", "GlowCare Beauty", NigerianState.Lagos, "Lagos", VerificationMethod.Nin),
            };

            foreach (var v in seed)
            {
                if (await db.VendorProfiles.AnyAsync(p => p.BusinessName == v.Business, ct))
                    continue;

                var user = await EnsureUserAsync(users, v.Phone, v.First, v.Last, nameof(AppRole.Vendor), isVendor: true);
                db.VendorProfiles.Add(new VendorProfile
                {
                    Id = Guid.CreateVersion7(),
                    UserId = user.Id,
                    BusinessName = v.Business,
                    WhatsAppNumber = v.Phone,
                    VerificationMethod = v.Method,
                    VerificationStatus = VerificationStatus.Verified,
                    VerifiedName = $"{v.First} {v.Last}",
                    NameMatch = true,
                    TrustTier = TrustTier.Trusted,
                    ApprovedListingCount = 3,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                    ApprovedAtUtc = DateTimeOffset.UtcNow
                });
            }
            await db.SaveChangesAsync(ct);

            return await db.VendorProfiles.AsNoTracking().ToListAsync(ct);
        }

        private static async Task<IReadOnlyList<User>> SeedBuyersAsync(UserManager<User> users)
        {
            var ngozi = await EnsureUserAsync(users, "+2348000000011", "Ngozi", "Eze", nameof(AppRole.User));
            var tunde = await EnsureUserAsync(users, "+2348000000012", "Tunde", "Bello", nameof(AppRole.User));
            return new[] { ngozi, tunde };
        }

        private static async Task SeedCatalogAndEngagementAsync(
            FifeNDbContext db,
            IReadOnlyDictionary<string, Category> categories,
            IReadOnlyList<VendorProfile> vendorList,
            IReadOnlyList<User> buyers,
            CancellationToken ct)
        {
            // Products gate the whole catalog/engagement block: if any exist, it has already run.
            if (await db.Products.AnyAsync(ct))
                return;

            var vendors = vendorList.ToDictionary(v => v.BusinessName, StringComparer.OrdinalIgnoreCase);
            var now = DateTimeOffset.UtcNow;

            Product Listing(string vendor, string slug, string title, string description, decimal price,
                PriceType priceType, ProductCondition condition, NigerianState state, string city, int daysAgo) =>
                new()
                {
                    Id = Guid.CreateVersion7(),
                    VendorProfileId = vendors[vendor].Id,
                    CategoryId = categories[slug].Id,
                    Title = title,
                    Description = description,
                    Price = new Money(price),
                    PriceType = priceType,
                    Condition = condition,
                    Location = new Location(state, city),
                    Status = ListingStatus.Live,
                    Attributes = new Dictionary<string, string>(),
                    CreatedAtUtc = now.AddDays(-daysAgo),
                    UpdatedAtUtc = now.AddDays(-daysAgo)
                };

            var products = new List<Product>
            {
                Listing("Lagos Ankara House", "fashion-clothing", "Ankara Print Dress", "Vibrant handmade Ankara dress.", 12000m, PriceType.Negotiable, ProductCondition.New, NigerianState.Lagos, "Lagos", 6),
                Listing("Lagos Ankara House", "fashion-clothing", "Ankara Flared Skirt", "Matching Ankara flared skirt.", 7000m, PriceType.Negotiable, ProductCondition.New, NigerianState.Lagos, "Lagos", 3),
                Listing("StepRight Footwear", "shoes", "Men's Leather Loafers", "Classic handcrafted leather loafers.", 9500m, PriceType.Fixed, ProductCondition.New, NigerianState.Lagos, "Lagos", 5),
                Listing("StepRight Footwear", "shoes", "Canvas Sneakers", "Lightweight everyday canvas sneakers.", 11000m, PriceType.Fixed, ProductCondition.New, NigerianState.Lagos, "Lagos", 2),
                Listing("ComfyHome Beddings", "home-beddings", "6-Piece Bedding Set", "Soft cotton bedding set, queen size.", 18000m, PriceType.Fixed, ProductCondition.New, NigerianState.Fct, "Abuja", 5),
                Listing("ComfyHome Beddings", "home-beddings", "Duvet & Pillow Set", "Warm duvet with two pillows.", 22000m, PriceType.Fixed, ProductCondition.New, NigerianState.Fct, "Abuja", 1),
                Listing("FreshMart Groceries", "food-groceries", "Beans Powder 1kg", "Stone-free milled beans powder.", 3500m, PriceType.Fixed, ProductCondition.New, NigerianState.Oyo, "Ibadan", 4),
                Listing("FreshMart Groceries", "food-groceries", "Garri 5kg", "Premium white garri.", 5000m, PriceType.Fixed, ProductCondition.New, NigerianState.Oyo, "Ibadan", 2),
                Listing("GlowCare Beauty", "beauty-personal-care", "Liquid Soap 5L", "Gentle moisturising liquid soap.", 4000m, PriceType.Fixed, ProductCondition.New, NigerianState.Lagos, "Lagos", 3),
                Listing("GlowCare Beauty", "beauty-personal-care", "Toilet Wash 1L", "Antibacterial toilet wash.", 1800m, PriceType.Fixed, ProductCondition.New, NigerianState.Lagos, "Lagos", 1),
            };
            db.Products.AddRange(products);
            await db.SaveChangesAsync(ct);

            // Attach placeholder gallery images (a cover + two more) to every seeded listing so grids
            // and detail pages render with imagery out of the box. Deterministic per product via picsum.
            var images = new List<ProductImage>();
            foreach (var product in products)
            {
                var key = product.Id.ToString("N");
                string[] suffixes = ["", "b", "c"];
                for (var i = 0; i < suffixes.Length; i++)
                {
                    images.Add(new ProductImage
                    {
                        Id = Guid.CreateVersion7(),
                        ProductId = product.Id,
                        CloudinaryPublicId = $"seed/{key}-{i}",
                        Url = $"https://picsum.photos/seed/{key}{suffixes[i]}/800/600",
                        IsCover = i == 0,
                        SortOrder = i,
                        CreatedAtUtc = now
                    });
                }
            }
            db.ProductImages.AddRange(images);
            await db.SaveChangesAsync(ct);

            var ngozi = buyers[0];
            var tunde = buyers[1];

            Interaction Lead(User buyer, Product product, bool crossDiscovery, int daysAgo, LeadStatus status) =>
                new()
                {
                    Id = Guid.CreateVersion7(),
                    BuyerUserId = buyer.Id,
                    VendorProfileId = product.VendorProfileId,
                    ProductId = product.Id,
                    BuyerMessage = "Hi, is this still available?",
                    LeadStatus = status,
                    IsCrossDiscovery = crossDiscovery,
                    CreatedAtUtc = now.AddDays(-daysAgo)
                };

            var leads = new List<Interaction>
            {
                Lead(ngozi, products[0], crossDiscovery: true, 6, LeadStatus.Viewed),   // first contact with Ankara House
                Lead(ngozi, products[4], crossDiscovery: true, 5, LeadStatus.New),      // first contact with ComfyHome
                Lead(ngozi, products[1], crossDiscovery: false, 4, LeadStatus.New),     // Ankara House again — not cross-discovery
                Lead(tunde, products[6], crossDiscovery: true, 5, LeadStatus.Viewed),   // first contact with FreshMart
                Lead(tunde, products[8], crossDiscovery: true, 3, LeadStatus.New),      // first contact with GlowCare
                Lead(tunde, products[0], crossDiscovery: true, 2, LeadStatus.New),      // first contact with Ankara House (for Tunde)
            };
            db.Interactions.AddRange(leads);
            await db.SaveChangesAsync(ct);

            Review Review(User author, Product product, Interaction gate, int rating, string text, int daysAgo) =>
                new()
                {
                    Id = Guid.CreateVersion7(),
                    AuthorUserId = author.Id,
                    ProductId = product.Id,
                    VendorProfileId = product.VendorProfileId,
                    InteractionId = gate.Id,
                    Rating = rating,
                    Text = text,
                    Status = ReviewStatus.Visible,
                    CreatedAtUtc = now.AddDays(-daysAgo)
                };

            db.Reviews.AddRange(
                Review(ngozi, products[0], leads[0], 5, "Lovely fabric and a quick reply.", 5),
                Review(ngozi, products[4], leads[1], 4, "Comfortable and well made.", 4),
                Review(tunde, products[6], leads[3], 5, "Fresh and well packaged.", 4),
                Review(tunde, products[8], leads[4], 4, "Good value for money.", 2));
            await db.SaveChangesAsync(ct);
        }

        private static async Task<User> EnsureUserAsync(
            UserManager<User> users, string phone, string first, string last, string role,
            bool isAdmin = false, bool isOwner = false, bool isVendor = false)
        {
            var existing = await users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (existing is not null)
            {
                if (isAdmin) await EnsureAdminMfaAsync(users, existing);
                return existing;
            }

            var user = new User
            {
                Id = Guid.CreateVersion7(),
                UserName = phone,
                PhoneNumber = phone,
                PhoneNumberConfirmed = true,
                FirstName = first,
                LastName = last,
                IsAdmin = isAdmin,
                IsOwner = isOwner,
                IsVendor = isVendor,
                // Admins require a satisfied second factor (amr=mfa) for the RequireAdmin policy. Seeded
                // admins are marked MFA-enabled so the demo admin surface is reachable; a real deployment
                // re-enrolls via /auth/mfa/enroll against an authenticator app.
                TwoFactorEnabled = isAdmin,
                Status = UserStatus.Active,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                LastActiveAtUtc = DateTimeOffset.UtcNow
            };

            var result = await users.CreateAsync(user);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Failed to seed user {phone}: {string.Join("; ", result.Errors.Select(e => e.Description))}");

            await users.AddToRoleAsync(user, role);
            if (isAdmin) await EnsureAdminMfaAsync(users, user);
            return user;
        }
    }
}
