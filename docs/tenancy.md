# Tenancy in Biller

Biller's instantiation of the shared mechanism. Read `Workspace/docs/tenancy.md` first —
this file only records what would change if Biller changed.

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
| `InvoiceLine` | no | no | no controller |
| `CompanyContactLink` | no | no | no controller |
| `test` | no | no | no controller — stray |

`Tier` and `RateType` are global by design: a lookup table that is the same for every
tenant is not a leak. `Tenant` needs no filter to read its own row; it needs a rule about
*which* row, which lives in the service (`GetTenantByUserIdAsync`, `WHERE "UserId" = @UserId`).

Five of eleven providers are scoped. That is not an accident of the generator — it is the
set of tables that carry a `TenantID` column.

## The child tables

`InvoiceLine` and `CompanyContactLink` have no `TenantID`, so nothing can filter them.
They are safe only while they are reached through their parent: a line is loaded because
an invoice was loaded, and the invoice was filtered first.

The rule: **never expose a child table by its own key.** `GetOneAsync(lineId)` would
return another tenant's line if the id were guessed. Neither table has a controller today;
if one is added, it must be scoped by the parent, not by id.

## Tenant entry points

- `/tenant` — `Wasm/Pages/Tenant.Page.razor`. Shows the tenant profile when one exists,
  otherwise the onboarding form (`TenantForm`). On registration the form reissues the
  token before anything else, because the server reads the claim.
- `TenantController` — `createorupdate`, `register`, `foruser/{userid}`.

`foruser/{userid}` looks a tenant up by the user id in the URL and loads its `Contact` and
`Company`. It is unscoped on purpose, since onboarding runs before a tenant exists — but
it does not check the id against the caller, so any authenticated Biller user can ask for
another user's company details. It should compare the id with the caller's `sub` claim,
or be restricted to the onboarding flow.

## Gotcha

Deleting a subscription does **not** clear `Tenant.UserId`. The `/tenant` fallback query
(`WHERE "UserId" = @UserId`) still finds the old tenant and repopulates the session, so a
"reset" user lands back in their old workspace. Clear `Tenant.UserId` as well.
