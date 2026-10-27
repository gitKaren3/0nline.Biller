# Tenancy in Biller

Biller's instantiation of the shared mechanism. Read `Workspace/docs/tenancy.md` first —
this file only records what would change if Biller changed. The shared child-collection
loader is described there under
[Child collections](../../Workspace/docs/tenancy.md#child-collections).

## Scoped tables

Tenant scoping is per entity: a table is isolated only when its generated provider puts
`@TenantId` into the SQL. In `DL/Providers/Generated/`:

| Table | `TenantID` column | Provider scoped | Exposed as |
|---|---|---|---|
| `Invoice` | yes | all five queries | `api/biller/invoice` |
| `Contact` | yes | all five queries | `api/biller/contact` |
| `Company` | yes | all five queries | `api/biller/company` |
| `Image` | yes | all five queries | `api/biller/image` |
| `TenantRate` | yes | all five queries | no controller |
| `Tenant` | no — it *is* the tenant | no | `api/biller/tenant` |
| `Tier` | no | no | `api/biller/tier` |
| `RateType` | no | no | no controller |
| `InvoiceLine` | no | no | read through invoice detail (no controller) |
| `CompanyContactLink` | no | no | no controller |
| `File` | yes | no controller yet | no controller |
| `test` | no | no | no controller — stray |

`Tier` and `RateType` are global by design: a lookup table that is the same for every
tenant is not a leak. `TierController` is anonymous because it is public, read-only
data: `ITierService` is `IDLReadableService<Tier>` only, so the generator emits
`GetAll`/`GetLookup` (plus the inherited `GetById`) and no write actions. `Tenant` needs
no filter to read its own row; it needs a rule about *which* row, which lives in the
service (`GetTenantByUserIdAsync`, `WHERE "UserId" = @UserId`) and in `TenantController`
(the caller's `sub` must equal `Tenant.UserId`).

## The tenant's own contact and company

`Tenant.ContactID` and `Tenant.CompanyID` are **not** redundant with
`Contact.TenantID` / `Company.TenantID`.

- `Company.TenantID` means "owned by this tenant". It includes the tenant's clients.
- `Tenant.CompanyID` means "this is the tenant's own company record" — the one shown on
  the profile. The same holds for `ContactID`.

A tenant can have many client contacts and companies; exactly one of each is its own.
The pointers are the differentiator, so they stay. They are nullable because the tenant
is created at first login before the profile is captured.

## First login: ensure

The tenant is created immediately after login, before the Get Started form is shown:

1. `POST api/biller/tenant/ensure` resolves the user from the token (`sub`), finds the
   tenant by `UserId`, and creates a bare tenant with the lowest-rank active `Tier` if
   there is none. `Tenant.UserId` has a unique index, so a user cannot end up with two.
2. `EnsureAsync` creates the free-tier subscription in Membership and returns a refreshed
   token. `TenancyMiddleware` resolves the tenant from the `tenantId` claim, so the client
   must adopt that token before saving the profile.
3. "Get Started" now means `ContactID is null || CompanyID is null`, not "no tenant".
4. The form creates/updates its own `Contact` and `Company` (both are tenant rows), then
   calls `POST api/biller/tenant/attach` with their ids. The service verifies each row
   carries this tenant's `TenantID` before setting the pointers.

`register`, `createorupdate` and `foruser/{userid}` remain for compatibility, but the
body `UserId`/`ID` is ignored, `register` is just `ensure`, and `foruser` only answers for
the caller's own `sub`. The client no longer uses any of them.

## Writes

`BillerBaseService` opts into `DbService.EnforceTenantScope`:

- On insert, a `TenantID` property is stamped from the token and any body value is
  overwritten. `ImageService` already did this explicitly; the generic path now covers
  `Contact` and `Company` too.
- On update, the generated `SET` and the `WHERE` both use the token's tenant id. A row
  with `TenantID = NULL` matches nothing; a row owned by another tenant matches nothing.
  A zero-row update becomes `KeyNotFoundException`, so it cannot be reported as saved.
- On deactivate, the tenant predicate is appended to the provider's query, which does not
  include one by default.

The invariant is `TenantID IS NOT NULL` for rows that belong to a tenant. The
`docs/migrations/2026-10-26-tenant-ownership.sql` migration backfills the tenant's own
`Contact`/`Company` (which were created before the tenant id was known) and adds the
unique `Tenant.UserId` index; `docs/migrations/2026-10-26-tenant-not-null.sql` then makes
`TenantID` `NOT NULL` on every scoped table. Do not reintroduce a nullable tenant id as a
normal state.

Writes to `Tenant` itself cannot use a `TenantID` predicate: the row has no such column.
They are guarded by `Tenant.UserId = @sub` in `TenantService`, and the nested
`Contact`/`Company` navigation properties are stripped before the update — otherwise
`SaveNavigationPropertiesAsync` would persist a client-supplied graph.

## The child tables

`InvoiceLine` and `CompanyContactLink` have no `TenantID`, so nothing can filter them.
They are safe only while they are reached through their parent: a line is loaded because
an invoice was loaded, and the invoice was filtered first.

The rule: **never expose a child table by its own key.** `GetOneAsync(lineId)` would
return another tenant's line if the id were guessed. Neither table has a controller today;
if one is added, it must be scoped by the parent, not by id.

`InvoiceLine` is surfaced through the invoice detail instead: `InvoiceService.SingleAugment`
calls `IInvoiceLineService.GetByInvoiceIdAsync`, which uses
`DbService.GetByForeignKeyAsync` to load the lines (and each line's `TenantRate`) after the
invoice itself has been tenant-filtered. `IInvoiceLineService` sits in
`DL.Contract/Interfaces/Internal/` so the controller and HTTP generators, which scan only
the top level of `Interfaces`, cannot emit an `InvoiceLine` endpoint.

## Closing and purging

Because `Tenant` and `Company`/`Contact` point at each other, a hard delete has to break
one edge first. The sequence is in `TenantService.PurgeAsync`:

1. `Tenant.ContactID`/`CompanyID` are set to `NULL` (the cycle break). The migration gives
   those FKs `ON DELETE SET NULL`, so a profile row can also be removed independently.
2. `CompanyContactLink`, then `InvoiceLine`, then `Invoice`, then `TenantRate`. Lines
   reference both invoices and rates, and invoices reference contacts.
3. `Company` before `Contact` (`BillingContactID`) and `Image` (`LogoImageID`).
4. `Contact`, `Image`, `File`, then `Tenant`.
5. After commit, the image/file files are removed from disk and the membership
   subscription is deleted.

`CloseAsync` is the normal removal action (not exposed in the UI yet): it sets `Active = FALSE`,
sets `UserId = NULL` and removes the subscription. The tenant and its invoices remain for
retention, and the next login's `ensure` creates a new workspace. That replaces the old
"delete a subscription and leave `Tenant.UserId` behind" gotcha — the user id is now
actually cleared.

Purge is destructive and has no HTTP route. The ordered implementation lives in
`TenantService.PurgeAsync` for a privileged back-office job; do not expose it to a tenant
or an autonomous agent without a separate policy and an audit trail.

A token issued before a close keeps its `tenantId` claim until it expires (60 minutes, no
`jti`). Closing removes the subscription so the next login cannot mint another, but
immediate revocation still needs a denylist or a token version check.

## Known gaps

- `TenantController.GetById`/`foruser` are owner-checked, but the generated controllers for
  other entities still accept cookie auth as well as bearer.
- `ImageController` accepts arbitrary files into the public uploads directory.
- A tenant-scoped row can still *reference* another tenant's row by foreign key
  (`Company.BillingContactID`, `Company.LogoImageID`, `Invoice.CompanyID`/`ContactID`,
  `TenantRate.*`). The write gate scopes the row, not the ids in it, and navigation
  loading then reads the referenced row by id. Validate referenced ids on write before
  exposing these endpoints to an agent.
- API errors still surface `ex.Message` in some paths.
