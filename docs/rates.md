# Billing rates

What a tenant charges, and the captured rates an invoice line can start from. A rate is
a record of a decision, not a price list that is kept up to date.

## The table

`TenantRate` is tenant-owned (`TenantID`), so the generated provider scopes all five
queries — see [`tenancy.md`](tenancy.md) for the scoping rules. `RateType` is global, not
tenant-scoped: it is a fixed list (Hourly, Daily, Monthly Retainer, Service, Ad-hoc, …).

| Column | Notes |
|---|---|
| `TenantID` | Stamped from the token; a body value cannot move it |
| `RateTypeID` | Required; foreign key to the global `RateType` |
| `CompanyID` / `ContactID` | Optional; set one to price only that client/contact |
| `RateIncl` | Required; the tax-inclusive amount |
| `CapturedOn` | Required UTC instant, set by the database on insert |
| `Note` | Optional free-form caption when several rates share a type and scope |
| `Active` | `true` = current, `false` = archived |

`StartDate` and `EndDate` were removed on 2026-11-12. There is no effective-date window:
a rate either applies now or it is archived.

## Captured, not edited

A rate is immutable from capture. There is no update path: `TenantRateService.UpdateAsync`
returns a validation failure. A mistake is corrected by deleting the row and capturing a
new one while nothing references it.

`DeleteOrDeactivateAsync` first tries a hard delete. The `InvoiceLine.TenantRateID` foreign
key is restrictive, so a rate referenced by any line cannot be deleted; the service then
archives it by setting `Active = false`. Archived rates can never be selected for a new
line, but the invoices that used them keep rendering.

## How an invoice picks a rate

`TenantRateResolver` is the single rule. It excludes archived rates and applies this scope
matrix:

| Invoice scope | Eligible rates, highest priority first |
|---|---|
| Contact only, no company | Contact-only rate (`CompanyID IS NULL`, `ContactID = contact`) → universal rate (`CompanyID IS NULL`, `ContactID IS NULL`) |
| Contact + company | Company + contact rate → company-only rate (`CompanyID = company`, `ContactID IS NULL`) → universal rate |

A company-scoped rate is not offered on a private contact invoice. A contact-only rate is
not offered once a company is involved. `CompanyID` is never inferred from the contact:
the user must choose the company on the invoice.

Within the same scope, the most recently `CapturedOn` rate wins; `ID` is the deterministic
tie-break. The quick-rate menu shows the whole eligible shortlist, grouped by rate type and
ordered by that same priority.

## The invoice line

`InvoiceLine` stores both:

- `RateTypeID` — optional; the category the user chose.
- `TenantRateID` — optional provenance; the captured rate the price came from.

`BilledRateIncl` is the authoritative price. When `TenantRateID` is set, `RateTypeID` and
`BilledRateIncl` must still equal that captured row. The invoice service rejects a
mismatch, because immutable rates mean a mismatch can only be corrupt data. Editing the
numeric price clears `TenantRateID`; the line is then a custom price. Changing the
invoice's contact or company clears the source links but keeps the prices.

Selecting a rate type prefills the quantity:

1. the most recent non-draft invoice line for the same contact, same company and the same
   rate type;
2. otherwise `RateType.MaxQty` when it is greater than zero;
3. otherwise `1`.

## The UI

- `Wasm/Pages/Rates.Page.razor` (`/admin/rates`) hosts `TenantRateSection`.
- `TenantRateSection` lists **Current** and **Archived** views, grouped by rate type with
  the standard rate first. It captures and removes rates; it does not edit them.
- `TenantRateForm` is the reusable capture form.
- `RateDialog` is the invoice editor's in-place capture. It is prefilled with the line's
  contact, company, rate type and current price.
- `TaxedAmountField` is the shared inclusive/exclusive amount field. It is the only place
  that turns an edited Excl or Incl value into the stored inclusive amount.

## Tax

A rate stores only `RateIncl`. `Tax` is derived with `TaxMath` from the tenant's current
`TenantSetting.DefaultTaxRate`, so it is never stale data on the rate row. The invoice line
stores the tax actually charged (`TaxRate`, `TotalTax`).

When the issuer company is tax-registered, the UI defaults **Show tax** on and displays
the rate as Excl + Tax. Tax is read-only. When it is not registered, the field is simply
**Rate**, with no `(Incl)` suffix, and the PDF omits the tax column and tax subtotal.

`TaxSchemeDefaults.ForCountry` seeds a sensible first scheme (VAT for South Africa and
most VAT jurisdictions, Sales Tax for the US, otherwise Other, which prints as "Tax").
The user can still change it.

## Migration

The schema change is `docs/migrations/2026-11-12-rate-capture.sql`:

- rename `DefaultRate` → `RateIncl`;
- drop `StartDate` / `EndDate`;
- add `CapturedOn` and `Note`;
- add nullable `InvoiceLine.RateTypeID`;
- make `InvoiceLine.TenantRateID` nullable but keep its restrictive FK;
- backfill existing line rate types from the rate they referenced;
- seed the generic `Service` and `Ad-hoc` rate types.

## Verification

`dotnet test` covers the resolver matrix, archived exclusion, recency, the required rate
type, the negative-rate rule, host-supplied lookups, the current/archived view, the
capture round trip, and the inclusive/exclusive tax arithmetic.
