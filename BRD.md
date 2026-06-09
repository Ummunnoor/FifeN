## Change Summary (v1.0 → v2.0)

| **Area**              | **Change in v2.0**                                                                                        |
|-----------------------|-----------------------------------------------------------------------------------------------------------|
| Reviews & sales       | Adopted interaction-gated reviews; “making sales” objective replaced with a measurable interaction proxy. |
| Data model            | Vendor is now a role on a single User entity (not a separate entity); VendorProfile is a 1:1 extension.   |
| Identity verification | BVN dropped. NIN (individuals) + CAC (businesses) via a licensed KYC API; raw identifiers are not stored. |
| Targets               | 6-month / 3-month / launch targets reconciled into one funnel with explicit definitions of “active.”      |
| Moderation            | Vendor trust-tier graduation model; full audit logging; reversible actions.                               |
| Trust & safety        | Buyer-facing reporting, prohibited-items policy, and NDPA compliance section added.                       |

## Change Summary (v2.0 → v2.1)

| **Area**     | **Change in v2.1**                                                                                                                |
|--------------|-----------------------------------------------------------------------------------------------------------------------------------|
| Strategy     | Added a retention & anti-leakage thesis: reputation as the moat, vendor stickiness, and cross-discovery as the north-star metric. |
| Discovery    | Buyer interest is now captured on-platform as a lead before the WhatsApp handoff; vendors see their leads.                        |
| Growth       | Added a shareable public Vendor Shop Page and a vendor-led pilot launch (new Go-to-Market section).                               |
| Monetization | Confirmed the MVP is free (no listing charges or commission) and added a phased monetization roadmap.                             |
| Metrics      | Cross-discovery added as the headline success metric.                                                                             |

# 1. Executive Summary & Project Overview

**Project Title:** TradeNaija

**Tagline:** Connecting Local Buyers and Sellers

## Vision

To create a trusted digital marketplace that makes buying, selling, and
supporting local businesses simple, safe, and accessible for everyone.

## Purpose

Build a phone-first Progressive Web App (PWA) marketplace for Nigerian
traders, with a strong focus on low-tech and semi-literate users. The
platform facilitates product discovery and connects buyers directly to
vendors via WhatsApp. **There are no in-app payments or delivery
handling in the first 6 months.**

## Trust Model

Because transactions occur off-platform on WhatsApp, the platform cannot
verify a completed sale. Trust is therefore established through three
observable mechanisms rather than transaction data:

1.  Verified vendor identity (NIN/CAC) with a visible “Verified Vendor”
    badge — the primary trust anchor.

2.  Interaction-gated reviews — only buyers who contacted a vendor may
    review.

3.  Buyer-facing reporting of vendors, listings, and reviews.

## Retention & Growth Thesis

Because deals close on WhatsApp, the platform’s defensibility cannot
come from owning the transaction — it comes from being the place users
return to. Disintermediation (a buyer and vendor exchanging numbers and
going direct) is accepted as inherent and is countered, not prevented,
by three mechanisms:

- **Recurring discovery:** buyers return to find new vendors and
  products, not to re-contact someone they already know. Fresh supply is
  surfaced by default.

- **Reputation as the moat:** Verified-Vendor badges and
  interaction-gated reviews live on the platform and do not exist on
  WhatsApp. Buyers return to check a vendor before a significant
  purchase; vendors stay because their reputation and visibility
  compound here.

- **Vendor stickiness:** vendors receive leads and visibility, making
  them the durable side, and they drive buyer acquisition through
  shareable shop pages.

**North-star metric — cross-discovery:** buyers contacting a vendor they
did not arrive with. Raw interaction counts can be inflated by vendors
routing their existing customers through the app; cross-discovery is the
honest signal that a true marketplace is forming.

## Key Objectives

- Gain the trust of vendors and buyers.

- Onboard and support at least 50 active verified vendors within the
  first 6 months (see Section 10 for the definition of “active”).

- Achieve high user adoption through extreme simplicity.

- Create a scalable foundation for future payment and logistics
  features.

## Initial Target Users

Nigerian traders and buyers. Initial traction will come from the
founder’s existing network (clothing, shoes, beddings, beans powder,
liquid soap, toilet wash, etc.).

# 2. Stakeholders & Users

**Key Stakeholders:** Founder, Vendors, Buyers, Development Team.

## User Personas

- **Vendor:** Low-tech trader who uses WhatsApp daily.

- **Buyer:** Everyday Nigerian consumer who prefers simple experiences.

# 3. User Roles & Permissions

Roles are flags on a single User account. Every user is a Buyer by
default; the Vendor and Admin roles unlock additional capabilities.
Login is by phone + OTP.

| **Capability**                               | **Anonymous** | **Buyer**           | **Vendor**           | **Admin**      |
|----------------------------------------------|---------------|---------------------|----------------------|----------------|
| Browse / search / view products              | Yes           | Yes                 | Yes                  | Yes            |
| Tap “Chat on WhatsApp”                       | No (login)    | Yes                 | Yes                  | Yes            |
| Register (phone + OTP)                       | —             | Yes                 | Yes                  | Yes            |
| Apply to become a vendor                     | No            | Yes                 | —                    | —              |
| Create / edit / delete own listings          | No            | No                  | Yes (after approval) | Yes (override) |
| Mark listing sold / unavailable / reactivate | No            | No                  | Yes                  | Yes            |
| View own vendor metrics                      | No            | No                  | Yes                  | Yes            |
| Leave a review / rating                      | No            | Yes (if interacted) | Yes (if interacted)  | Yes            |
| Report listing / vendor / review             | No            | Yes                 | Yes                  | Yes            |
| Approve / reject vendors & first listings    | No            | No                  | No                   | Yes            |
| Moderate content (hide/remove/suspend)       | No            | No                  | No                   | Yes            |
| Access Admin Dashboard                       | No            | No                  | No                   | Yes            |
| Add / remove admins                          | No            | No                  | No                   | Owner only     |

*Admin team: the Founder plus up to two authorized persons. All three
hold a flat Admin role for MVP, but each has an individual login with
mandatory MFA, and every admin action is attributed and audited. One
Owner flag (the Founder) can add or remove admins.*

# 4. Data Model & Core Entities

## 4.1 Entities

| **Entity**        | **Description & key attributes**                                                                                                                                                                                                                                              |
|-------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| User              | Base account. primary_phone (unique, +234), secondary_phone (recovery only, verified), is_vendor, is_admin, is_owner, status, created_at, last_active_at.                                                                                                                     |
| VendorProfile     | 1:1 extension of User. business_name (unique, case-insensitive), whatsapp_number, verification_method (NIN/CAC), verification_status, verified_name, name_match (bool/confidence), kyc_reference, trust_tier (pending/probation/trusted), created_at.                         |
| Product (Listing) | vendor_id, title, description, price, currency (NGN), price_type (fixed/negotiable), category_id, condition, city, state, photos\[\], status (live/unavailable/archived/removed), created_at, updated_at.                                                                     |
| Category          | name, slug, is_active, sort_order. Admin-managed; ships with 5 initial categories.                                                                                                                                                                                            |
| Interaction       | user_id, vendor_id, product_id, timestamp, source, optional buyer message/offer, lead_status (new/viewed/closed), is_cross_discovery. A logged “Contact / I’m interested” event by a logged-in user; surfaced to the vendor as a lead. Powers metrics and review eligibility. |
| Review            | author_user_id, product_id, vendor_id (denormalized), rating (1–5), text (optional), interaction_id, status, created_at, edited_at.                                                                                                                                           |
| Report            | reporter_user_id, target_type (listing/vendor/review), target_id, reason (enum), note, status, created_at.                                                                                                                                                                    |
| AuditLog          | actor_id, action, object_type, object_id, before/after or reason, ip, timestamp. Append-only, immutable.                                                                                                                                                                      |
| OtpRequest        | phone, channel (whatsapp/sms), code_hash, expires_at, attempts, status. Transient; short retention.                                                                                                                                                                           |

## 4.2 Relationships

- User 1–1 VendorProfile (only when is_vendor = true).

- VendorProfile 1–N Product.

- Category 1–N Product (single category per product for MVP).

- User 1–N Interaction; Product 1–N Interaction.

- Product 1–N Review; Vendor rating = roll-up of that vendor’s product
  reviews.

- Interaction 1–1 Review (a review must reference the buyer’s prior
  interaction).

*Note: there is intentionally no Order/Transaction entity, because
transactions occur off-platform. The Interaction entity is the closest
observable signal and stands in for lead/intent.*

# 5. Functional Requirements

## 5.1 Authentication & OTP

Workflow

4.  User enters a Nigerian (+234) phone number.

5.  System sends a one-time code via WhatsApp (primary channel); falls
    back to SMS on delivery failure.

6.  User enters the code; system validates within the 5-minute window
    and creates/loads the session.

Rules & validation

- Nigeria only: +234 with valid carrier prefixes; VoIP/disposable
  numbers blocked.

- One account per primary phone. One optional verified secondary phone
  for recovery only (not a second login identity).

- OTP expires after 5 minutes. Resend button with a 30–60s cooldown; max
  3 resends per 15 minutes.

- 3 incorrect entries invalidate the code; repeated invalidations
  trigger a 15-minute lockout on that phone.

- Rate limits: OTP requests 3 / 15 min, 5 / hour, 10 / day per phone;
  auth endpoints throttled per IP; general API ~60 req/min/IP.

- Anti-pumping: per-phone, per-IP and per-device limits; Termii daily
  spend alarm + spend-velocity alert; invisible bot check on request
  spikes.

Edge cases

- Code not received → resend (within cooldown) or switch channel.

- Number change → OTP-verify the new number; listing WhatsApp number is
  edited separately so listings never silently break; notify the user.

- Both phones lost → manual admin recovery after re-running the NIN
  name-match against the original verified name.

## 5.2 Vendor Onboarding & Identity Verification

Workflow

7.  Authenticated user submits: Business Name, business WhatsApp number,
    and identity verification (NIN for individuals or CAC RC number for
    registered businesses).

8.  System verifies identity through a licensed KYC API and checks that
    the returned name matches the applicant’s name.

9.  Application enters the pending queue; an admin approves or rejects.

10. On approval the vendor enters probation tier and may create listings
    (first three are pre-moderated).

Rules & validation

- Accepted methods: NIN (individuals), CAC RC number (businesses). BVN
  is not used.

- Admin verifies the name match and the application’s legitimacy before
  approval.

- Business name is unique (trimmed, case-insensitive).

- Business WhatsApp number is captured separately (defaulted from the
  login phone but overridable) and validated as WhatsApp-reachable.

- Raw NIN/identity numbers are not stored — only verification status,
  returned name, match result, KYC reference, and timestamp (see Section
  9).

Edge cases

- Unreadable/failed verification → clear icon-led retry (up to 3), then
  route to manual admin review; never dead-end the user.

- Rejection → notify (WhatsApp + in-app) with a short standard reason;
  allow fix-and-resubmit; “Contact support” WhatsApp link.

## 5.3 Product Listing Management

Workflow

11. Approved vendor creates a listing: 1–5 photos, title, description,
    price, price type, category, condition, city/state.

12. Probation vendors’ first three listings are pre-publish moderated;
    once they pass, the vendor becomes Trusted and later listings
    publish immediately (post-publish moderation via reports and
    spot-checks).

Rules & validation

- Photos: 1 required, up to 5; JPEG/PNG/WebP; ≤5MB pre-compression;
  compressed/resized client-side to ~1280px before upload; first photo
  is the cover.

- Title ≤80 characters; description ≤1000 characters.

- Price in NGN, greater than ₦0; flag as Fixed or Negotiable; “Contact
  for price” optional.

- Single category per product. Condition is category-dependent: New/Used
  for fashion, shoes and home goods; locked to New/Sealed for Food &
  Groceries and Beauty & Personal Care.

- Soft cap of 50 active listings per vendor (raised on request).

- Listing statuses: Live, Unavailable (out of stock; hidden,
  reactivatable), Archived (vendor removed), Removed (admin).

- Listings not updated in 60–90 days receive a “Still available?” nudge;
  no response auto-sets them to Unavailable.

- Edit history is retained; trusted vendors’ edits publish immediately;
  untrusted or previously-reported items are re-moderated on edit.

Edge cases

- Interrupted photo upload on poor network → per-image progress + retry;
  listing saved as draft so nothing is lost.

- Prohibited/illegal item → banned-keyword flag at submission,
  report-driven takedown, admin removal, possible vendor penalty.

## 5.4 Browsing, Search & Discovery

Rules & validation

- Anonymous users may browse, search and view; login is required only to
  chat or review.

- Search covers title + category + city/state, with description at lower
  weight; case-insensitive, partial match.

- Filters: Category, Location (State → City), Price range (Condition and
  Negotiable optional). Default sort: Most recent; options: Price ↑/↓,
  Highest rated.

- Location scope: structured State/City filtering is in scope; GPS,
  distance/radius and “near me” are out of scope for MVP.

- Only approved-active vendors with live listings appear in results;
  hidden items show “no longer available” on direct links.

- Discovery surfaces fresh supply by default — e.g. “New this week” and
  recently added items in browsed categories — to drive repeat visits
  and cross-discovery.

Edge cases

- Empty results → friendly, icon-led state (“Nothing found for …”) with
  suggested categories and a clear next action — never a blank screen.

- Poor network → service-worker caching, skeleton loaders, auto-retry
  with backoff, offline banner with cached items.

## 5.5 Buyer Interest, Leads & WhatsApp Connect

Workflow

13. A logged-in buyer taps “Contact / I’m interested” on a listing and
    may add a short message or offer price (optional).

14. The system records an Interaction (a lead) that is visible to the
    vendor, then opens WhatsApp (via wa.me deep link) to the vendor’s
    business number with a pre-filled, editable message.

15. The transaction proceeds off-platform; the lead and its context
    remain on the platform.

Rules & validation

The interest step always records the lead before WhatsApp opens, so
buyer intent and context stay on-platform even though messaging is
external. This is what makes the vendor leads dashboard, cross-discovery
measurement, and review eligibility possible.

**Pre-filled message:** *“Hi \[Business Name\], I saw your \[Product
Title\] (₦\[Price\]) on TradeNaija. \[buyer note\]. Is it still
available?”*

- An Interaction is a logged interest event by a logged-in user (user,
  vendor, product, optional message/offer, timestamp).

- Each interaction is flagged as cross-discovery when the buyer had no
  prior interaction with that vendor.

- Metrics count distinct users per product per 24h; the raw event log
  retains all events for abuse analysis.

Edge cases

- WhatsApp not installed → fallback options: Call (tel:), Copy number,
  SMS with the same pre-filled text; the number is always shown in plain
  text.

- Repeat taps by the same user are rate-limited for metrics; anomalies
  flagged.

## 5.6 Ratings & Reviews

Rules & validation

- Interaction-gated: only a logged-in user who tapped “Chat on WhatsApp”
  may review that product/vendor, after a ~48-hour cooldown.

- Reviews are badged “Reviewer contacted this vendor” — never “Verified
  purchase.”

- Rating is 1–5 stars with optional short text.

- Reviews are written against a product; the vendor rating is the
  roll-up of their product reviews.

- One review per buyer per product; editable within 30 days; not
  buyer-deletable; admin can remove; vendors cannot review buyers or
  delete reviews.

Anti-abuse

- Primary defense is interaction-gating plus
  one-review-per-user-per-product.

- Report button on reviews routes to the admin queue; anomaly detection
  (bursts of new-account 5-stars) added post-MVP.

## 5.7 Reporting & Trust/Safety

- Report buttons on listings, vendors and reviews from day one.

- Report → reason picker → admin queue; report counts tracked per
  vendor; vendors crossing a threshold are auto-flagged for review.

- A “Report a problem” WhatsApp link reaches the admin team directly.

## 5.8 Admin Dashboard & Moderation

Dashboard content

- User growth, active users, active listings, OTP success rate,
  buyer–seller interactions, top categories and locations.

- Actionable reports: pending approvals, flagged content, inactive
  users/vendors.

Moderation

- Moderatable objects: listings, reviews, vendors/users. Actions:
  approve/reject, hide/unhide, suspend/reinstate, soft-delete, warn.

- Actions are reversible where possible (soft-delete; suspend ≠ delete)
  and use a standardized reason enum that drives vendor-facing messages.

- Every action is attributed and written to the immutable AuditLog.

- Concurrency: optimistic locking — a second admin acting on an
  already-actioned item is rejected with “already actioned by \[admin\]”
  and the queue refreshes.

Suspend / delete effects

- Suspend vendor → listings hidden immediately; reviews retained;
  reversible.

- Delete vendor → listings archived (hidden); reviews retained then
  anonymized after retention; verification data deleted per NDPA. No
  immediate hard-delete — purge only after the retention window.

## 5.9 Vendor Metrics (vendor-facing)

A deliberately simple, icon-led view: leads (“X people contacted you,”
with the buyer’s note where given), listing views, reviews and average
rating, shop-page views and shares, and listing status. Leads are the
primary reason a vendor returns to the app.

## 5.10 Notifications

- Channels: in-app plus WhatsApp/SMS via Termii (WhatsApp preferred).

- Vendor triggers: new lead received (the core pull-back signal), vendor
  approved/rejected, listing approved/taken down, new review received.

- Buyer triggers: review nudge ~48h after an interaction (“Did you buy
  from \[Vendor\]? Leave a quick review”), and new-in-category alerts
  for categories the buyer has browsed (drives repeat visits).

- Low-priority notifications are batched to avoid overwhelming low-tech
  users.

## 5.11 Vendor Shop Page (Public & Shareable)

Each vendor has a public shop page that lists all their live products
with their Verified Vendor badge and rating, reachable by a short
shareable link the vendor can post on WhatsApp status or social media.
This is the core of vendor-led growth — every share is buyer acquisition
the platform did not pay for.

- Publicly viewable without login; the contact action still requires
  login.

- Shows only the vendor’s live listings; the page is hidden if the
  vendor is suspended or unapproved.

- Tracks page views and share/link clicks, surfaced in vendor metrics.

# 6. Consolidated Business Rules

16. The platform is a discovery and connection tool only; no in-app
    payments or delivery in the first 6 months.

17. All transactions happen via WhatsApp, off-platform.

18. Authentication is phone + OTP only (Nigeria +234); OTP expires after
    5 minutes.

19. New vendors require manual admin approval and identity verification;
    their first three listings are pre-moderated.

20. Vendor is a role on a single user account; one account per primary
    phone.

21. Reviews are interaction-gated and badged honestly; one per buyer per
    product.

22. Categories are admin-managed; single category per product for MVP.

23. Delivered as a PWA optimized for low bandwidth; English-first with
    i18n-ready architecture.

# 7. Non-Functional Requirements

| **Attribute**             | **Requirement**                                                                                                                                                                                                        |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Performance               | Meaningful load ≤ 3s at p75 on slow-4G (~1.6 Mbps, 300 ms RTT) on a low-end Android. Track p75 and p95.                                                                                                                |
| Usability / Accessibility | WCAG 2.1 AA baseline plus low-literacy patterns: icon-with-text (never icon-only for key actions), ≥48dp touch targets, high contrast, product-image-first layout. Tested with real semi-literate users before launch. |
| Language / Localization   | i18n-ready architecture; English-only content for MVP. Pidgin is the planned first additional language.                                                                                                                |
| Reliability / Backup      | Daily automated DB backups (7–30 day retention); pursue point-in-time recovery (~5-min RPO) if cost-effective, else RPO ≤ 24h; RTO ≤ 4h; restore tested before launch.                                                 |
| Availability              | 99.5% uptime target. A paid hosting instance is budgeted (free tiers sleep and would undermine the load-time goal).                                                                                                    |
| Monitoring                | Sentry for errors. Alerts on OTP success \< 95% (15-min window), error spikes, OTP/SMS spend velocity, health-check failures, and failed backups. Weekly on-call rotation across the three admins.                     |
| Security                  | HTTPS everywhere, RBAC, rate limiting, mandatory MFA for admins, full immutable audit logging of admin actions, and a defined data-retention policy.                                                                   |

# 8. Data Protection & Compliance (NDPA)

Identity data is the most sensitive data the platform handles and is
treated under the principle of data minimization.

- Verification is performed via a licensed KYC/identity provider. Raw
  NIN and BVN are not stored (BVN is not used at all).

- Stored fields are limited to: verification status, method, provider
  reference token, returned name, match result and confidence, and
  timestamp.

- Explicit consent is captured before verification, stating purpose,
  what is checked, and retention.

- Identity data is encrypted at rest; access is restricted to the three
  admins and fully audited.

- Retention: verification results are kept for the life of the account
  plus any statutory minimum, then deleted on account closure subject to
  legal exceptions (NDPA right to erasure).

**Dependency:** A Nigerian data-protection lawyer must review the
privacy policy, consent flow and retention schedule, and the platform
must register as a data controller under the NDPA before launch. This
document does not constitute legal advice.

# 9. Project Scope, Timeline & Success Criteria

**MVP Launch Target:** End of Month 3.

## In Scope (MVP)

Authentication; vendor onboarding, identity verification & approval;
product listings; search & browsing (State/City filtering); WhatsApp
connect & interaction logging; interaction-gated reviews; reporting;
admin dashboard & moderation; notifications; PWA.

## Out of Scope (MVP)

In-app payments; delivery/logistics; advanced location search
(GPS/radius/maps); monetization/commission; native apps; multi-category
listings and subcategories; translated content.

## Reconciled Success Criteria (measured by end of Month 6)

| **Metric**                                 | **Target**                                                            |
|--------------------------------------------|-----------------------------------------------------------------------|
| Registered users                           | ≥ 1,000                                                               |
| Active listings                            | ≥ 500                                                                 |
| Active verified vendors                    | ≥ 50                                                                  |
| OTP success rate                           | \> 95%                                                                |
| Buyers initiating ≥ 1 WhatsApp interaction | ≥ 30% of registered buyers                                            |
| Vendor 30-day retention                    | ≥ 50%                                                                 |
| Cross-discovery interactions               | ≥ 25% of interactions are with a vendor the buyer did not arrive with |

## Definitions of “Active” (30-day rolling windows)

- Active listing: Live, not Unavailable, created or updated within the
  last 30 days.

- Active vendor: at least one active listing and at least one buyer
  interaction in the last 30 days.

- Active user: logged in, or performed an interaction or search, within
  the last 30 days.

- Cross-discovery interaction: a buyer’s interaction with a vendor they
  had no prior interaction with — the headline signal that a true
  marketplace is forming.

*Note: the original “vendors making sales” objective is intentionally
replaced by the interaction proxy above, because completed sales occur
off-platform and are not observable. Launch is a vendor-led pilot
(Section 10); early targets are validated against real data rather than
assumed.*

# 10. Go-to-Market & Launch Strategy

Launch is vendor-led. An initial cohort of around five committed vendors
seeds supply, proves the end-to-end flow, generates the first content
and reviews, and brings their own networks as the first buyers.

- Vendors are the easier, more durable side and double as a
  buyer-acquisition channel via shareable shop pages.

- The initial cohort is a pilot, not a marketplace; cross-discovery is
  not expected until supply density builds, and individual deals closing
  on WhatsApp at this stage is normal.

- Beachhead by data: instrument the pilot and concentrate the next wave
  of recruitment where real interactions — especially cross-discovery —
  are strongest.

- Density may be geographic (“local traders near you,” all categories)
  or by category. The local-market model suits the low-tech audience and
  is the preferred default if vendors cluster by city.

*The honest measure of progress is cross-discovery (Section 9), not raw
interaction volume, since early interactions are largely vendors’ own
customers routed through the app.*

# 11. Monetization Roadmap

The MVP is free: no listing charges and no commission for the first 6
months. Monetization begins only after traction and is value-aligned and
opt-in.

| **Phase**     | **When**     | **Model**                                                                                                                                                                               |
|---------------|--------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Phase 1 (MVP) | 0–6 months   | Free. Goal is supply, trust and traction. No charges of any kind.                                                                                                                       |
| Phase 2       | ~6–12 months | Promoted/boosted listings and an enhanced Verified-Vendor tier or extra listing slots beyond the free cap. Opt-in; does not tax base supply; requires buyer demand worth competing for. |
| Phase 3       | 12 months+   | Optional escrow plus a small transaction fee once payments exist. Escrow doubles as the strongest anti-leakage and trust mechanism.                                                     |

Listing fees were considered and rejected for the MVP: they add friction
to the side the platform most wants to grow, tax the supply that drives
discovery, and would force building payment collection — the very
capability deferred for the first 6 months.

# 12. Security, Risks & Mitigations

| **Risk**                                               | **Mitigation**                                                                                                                                                                                                                             |
|--------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Fake or retaliatory reviews                            | Interaction-gating; one review per user per product; report button; honest badging; post-MVP anomaly detection.                                                                                                                            |
| OTP/SMS pumping draining budget                        | WhatsApp-first; NG-only + valid-prefix enforcement; per-phone/IP/device limits; spend-velocity alerts and daily budget alarm.                                                                                                              |
| Identity-data breach / NDPA exposure                   | No raw NIN/BVN stored; KYC via licensed provider; encryption, access control, audit logging; legal review before launch.                                                                                                                   |
| Off-platform scams                                     | Verified Vendor badge; reporting from day one; auto-flag vendors crossing report thresholds; “transact carefully” disclaimer at handoff.                                                                                                   |
| Moderation overload for a 3-person team                | Trust-tier graduation; pre-moderation only for new vendors’ first three listings; report-driven and spot-check moderation thereafter.                                                                                                      |
| Poor connectivity degrading UX                         | PWA caching, image compression, skeleton loaders, retry with backoff; performance budget at p75 on slow-4G.                                                                                                                                |
| Disintermediation / leakage (deals leave for WhatsApp) | Accepted as inherent; countered by recurring discovery, on-platform reputation (badges + reviews), vendor stickiness (leads + visibility), shareable shop pages, and — later — escrow. Measured via cross-discovery, not raw interactions. |

## Terms & Liability

Launch requires a Terms of Service, an NDPA-compliant Privacy Policy,
and a clear disclaimer shown at the WhatsApp handoff stating that
TradeNaija is a discovery platform, that transactions are directly
between buyer and seller, and that the platform does not handle payment
or delivery. Acceptance is captured at signup and the documents are
reviewed by Nigerian counsel.

# 13. Outstanding Dependencies

24. Engage Nigerian data-protection counsel; register as data controller
    under the NDPA.

25. Select and integrate a licensed KYC/identity-verification provider
    (NIN + CAC).

26. Confirm the prohibited-items list with counsel.

27. Budget a paid hosting instance to meet the availability and
    performance targets.

28. Confirm the pilot vendor cohort and instrument cross-discovery
    measurement from day one.

# Addendum A: Cost & Resource Estimate (First 6 Months)

## Development Cost

Cash cost: ₦0 – ₦150,000 (Founder plus AI tools handle development,
design, PM and testing).

## Infrastructure & Running Costs

| **Item**                    | **Recommendation**        | **Monthly Cost**  | **Notes**                                            |
|-----------------------------|---------------------------|-------------------|------------------------------------------------------|
| Hosting                     | Render.com (paid tier)    | ₦0 – ₦15,000      | Paid instance recommended; free tier sleeps.         |
| Image Storage               | Cloudinary                | ₦0 – ₦8,000       | Use f_auto,q_auto delivery.                          |
| OTP / SMS & WhatsApp        | Termii                    | ₦15,000 – ₦45,000 | Main variable cost; WhatsApp-first to control spend. |
| Identity verification (KYC) | Licensed NIN/CAC provider | Variable          | New in v2.0; per-verification pricing.               |
| Domain + SSL                | Namecheap                 | —                 | ₦8k–12k yearly.                                      |
| Monitoring                  | Sentry                    | ₦0 – ₦10,000      | —                                                    |

**Total Estimated Budget (First 6 Months) — Low:** ₦150,000 – ₦300,000
(plus KYC usage)

**Total Estimated Budget (First 6 Months) — Realistic:** ₦400,000 –
₦700,000 (plus KYC usage)
