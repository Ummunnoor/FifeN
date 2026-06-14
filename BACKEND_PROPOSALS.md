# Backend Proposals — gaps surfaced by the frontend build

Two small backend additions would close real gaps the web client hit while implementing the vendor
flows. Both are additive (no breaking changes) and align with existing patterns in the Catalog module.

---

## 1. Vendor "my listings" endpoint (all statuses)

### Problem
The only way to read a vendor's listings today is the **public** shop endpoint
`GET /api/v1/vendors/{id}/products` (`DiscoveryController`), which returns **live listings only**.
So the vendor management table (`MyListingsPage`) cannot show a vendor's own `Unavailable`,
`Archived`, or pre-moderation (`Probation`) listings — exactly the ones they most need to manage.
The frontend currently uses the public endpoint and documents the limitation in `ListingService.getMine`.

### Proposal
Add an authenticated, owner-scoped listing endpoint to `ProductController` (the vendor's own catalog),
returning every status with an optional status filter:

```
GET /api/v1/products/mine?status={ListingStatus?}&page=1&pageSize=20   [Authorize(Policy = "RequireVendor")]
→ PagedResponse<ProductSummary>   // or a richer ProductSummary that includes Status
```

Sketch (`API/Controllers/ProductController.cs`):
```csharp
/// <summary>The authenticated vendor's own listings, across all statuses.</summary>
[HttpGet("mine")]
[Authorize(Policy = "RequireVendor")]
public async Task<ActionResult<PagedResponse<ProductSummary>>> Mine(
    [FromQuery] ListingStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
    CancellationToken ct = default) =>
    Ok(await products.GetOwnListingsAsync(currentUser.RequireUserId(), status, page, pageSize, ct));
```

Add `GetOwnListingsAsync` to `IProductService` and a corresponding query in `ProductRepository`
(filter by `VendorId == userId`, no `status == Live` constraint). Reuse the existing `ProductSummary`
projection — but **include `Status`** so the table can render a status badge and gate transitions.

### Frontend follow-up
Switch `ListingService.getMine` from `/vendors/{id}/products` to `/products/mine`, add the status
column/filter, and drop the limitation note.

---

## 2. Expose `CategoryId` on `ProductDetail`

### Problem
`ProductDetail` (PDR §2.3) returns `categoryName` but not `categoryId`. On the edit-listing screen
(`ListingFormPage`) the category dropdown therefore can't be pre-selected, forcing the vendor to
re-pick their category on every edit — easy to get wrong, and a poor UX.

### Proposal
Add `CategoryId` to the `ProductDetail` record (`Application/Modules/Catalog/DTOs/ProductDtos.cs`):

```csharp
public record ProductDetail(
    Guid Id,
    string Title,
    string Description,
    decimal PriceAmount,
    string Currency,
    PriceType PriceType,
    ProductCondition Condition,
    Guid CategoryId,        // ← add
    string CategoryName,
    NigerianState State,
    ...);
```

Populate it wherever the detail projection is built (the product detail query in `ProductRepository`
already joins the category — it just needs to select the id alongside the name). Purely additive;
existing consumers ignore the new field.

### Frontend follow-up
In `ListingFormPage`, seed `form.categoryId` from `existing.categoryId` in the prefill effect and
remove the "vendor re-selects on edit" comment.

---

## 3. Let the client identify a buyer's own review (enable review editing)

### Problem
The edit endpoint already exists — `PUT /api/v1/reviews/{id}` (`ReviewController`, author-only, 30-day
window). But the web client cannot safely offer an **Edit** button, because the public
`ReviewResponse` (PDR §2.6) exposes no way to tell which review belongs to the current user:

```csharp
public record ReviewResponse(
    Guid Id, string AuthorDisplayName, int Rating, string? Text,
    bool ContactedVendor, DateTimeOffset CreatedAtUtc);   // ← no author id, no "isMine"/"canEdit"
```

Matching on `AuthorDisplayName` is unsafe (names aren't unique and can change), so the frontend
currently ships review **creation** only. `ProductService.updateReview` and a reusable review dialog
are already in place — they just need a sound signal of ownership.

### Proposal (pick one)
**Option A — a `canEdit` flag on `ReviewResponse` (preferred).** Smallest change, no extra round-trip,
and it also encodes the 30-day window so the UI shows Edit exactly when the API will accept it:

```csharp
public record ReviewResponse(
    Guid Id, string AuthorDisplayName, int Rating, string? Text,
    bool ContactedVendor, DateTimeOffset CreatedAtUtc,
    bool CanEdit);   // ← true when the caller is the author AND within the edit window
```
Compute it in the review projection using `ICurrentUserService` (false for anonymous readers).

**Option B — a dedicated read endpoint.** `GET /api/v1/products/{id}/reviews/me → ReviewResponse?`
(or `204` when none). Useful if you'd rather keep the public list anonymous, but it adds a call and
still wants the review `Id` to feed `PUT /reviews/{id}`.

Either way, the frontend needs the **author's own review `Id`** reachable; Option A delivers that on
the row it's already rendering.

### Frontend follow-up
Render an **Edit** button on rows where `canEdit` (Option A) — or fetch `reviews/me` on load
(Option B) — and reuse `ReviewFormDialog` in edit mode calling `ProductService.updateReview`. Roughly
half a day once the contract lands.

---

## Notes
- All three changes are backward-compatible: #1 is a new route, #2 and #3 (Option A) add a field,
  #3 (Option B) is a new route.
- No new auth surface — #1 reuses `RequireVendor`; #2 touches a DTO only; #3 reads `ICurrentUserService`.
- After #1 ships, `ProductSummary.Status` (if added) lets the frontend enable the
  Live/Unavailable/Archived status transitions already specified in PDR §2.11.
