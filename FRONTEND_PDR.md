# TradeNaija — Frontend Product Requirements Document (PDR)

> **Source of truth:** the implemented FifeN backend (.NET 9). This document maps every page
> the frontend must build to the **exact** API contract it consumes. All routes are versioned
> under `api/v1/...`. Request/response examples are literal JSON shapes derived from the backend
> DTOs and controllers — implement against these with zero guessing.
>
> **Base URL:** `https://<host>/api/v1`
> **Content type:** `application/json` (image upload is `multipart/form-data`)
> **Auth scheme:** `Authorization: Bearer <accessToken>`
>
> ### Differences from the generic template
> The template references `/api/users/login`, `GetMe`, and HATEOAS links. FifeN does **not** use
> those. The real equivalents:
> - **There are no passwords and no `/login`.** Auth is **phone + OTP**: `POST /auth/otp/request`
>   then `POST /auth/otp/verify`. Verify both authenticates and returns the session.
> - **There is no `GetMe` endpoint.** The authenticated identity is returned inline as
>   `AuthResponse.user` (`{ id, displayName, isVendor, isAdmin }`) by `otp/verify` and `refresh`.
>   Persist this object alongside the tokens.
> - **There are no HATEOAS `_links`.** Conditional rendering is driven by the **JWT claims** and the
>   **boolean flags on DTOs** (`isVendor`, `isAdmin`, `vendorVerified`, lead/listing status, etc.).
>   See §3.

---

## 1. Authentication Flow

### 1.1 Concepts

| Item | Value |
|---|---|
| Credential | Nigerian phone number (+234) + 6-digit OTP. No password. |
| Account creation | Implicit — first successful `otp/verify` for an unknown number creates a **buyer** account. |
| Access token | JWT, **30-minute** lifetime (`Jwt:ExpiryMinutes`). Sent as `Authorization: Bearer`. |
| Refresh token | Opaque string, rotating + single-use, 7-day max. Store it; exchange at `auth/refresh`. |
| Admin MFA | MFA-enrolled admins must ALSO send `totpCode` at verify. Buyers/vendors never need it. |
| Identity payload | `AuthResponse.user` — store it; it replaces a `GetMe` call. |

### 1.2 Request an OTP

`POST /api/v1/auth/otp/request` — `AllowAnonymous`, rate-limited.

Request:
```json
{ "phoneNumber": "08012345678" }
```
Response: `200 OK`, empty body. (Nigerian numbers are normalized to `+234` server-side; `08012345678`, `+2348012345678`, and `2348012345678` are all accepted.)

Rate-limit errors (`429`, RFC 7807) — see §1.7:
```json
{
  "type": "https://httpstatuses.io/429",
  "title": "Too many requests",
  "status": 429,
  "detail": "Too many codes requested. Try again later (limit 3 per 15 minutes).",
  "traceId": "00-abc...-01"
}
```

### 1.3 Verify OTP (authenticate / sign up)

`POST /api/v1/auth/otp/verify` — `AllowAnonymous`.

Request (buyer/vendor):
```json
{ "phoneNumber": "08012345678", "code": "153343" }
```
Request (MFA-enrolled admin — `totpCode` from the authenticator app is mandatory):
```json
{ "phoneNumber": "08032000000", "code": "153343", "totpCode": "094211" }
```

Response `200 OK` (`AuthResponse`):
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "oh9D15USM1bT6vJuL4g0PKfudBdFUD4+2rbz/VN/dP84g5MbUu/QWHdhp4I/lvQ1idmyHAoTR/dgjoff0IHN6Q==",
  "expiresAtUtc": "2026-06-11T12:30:00+00:00",
  "user": {
    "id": "019eb253-1d16-77a9-8f5a-c181b5603e0e",
    "displayName": "AlHusna Stores",
    "isVendor": true,
    "isAdmin": false
  }
}
```

**Frontend after verify:**
1. Store `accessToken`, `refreshToken`, `expiresAtUtc`, and the `user` object (memory + a secure
   persistent store; see §1.6 security note).
2. Use `user.isVendor` / `user.isAdmin` to build the navigation and route guards (§3).
3. No follow-up `GetMe` call is needed — `user` is the profile summary.

**MFA-required signal** — if an admin omits `totpCode`, verify returns `401` with the distinct title
`"MFA required"`. The OTP challenge is NOT consumed; prompt for the authenticator code and resubmit
the same `code` plus `totpCode`:
```json
{ "type": "https://httpstatuses.io/401", "title": "MFA required", "status": 401,
  "detail": "An authenticator code is required. Resubmit with both the OTP and your authenticator code." }
```

### 1.4 Refresh flow (401 interceptor)

`POST /api/v1/auth/refresh` — `AllowAnonymous`.

Request:
```json
{ "refreshToken": "oh9D15USM1bT6vJuL4g0PKfudBdFUD4+2rbz/VN..." }
```
Response `200 OK`: identical `AuthResponse` shape as §1.3 (new access token **and** a new refresh
token — the old one is now revoked; **always replace your stored refresh token**).

**HTTP-client interceptor algorithm:**
```
on response 401 (and request was NOT itself /auth/refresh or /auth/otp/*):
  if a refresh is already in flight → queue this request, await its result
  else:
    call POST /auth/refresh { refreshToken }
    on 200 → store new tokens; retry the original request once with the new access token
    on 401/4xx → refresh token is dead → run logout (§1.5) and redirect to /login
```
Guard against infinite loops: retry the original request **at most once** after a successful refresh.
Refresh is single-use, so serialize concurrent refreshes behind one shared promise.

### 1.5 Logout

`POST /api/v1/auth/logout` — requires `Authorization: Bearer`.

Request:
```json
{ "refreshToken": "oh9D15USM1bT6vJuL4g0PKfudBdFUD4+2rbz/VN..." }
```
Response `204 No Content`. Frontend: clear `accessToken`, `refreshToken`, `user`, and all cached
authed data, then redirect to `/login`. (Revokes the refresh token server-side; the access token
remains valid until its 30-min expiry, so always clear it client-side too.)

### 1.6 Recovery (secondary) phone — optional, authenticated

| Step | Endpoint | Request | Response |
|---|---|---|---|
| Send OTP to new number | `POST /auth/secondary-phone` | `{ "phoneNumber": "08099998888" }` | `202 Accepted` |
| Confirm + bind | `POST /auth/secondary-phone/verify` | `{ "phoneNumber": "08099998888", "code": "445566" }` | `204 No Content` |

`409 Conflict` if the number is already in use.

### 1.7 Admin MFA enrollment — admin role only

| Step | Endpoint | Request | Response |
|---|---|---|---|
| Begin enrollment | `POST /auth/mfa/enroll` | _(none)_ | `200` → `{ "otpAuthUri": "otpauth://totp/TradeNaija:..." }` (render as QR) |
| Activate | `POST /auth/mfa/verify` | `{ "code": "094211" }` | `204 No Content` |

These two authorize on the **Admin role alone** (not the MFA gate) so an admin can bootstrap MFA.
Once enrolled, all subsequent admin logins require `totpCode` (§1.3), and admin pages require the
`amr=mfa` claim (§3.1).

> **Security note:** the access token is short-lived; keep it in memory. The refresh token is
> long-lived — store it in the most secure mechanism your platform allows (httpOnly cookie if you
> add a BFF, else secure storage). Never log tokens.

---

## 2. Pages

Each page lists its endpoints with literal JSON, UI layout, actions, validation (mirroring the
backend FluentValidation rules), and error handling. All error bodies follow the §5 ProblemDetails
shape.

### 2.1 Login / OTP page  *(public)*

- **Endpoints:** `POST /auth/otp/request`, `POST /auth/otp/verify` (§1.2–1.3).
- **Layout:** two-step. Step 1: single phone input + "Send code". Step 2: 6-digit code input +
  "Verify"; a conditional `totpCode` input that appears only after a `401 "MFA required"`.
- **Actions:** Send code → `otp/request`; Verify → `otp/verify` (store session, redirect by role).
- **Validation (client, mirrors backend):** phone must be a valid Nigerian mobile (`+234` / `0…`,
  11 digits local). `code` is exactly 6 digits.
- **Errors:** `429` → "Too many codes requested…" (show `detail`, disable resend with a countdown);
  `401 Unauthorized` → "Invalid phone number or code."; `410 Gone` → "This code has expired. Request
  a new one." (re-enable Send code); `423 Locked` → "Code locked after too many attempts. Request a
  new one."; `401 MFA required` → reveal the authenticator-code field.

### 2.2 Discovery / Browse + Search  *(public)*

- **Endpoint:** `GET /api/v1/products` (paged search). Query params (`ProductQuery`):

  | Param | Type | Default | Notes |
  |---|---|---|---|
  | `q` | string | — | full-text search |
  | `categoryId` | guid | — | |
  | `state` | enum `NigerianState` | — | e.g. `Lagos`, `Abuja`, `Rivers` (37 values incl. FCT) |
  | `city` | string | — | free text |
  | `minPrice` / `maxPrice` | decimal | — | NGN |
  | `condition` | enum | — | `New` \| `Used` \| `Sealed` |
  | `sort` | string | `recent` | one of `recent`, `price_asc`, `price_desc`, `rating` |
  | `page` | int | 1 | |
  | `pageSize` | int | 20 | |

  Example: `GET /api/v1/products?q=iphone&state=Lagos&minPrice=50000&sort=price_asc&page=1&pageSize=20`

  Response `200 OK` (`PagedResponse<ProductSummary>` — see §4):
```json
{
  "items": [
    {
      "id": "019eb300-0000-7000-8000-000000000001",
      "title": "iPhone 13 Pro 256GB",
      "priceAmount": 520000.00,
      "currency": "NGN",
      "priceType": "Negotiable",
      "coverUrl": "https://res.cloudinary.com/.../cover.jpg",
      "city": "Ikeja",
      "state": "Lagos",
      "vendorId": "019eb253-1d16-77a9-8f5a-c181b5603e0e",
      "vendorName": "AlHusna Stores",
      "vendorVerified": true,
      "averageRating": 4.6
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 137
}
```
- **Layout:** filter sidebar/drawer (search box, category select, state+city, price min/max,
  condition, sort) + responsive product grid. Card shows `coverUrl`, `title`, formatted
  `₦priceAmount` (or "Contact for price" when `priceType === "ContactForPrice"`), `city, state`,
  `vendorName` with a **Verified** badge when `vendorVerified`, and a rating stars from
  `averageRating`. Pagination controls (§4).
- **Actions:** card click → Product Detail (2.3). Filters update the query string and refetch.
- **Errors:** generic `500` banner; empty `items` → "No listings match your filters."

Companion feeds (same `PagedResponse<ProductSummary>` shape):
- `GET /api/v1/products/new?categoryId=&page=1&pageSize=20` — "New this week" rail.
- `GET /api/v1/vendors/{id}/products?page=1&pageSize=20` — a vendor's public shop grid.

### 2.3 Product Detail  *(public; actions need auth)*

- **Endpoint:** `GET /api/v1/products/{id}` → `ProductDetail`. (Increments view count; hidden/removed
  listings return `404`.)
```json
{
  "id": "019eb300-0000-7000-8000-000000000001",
  "title": "iPhone 13 Pro 256GB",
  "description": "Clean UK-used, no scratches, battery 92%.",
  "priceAmount": 520000.00,
  "currency": "NGN",
  "priceType": "Negotiable",
  "condition": "Used",
  "categoryName": "Phones & Tablets",
  "state": "Lagos",
  "city": "Ikeja",
  "imageUrls": ["https://res.cloudinary.com/.../1.jpg", "https://res.cloudinary.com/.../2.jpg"],
  "vendorId": "019eb253-1d16-77a9-8f5a-c181b5603e0e",
  "vendorName": "AlHusna Stores",
  "vendorVerified": true,
  "averageRating": 4.6,
  "reviewCount": 18,
  "status": "Live",
  "createdAtUtc": "2026-06-01T09:15:00+00:00"
}
```
- **Layout:** image gallery (`imageUrls`), title, price/price-type, condition, category, location,
  vendor block (name + Verified badge, link to shop), rating + `reviewCount`, description. Below:
  reviews list (2.6) and a **Report** affordance (2.7).
- **Primary action — "I'm interested" (WhatsApp hand-off):** see 2.4. Visible to any authenticated
  user; if anonymous, prompt login first.
- **Errors:** `404` → "Listing not found or no longer available" (redirect to browse).

### 2.4 Express Interest (lead → WhatsApp)  *(authenticated)*

- **Endpoint:** `POST /api/v1/products/{id}/interest` → `InterestResponse`.

  Request (`ExpressInterestRequest` — both fields optional):
```json
{ "message": "Is the price negotiable? I can pick up today.", "offerPrice": 480000 }
```
  Response `201 Created`:
```json
{
  "interactionId": "019eb400-1111-7000-8000-000000000010",
  "whatsAppUrl": "https://wa.me/2348132972088?text=Hi%20AlHusna%20Stores%2C%20I%27m%20interested%20in%20iPhone%2013%20Pro..."
}
```
- **Action:** on success, open `whatsAppUrl` in a new tab/window (the lead is already recorded
  server-side before hand-off). Show a confirmation toast.
- **Validation (mirrors backend):** `message` ≤ 500 chars; `offerPrice` must be `> 0` when provided.
- **Errors:** `401` → prompt login; `404` → listing gone; `422` business rule (e.g. own listing).

### 2.5 Write / Edit Review  *(authenticated, interaction-gated)*

- **Create:** `POST /api/v1/products/{id}/reviews` → `201` `ReviewResponse`.
  ```json
  { "rating": 5, "text": "Exactly as described, fast WhatsApp response." }
  ```
- **Edit (author only, within 30 days):** `PUT /api/v1/reviews/{id}` → `200` `ReviewResponse`.
  Same body shape as create.

  `ReviewResponse`:
```json
{
  "id": "019eb500-2222-7000-8000-000000000020",
  "authorDisplayName": "Bola A.",
  "rating": 5,
  "text": "Exactly as described, fast WhatsApp response.",
  "contactedVendor": true,
  "createdAtUtc": "2026-06-05T14:20:00+00:00"
}
```
- **Layout:** star picker (1–5) + optional textarea. Show the review form only when the buyer is
  eligible (has contacted the vendor ≥48h ago, no existing review). Since there's no "can-I-review"
  endpoint, attempt and handle the gate via the error response below.
- **Validation:** `rating` integer 1–5 (required); `text` ≤ 500 chars (optional).
- **Errors:** `422 Business rule violation` → show `detail` (e.g. "You can review a vendor only after
  contacting them, and after a 48-hour cooldown."); `409 Conflict` → "You've already reviewed this
  listing." (switch UI to edit mode); `403 Forbidden` on edit by non-author; `422` past the 30-day
  edit window.

### 2.6 Product Reviews list  *(public)*

- **Endpoint:** `GET /api/v1/products/{id}/reviews?page=1&pageSize=20` →
  `PagedResponse<ReviewResponse>`.
```json
{
  "items": [
    { "id": "019eb500-2222-7000-8000-000000000020", "authorDisplayName": "Bola A.",
      "rating": 5, "text": "Great seller.", "contactedVendor": true,
      "createdAtUtc": "2026-06-05T14:20:00+00:00" }
  ],
  "page": 1, "pageSize": 20, "total": 18
}
```
- **Layout:** list of cards (author, stars, text, date). Show an **"Contacted vendor"** honesty
  badge when `contactedVendor === true`. Paginate (§4).

### 2.7 Report content  *(authenticated)*

- **Endpoint:** `POST /api/v1/reports` → `201` `ReportResponse`.
  Request (`CreateReportRequest`):
```json
{ "targetType": "Listing", "targetId": "019eb300-0000-7000-8000-000000000001",
  "reason": "Counterfeit", "note": "Logo doesn't match the official brand." }
```
  Response:
```json
{
  "id": "019eb600-3333-7000-8000-000000000030",
  "targetType": "Listing",
  "targetId": "019eb300-0000-7000-8000-000000000001",
  "reason": "Counterfeit",
  "note": "Logo doesn't match the official brand.",
  "status": "Open",
  "resolvedByUserId": null,
  "resolvedAtUtc": null,
  "createdAtUtc": "2026-06-11T10:05:00+00:00"
}
```
- **Layout:** modal with `targetType` (`Listing` \| `Vendor` \| `Review` — usually pre-set by
  context), a `reason` dropdown, and an optional note. `targetId` comes from the page context.
- **Validation:** `targetType`, `reason` must be valid enum values; `targetId` required (non-empty
  guid); `note` ≤ 500 chars.
  - `reason` values: `Prohibited`, `Counterfeit`, `Scam`, `Offensive`, `Spam`, `WrongCategory`, `Other`.
- **Errors:** `401` prompt login; `409` "You've already reported this." (if the backend dedupes).

### 2.8 Notification Centre  *(authenticated)*

- **List:** `GET /api/v1/notifications?page=1&pageSize=20` → `PagedResponse<NotificationResponse>`.
```json
{
  "items": [
    { "id": "019eb700-4444-7000-8000-000000000040", "type": "NewLead",
      "title": "New lead on iPhone 13 Pro", "body": "Bola A. is interested.",
      "isRead": false, "createdAtUtc": "2026-06-11T09:40:00+00:00" }
  ],
  "page": 1, "pageSize": 20, "total": 5
}
```
- **Unread badge:** `GET /api/v1/notifications/unread-count` → `{ "count": 3 }`.
- **Mark one read:** `PATCH /api/v1/notifications/{id}/read` → `204`.
- **Mark all read:** `PATCH /api/v1/notifications/read-all` → `204`.
- **Layout:** bell icon with the unread `count`; dropdown/page listing items newest-first, unread
  visually distinct, each with a "mark read" affordance and a "Mark all read" button. Route the click
  by `type` (e.g. `NewLead` → vendor leads; `NewReview` → product detail).
- **`type` values:** `VendorApproved`, `VendorRejected`, `ListingApproved`, `ListingTakenDown`,
  `NewReview`, `NewLead`, `ReviewNudge`, `NewInCategory`, `Security`.

### 2.9 Become a Vendor (onboarding)  *(authenticated buyer)*

- **Submit application:** `POST /api/v1/vendor/requests` → `201` `VendorRequestResponse`.
  Request (`CreateVendorRequestRequest`):
```json
{ "businessName": "AlHusna Stores", "whatsAppNumber": "08132972088",
  "method": "Nin", "identifierToken": "12345678901" }
```
  Response:
```json
{ "id": "019eb800-5555-7000-8000-000000000050", "status": "Pending",
  "nameMatch": false, "createdAtUtc": "2026-06-11T10:10:00+00:00" }
```
- **Check my latest request:** `GET /api/v1/vendor/requests/me` → same `VendorRequestResponse`.
  Use it to render status (`Pending` \| `Approved` \| `Rejected`).
- **Layout:** form — business name, WhatsApp number, verification method radio (`Nin` \| `Cac`),
  identity number. After submit, show a status panel that polls/reads `vendor/requests/me`.
- **Validation (mirrors backend):** `businessName` required, 2–160 chars; `whatsAppNumber` required;
  `method` must be `Nin` or `Cac`; `identifierToken` required, ≥7 chars.
- **Security note:** `identifierToken` (raw NIN/CAC) is sent to KYC and never persisted — do not echo
  or cache it client-side.
- **Errors:** `409` "You already have a pending/approved request."; `422` business rule.

### 2.10 Vendor — My Profile  *(Vendor role)*

- **Endpoint:** `PATCH /api/v1/vendor/profile` → `200`.
  ```json
  { "whatsAppNumber": "08132972088" }
  ```
- **Validation:** `whatsAppNumber` required.
- **Note:** authorizes on the Vendor **role alone** (a vendor may correct their profile even if
  verification was revoked). Errors: `401`/`403` if not a vendor.

### 2.11 Vendor — My Listings (CRUD)  *(`RequireVendor`: Vendor role + verified)*

- **Create:** `POST /api/v1/products` → `201` `ProductDetail`.
  Request (`CreateProductRequest`):
```json
{
  "title": "iPhone 13 Pro 256GB",
  "description": "Clean UK-used, no scratches, battery 92%.",
  "priceAmount": 520000,
  "priceType": "Negotiable",
  "condition": "Used",
  "categoryId": "019eb100-0000-7000-8000-0000000000aa",
  "state": "Lagos",
  "city": "Ikeja",
  "attributes": { "Storage": "256GB", "Color": "Graphite" }
}
```
- **Update (owner):** `PUT /api/v1/products/{id}` → `200` `ProductDetail`. Same fields as create
  **minus `attributes`** (`UpdateProductRequest` manages attributes separately).
- **Change status (owner):** `PATCH /api/v1/products/{id}/status` → `200`.
  ```json
  { "status": "Unavailable" }
  ```
  `status` ∈ `Live` \| `Unavailable` \| `Archived` (vendor-settable; `Removed` is admin-only).
- **Delete / archive (owner or admin):** `DELETE /api/v1/products/{id}` → `204`.
- **Upload images (owner):** `POST /api/v1/products/{id}/images` — **`multipart/form-data`**, field
  name `files`, 1–5 files, JPEG/PNG/WebP, ≤5MB each (request cap 30MB). → `201` with a string array:
  ```json
  ["https://res.cloudinary.com/.../1.jpg", "https://res.cloudinary.com/.../2.jpg"]
  ```
- **Layout:** a "My Listings" table (columns: cover thumb, Title, ₦Price, Status badge, Category,
  Created) with row actions. A create/edit form with: title, description, price + price-type select,
  condition select, category select (from `GET /categories`), state select + city, attributes
  key/value editor (create only), and an image dropzone.
- **Validation (mirrors backend):** `title` required ≤80; `description` required ≤1000;
  `priceAmount > 0` unless `priceType === "ContactForPrice"`; `priceType`/`condition`/`state` valid
  enums; `categoryId` required; `city` required ≤80.
  - `priceType`: `Fixed` \| `Negotiable` \| `ContactForPrice`. `condition`: `New` \| `Used` \| `Sealed`.
- **Errors:** `403`/`401` if not a verified vendor (`RequireVendor`); `404` editing a non-owned
  listing; `413`/`415` on image upload size/type; `422` business rule (e.g. probation listing limits).

### 2.12 Vendor — Leads dashboard  *(`RequireVendor`)*

- **List:** `GET /api/v1/vendor/leads?status=New&page=1&pageSize=20` →
  `PagedResponse<LeadResponse>`. `status` optional (`New` \| `Viewed` \| `Closed`).
```json
{
  "items": [
    {
      "id": "019eb400-1111-7000-8000-000000000010",
      "productId": "019eb300-0000-7000-8000-000000000001",
      "productTitle": "iPhone 13 Pro 256GB",
      "buyerDisplayName": "Bola A.",
      "message": "Is the price negotiable?",
      "offerPrice": 480000.00,
      "status": "New",
      "isCrossDiscovery": true,
      "createdAtUtc": "2026-06-11T09:40:00+00:00"
    }
  ],
  "page": 1, "pageSize": 20, "total": 12
}
```
- **Update lead status (owner):** `PATCH /api/v1/vendor/leads/{id}` → `200`.
  ```json
  { "status": "Viewed" }
  ```
  `status` ∈ `New` \| `Viewed` \| `Closed`.
- **Layout:** table (Product, Buyer, Message, Offer ₦, Status, Cross-discovery flag, Date) with a
  status filter and a per-row status dropdown. Highlight `isCrossDiscovery` leads (north-star metric).
- **Errors:** `403`/`401` if not verified vendor; `404` on a non-owned lead.

### 2.13 Admin — Dashboard  *(`RequireAdmin`: Admin role + `amr=mfa`)*

- **Endpoint:** `GET /api/v1/admin/dashboard` → `DashboardResponse`.
```json
{
  "registeredUsers": 4210,
  "activeUsers30d": 1188,
  "activeListings": 932,
  "otpSuccessRate": 0.97,
  "interactions30d": 3540,
  "crossDiscoveryRate": 0.41,
  "topCategories": [ { "name": "Phones & Tablets", "count": 210 }, { "name": "Fashion", "count": 168 } ],
  "topLocations": [ { "name": "Lagos", "count": 380 }, { "name": "Abuja", "count": 152 } ],
  "pendingApprovals": 7,
  "openReports": 3
}
```
- **Layout:** KPI cards (Registered, Active 30d, Active listings, OTP success %, Interactions 30d,
  **Cross-discovery rate — headline**), two top-N tables (categories, locations), and alert badges
  for `pendingApprovals` / `openReports` linking to 2.14 / 2.16.
- **Errors:** `401` if no token; `403` if Admin without `amr=mfa` (force MFA enrollment, §1.7).

### 2.14 Admin — Vendor moderation  *(`RequireAdmin`)*

- **Queue:** `GET /api/v1/admin/vendor-requests?status=Pending&page=1&pageSize=20` →
  `IReadOnlyList<VendorRequestQueueItem>` (a plain JSON array, **not** a `PagedResponse`):
```json
[
  {
    "id": "019eb800-5555-7000-8000-000000000050",
    "userId": "019eb253-1d16-77a9-8f5a-c181b5603e0e",
    "businessName": "AlHusna Stores",
    "method": "Nin",
    "verificationStatus": "Verified",
    "nameMatch": true,
    "createdAtUtc": "2026-06-11T10:10:00+00:00"
  }
]
```
  `status` ∈ `Pending` \| `Approved` \| `Rejected` (default `Pending`).
- **Approve:** `POST /api/v1/admin/vendor-requests/{id}/approve` → `200`.
- **Reject:** `POST /api/v1/admin/vendor-requests/{id}/reject` → `200`.
  ```json
  { "reason": "NameMismatch" }
  ```
  `reason` ∈ `NameMismatch` \| `UnreadableDocument` \| `IneligibleBusiness` \| `SuspectedFraud` \| `Other`.
- **Suspend vendor:** `POST /api/v1/admin/vendors/{id}/suspend` → `200`. `{ "reason": "Repeated counterfeit listings" }`
- **Reinstate vendor:** `POST /api/v1/admin/vendors/{id}/reinstate` → `200`.
- **Layout:** queue table with status filter; row shows business name, method, KYC
  `verificationStatus`, and a `nameMatch` indicator; Approve / Reject (reason modal) actions.
  Separate vendor management view for Suspend/Reinstate.
- **Errors:** `409` if already actioned; `404` unknown request/vendor.

### 2.15 Admin — Listing moderation  *(`RequireAdmin`)*

- **Endpoint:** `POST /api/v1/admin/products/{id}/moderate` → `200`.
  ```json
  { "status": "Removed", "reason": "Prohibited item." }
  ```
  `status` is a `ListingStatus` (admin uses `Removed` for takedown, or `Live` to restore). `reason`
  is recorded in the audit log.

### 2.16 Admin — Report moderation  *(`RequireAdmin`)*

- **Queue:** `GET /api/v1/admin/reports?status=Open&page=1&pageSize=20` →
  `PagedResponse<ReportResponse>` (shape per 2.7, paged). `status` ∈ `Open` \| `Actioned` \| `Dismissed`.
- **Resolve:** `PATCH /api/v1/admin/reports/{id}` → `200`.
  ```json
  { "status": "Actioned", "note": "Listing removed, vendor warned." }
  ```
- **Layout:** queue table (target type/id link, reason, note, status, date) with a resolve action
  (Actioned / Dismissed + note). `note` ≤ 500 chars.

### 2.17 Admin — Review moderation  *(`RequireAdmin`)*

- **Endpoint:** `PATCH /api/v1/admin/reviews/{id}` → `200`.
  ```json
  { "status": "Hidden", "reason": "Abusive language." }
  ```
  `status` ∈ `Visible` \| `Hidden` \| `Removed`. `reason` required (≤ per validator), audited.

---

## 3. Conditional Rendering

There are **no HATEOAS `_links`** in FifeN responses. Drive visibility off **(a) the JWT claims /
the stored `user` summary** and **(b) the boolean/enum flags carried on each DTO**.

### 3.1 Authorization policies (backend) → frontend gates

| Backend policy | Backend requirement | Frontend gate |
|---|---|---|
| `[Authorize]` | any authenticated user | token present |
| `RequireVendor` | `Vendor` role **AND** verified vendor | `user.isVendor === true` (+ handle `403` if verification revoked) |
| `RequireAdmin` | `Admin` role **AND** `amr=mfa` claim | `user.isAdmin === true` **AND** MFA satisfied |
| `RequireOwner` | `is_owner` claim `true` | decode `is_owner` from the JWT |

JWT claims emitted by `TokenService`: `is_owner`, `is_vendor`, `is_admin`, role, `amr=mfa`
(present only when an enrolled admin's second factor is satisfied). Decode the access token (it's a
standard JWT) to read `amr` and `is_owner`; for the common role checks just use the stored `user`
flags. **Never trust client flags for security** — they only hide UI; the API re-checks every call.

### 3.2 Navigation visibility

| Nav item / route | Show when |
|---|---|
| Browse, Product detail, Vendor shop, public reviews | always (public) |
| Notifications bell, Logout, "Become a vendor" | authenticated |
| "My Listings", "Leads", vendor profile | `user.isVendor` |
| "Admin" section (dashboard, vendor/report/review/listing moderation) | `user.isAdmin` **and** `amr=mfa` |
| If `user.isAdmin` but MFA not yet satisfied | redirect to MFA enroll (§1.7) instead of admin pages |

### 3.3 Action-button visibility (from DTO flags, not links)

| Button | Show when |
|---|---|
| "Verified" badge (listing/vendor) | `vendorVerified === true` / `verified === true` |
| "I'm interested" | authenticated **and** viewer is not the listing's vendor |
| "Edit review" | the review's author (compare `authorDisplayName`/your id) **and** within 30 days |
| "Contacted vendor" honesty badge on a review | `contactedVendor === true` |
| Lead status dropdown / cross-discovery highlight | leads page; `isCrossDiscovery` for the highlight |
| Listing status transitions (Live/Unavailable/Archived) | owner; `status` drives which transitions are valid |
| "Mark all read" | unread `count > 0` |
| Admin "Approve/Reject" | `RequireAdmin` gate **and** queue item `status === "Pending"` |
| Admin "Resolve" report | report `status === "Open"` |

Because the API enforces every rule server-side, a hidden button that gets clicked anyway (deep
link) will return `401`/`403`/`409`/`422` — always handle those per §5 rather than assuming the UI
gate is sufficient.

---

## 4. Paging

### 4.1 Request

Paginated endpoints take `page` and `pageSize` query params (1-based; defaults `page=1`,
`pageSize=20`). Example: `GET /api/v1/products?page=2&pageSize=24`. Discovery search also accepts
the filter params in §2.2.

### 4.2 Response shape — `PagedResponse<T>`

```json
{ "items": [ /* T[] */ ], "page": 1, "pageSize": 20, "total": 137 }
```

> **Note the field names.** The template mentions `totalCount` / `totalPages`; FifeN returns
> `total` (the total item count) and does **not** send a page count. Compute it on the client:
> `totalPages = Math.ceil(total / pageSize)`.

**Endpoints returning `PagedResponse<T>`:** `GET /products`, `GET /products/new`,
`GET /vendors/{id}/products`, `GET /products/{id}/reviews`, `GET /vendor/leads`,
`GET /notifications`, `GET /admin/reports`.

**Exception:** `GET /admin/vendor-requests` returns a **plain array** (`VendorRequestQueueItem[]`),
not a `PagedResponse`, even though it accepts `page`/`pageSize`. Don't read `.items`/`.total` off it.

### 4.3 UI

- Page controls: Prev/Next (disable Prev at `page === 1`, disable Next when
  `page * pageSize >= total`), plus numbered pages computed from `totalPages`.
- Page-size selector (e.g. 10 / 20 / 50) → set `pageSize` and reset to `page=1`.
- Show "Showing {(page-1)*pageSize + 1}–{min(page*pageSize, total)} of {total}".
- Empty state when `total === 0`.

---

## 5. Error Handling (RFC 7807 ProblemDetails)

Every backend error is a `application/problem+json` body of this shape (validation errors add an
`errors` map):

```json
{
  "type": "https://httpstatuses.io/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "traceId": "00-9f2c...-01",
  "errors": {
    "PhoneNumber": ["A valid Nigerian (+234) mobile number is required."],
    "Title": ["Title cannot exceed 80 characters."]
  }
}
```

### 5.1 Status → frontend handling

| Status | Title (examples) | Frontend |
|---|---|---|
| `400` | Validation failed | Map `errors[field]` to inline field messages; fall back to `detail`. |
| `401` | Unauthorized / MFA required | If `MFA required` → prompt TOTP (§1.3). Else run the refresh interceptor (§1.4); if that fails, logout. |
| `403` | Forbidden | "You don't have permission to do that." (and re-evaluate nav gates). |
| `404` | Resource not found | "Not found / no longer available." |
| `409` | Conflict | Show `detail` (already exists / already actioned); often switch UI mode. |
| `410` | Gone | OTP expired → re-enable "Send code". |
| `413` | Payload too large | "Image too large (max 5MB each)." |
| `415` | Unsupported media type | "Use JPEG, PNG, or WebP." |
| `422` | Business rule violation | Show `detail` verbatim (review cooldown, listing limits, etc.). |
| `423` | Locked | OTP locked → "Request a new code." |
| `429` | Too many requests | Show `detail`; back off / disable with countdown. |
| `500` | Server error | Generic banner + `traceId` for support. |

### 5.2 Implementation notes

- Parse the body as ProblemDetails on any non-2xx; never assume a bare string.
- Always surface `traceId` in a copyable form on `500`s (it ties to server logs).
- The `errors` extension is present only on `400` validation failures; key = the DTO property name
  (PascalCase, e.g. `WhatsAppNumber`, `PriceAmount`). Map these to your form fields.

---

## Appendix A — Endpoint index

| Method | Path | Auth | Body / Query | Returns |
|---|---|---|---|---|
| POST | `/auth/otp/request` | anon | `{phoneNumber}` | 200 |
| POST | `/auth/otp/verify` | anon | `{phoneNumber,code,totpCode?}` | `AuthResponse` |
| POST | `/auth/refresh` | anon | `{refreshToken}` | `AuthResponse` |
| POST | `/auth/logout` | bearer | `{refreshToken}` | 204 |
| POST | `/auth/secondary-phone` | bearer | `{phoneNumber}` | 202 |
| POST | `/auth/secondary-phone/verify` | bearer | `{phoneNumber,code}` | 204 |
| POST | `/auth/mfa/enroll` | Admin | — | `{otpAuthUri}` |
| POST | `/auth/mfa/verify` | Admin | `{code}` | 204 |
| GET | `/products` | anon | `ProductQuery` | `PagedResponse<ProductSummary>` |
| GET | `/products/new` | anon | `categoryId?,page,pageSize` | `PagedResponse<ProductSummary>` |
| GET | `/products/{id}` | anon | — | `ProductDetail` |
| POST | `/products` | RequireVendor | `CreateProductRequest` | `ProductDetail` |
| PUT | `/products/{id}` | RequireVendor | `UpdateProductRequest` | `ProductDetail` |
| PATCH | `/products/{id}/status` | RequireVendor | `{status}` | 200 |
| DELETE | `/products/{id}` | bearer (owner/admin) | — | 204 |
| POST | `/products/{id}/images` | RequireVendor | multipart `files` | `string[]` |
| GET | `/categories` | anon | — | `CategoryResponse[]` |
| GET | `/vendors/{id}` | anon | — | `VendorPublicResponse` |
| GET | `/vendors/{id}/products` | anon | `page,pageSize` | `PagedResponse<ProductSummary>` |
| POST | `/vendor/requests` | bearer | `CreateVendorRequestRequest` | `VendorRequestResponse` |
| GET | `/vendor/requests/me` | bearer | — | `VendorRequestResponse` |
| PATCH | `/vendor/profile` | Vendor | `{whatsAppNumber}` | 200 |
| POST | `/products/{id}/interest` | bearer | `ExpressInterestRequest` | `InterestResponse` |
| GET | `/vendor/leads` | RequireVendor | `status?,page,pageSize` | `PagedResponse<LeadResponse>` |
| PATCH | `/vendor/leads/{id}` | RequireVendor | `{status}` | 200 |
| POST | `/products/{id}/reviews` | bearer | `CreateReviewRequest` | `ReviewResponse` |
| PUT | `/reviews/{id}` | bearer | `UpdateReviewRequest` | `ReviewResponse` |
| GET | `/products/{id}/reviews` | anon | `page,pageSize` | `PagedResponse<ReviewResponse>` |
| POST | `/reports` | bearer | `CreateReportRequest` | `ReportResponse` |
| GET | `/notifications` | bearer | `page,pageSize` | `PagedResponse<NotificationResponse>` |
| GET | `/notifications/unread-count` | bearer | — | `{count}` |
| PATCH | `/notifications/{id}/read` | bearer | — | 204 |
| PATCH | `/notifications/read-all` | bearer | — | 204 |
| GET | `/admin/dashboard` | RequireAdmin | — | `DashboardResponse` |
| GET | `/admin/vendor-requests` | RequireAdmin | `status,page,pageSize` | `VendorRequestQueueItem[]` |
| POST | `/admin/vendor-requests/{id}/approve` | RequireAdmin | — | 200 |
| POST | `/admin/vendor-requests/{id}/reject` | RequireAdmin | `{reason}` | 200 |
| POST | `/admin/vendors/{id}/suspend` | RequireAdmin | `{reason}` | 200 |
| POST | `/admin/vendors/{id}/reinstate` | RequireAdmin | — | 200 |
| POST | `/admin/products/{id}/moderate` | RequireAdmin | `{status,reason}` | 200 |
| GET | `/admin/reports` | RequireAdmin | `status?,page,pageSize` | `PagedResponse<ReportResponse>` |
| PATCH | `/admin/reports/{id}` | RequireAdmin | `{status,note}` | 200 |
| PATCH | `/admin/reviews/{id}` | RequireAdmin | `{status,reason}` | 200 |

## Appendix B — Enum reference (JSON values are the string names)

- **PriceType:** `Fixed`, `Negotiable`, `ContactForPrice`
- **ProductCondition:** `New`, `Used`, `Sealed`
- **ListingStatus:** `Live`, `Unavailable`, `Archived`, `Removed`
- **LeadStatus:** `New`, `Viewed`, `Closed`
- **ReviewStatus:** `Visible`, `Hidden`, `Removed`
- **ReportTargetType:** `Listing`, `Vendor`, `Review`
- **ReportReason:** `Prohibited`, `Counterfeit`, `Scam`, `Offensive`, `Spam`, `WrongCategory`, `Other`
- **ReportStatus:** `Open`, `Actioned`, `Dismissed`
- **VerificationMethod:** `Nin`, `Cac`
- **VerificationStatus:** `Pending`, `Verified`, `Failed`
- **VendorRequestStatus:** `Pending`, `Approved`, `Rejected`
- **RejectionReason:** `NameMismatch`, `UnreadableDocument`, `IneligibleBusiness`, `SuspectedFraud`, `Other`
- **NotificationType:** `VendorApproved`, `VendorRejected`, `ListingApproved`, `ListingTakenDown`, `NewReview`, `NewLead`, `ReviewNudge`, `NewInCategory`, `Security`
- **NigerianState:** the 36 states + FCT (e.g. `Lagos`, `Abuja`, `Rivers`, `Kano`, …) — fetch the
  full set from your shared enum mirror; the API accepts/returns the string name.

> Enums are persisted and serialized **as their string names** (not integers). Send and expect the
> exact PascalCase names above.
