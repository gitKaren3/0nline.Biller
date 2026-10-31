# Billing rates

What a tenant charges, and the rates an invoice line will price against. The table and the
form existed before 2026-10-31, but nothing hosted the form and it had never been run
against the live database. This note records what made it usable.

## The table

`TenantRate` is tenant-owned (`TenantID`), so the generated provider scopes all five
queries — see [`tenancy.md`](tenancy.md) for the scoping rules. `RateType` is global, not
tenant-scoped: it is a fixed list (Hourly, Daily, Monthly Retainer, …).

| Column | Notes |
|---|---|
| `TenantID` | Stamped from the token; a body value cannot move it |
| `RateTypeID` | Required; foreign key to the global `RateType` |
| `CompanyID` / `ContactID` | Optional; set one to price only that client/contact |
| `DefaultRate` | Required; the standard rate when no company/contact is set |
| `StartDate` | `date`, not null; the form defaults a new rate to today |
| `EndDate` | `date`, optional |
| `Active` | Soft-delete flag; `DeactivateQuery` sets it false |

A rate with no client and no contact is the tenant's standard rate. Note that `Active` is
not derived from the dates: an ended rate keeps `Active = true` until it is removed.

## The UI

- `Wasm/Pages/Rates.Page.razor` (`/admin/rates`) — the page, reachable from **Rates** under
  **My Admin**. It hosts the section and supplies the heading.
- `Wasm/Components/TenantRateSection.razor` — lists, adds, edits and removes rates. It loads
  and saves itself, so a host only renders it. `ShowHeading` (default `true`) lets a page
  supply its own title; the planned Get Started wizard can render it as one step with
  `ShowHeading="false"`.
- `Wasm/Components/TenantRateForm.razor` — the fields and their `EditContext`. A host that
  wants to own the save can render the form alone and call `Validate()` (the same contract
  as `BankAccountForm`). It also exposes `Disabled` for a host that is saving, and an
  optional `OnValidSubmit` for Enter-to-save.

Lookups are optional parameters. When the host does not pass them, the form loads rate
types, companies and contacts from the registered services. A host-supplied dictionary
always wins, and a lookup handed over after the first render is picked up.

### The mandatory rate type

`RateTypeID` is a non-nullable `short` whose "not chosen" value is `0`, which a
`MudSelect` renders as the literal "0" and which `[Required]` cannot reject. The
hand-written partial `TenantRate.cs` adds a nullable view, `RateTypeSelection`, that maps
`0` to `null` and back. It carries `[Required]`, `[JsonIgnore]` and `[Editable(false)]`:

- the picker starts empty and validates on blur and on submit;
- the API payload still carries only `rateTypeId`;
- `DbService` never writes it, and the column stays `NOT NULL`.

## Lookups

- A contact is labelled `Surname, FirstName` (`LookupLabels` in
  `DL/SqlQueryGenerator.json`). The generator's default was the first text column, which
  would have shown only the first name.
- The client and contact pickers exclude the tenant's own profile. The tenant's own
  `Contact`/`Company` are named by `Tenant.ContactID`/`Tenant.CompanyID`, and
  `LookupFilters` hides those rows from the lookup. They remain tenant-owned rows; the
  profile page, attach and purge still see them. See
  [`tenancy.md`](tenancy.md#the-tenants-own-contact-and-company).
- Entity pickers sort by label. Rate types keep their table order: they are a curated
  enum, not a list of rows the tenant created.

Both `LookupLabels` and `LookupFilters` are **app-specific** and live in the app's
generator config, not in the schema or the shared libraries.

## Dates

`StartDate` and `EndDate` are PostgreSQL `date` columns. The generated model exposes them
as `DateTime?` because the entities double as Blazor models and `MudDatePicker` binds
`DateTime?`. Npgsql returns `DateOnly`, so `Shared/Db/Service` registers a Dapper handler
that converts on read, and `DbService` converts a database default read back through
`RETURNING`. Writes need no conversion; the database casts the parameter to `date`.

The column default is a fixed date, so the form sets a new rate's `StartDate` to today. A
create through the API that omits it gets the database default.

## Verification

- `dotnet test` on `Z_0nline.Biller.slnx`: 31 tests pass. `Test/TenantRateTests.cs` covers
  the required rate type, the empty initial picker, the end-before-start rule, the negative
  rate rule, the start-date default, host-supplied lookups, the list labels, the empty
  state and a full add → validate → save round trip.
- `dotnet build` on `Z_0nline.Biller.slnx` and `Z_0nline.Web.slnx`: 0 errors.
- Throwaway integration probes (not committed) against the live database checked: rate
  create with and without `StartDate`, `GetAllAsync`, update; and that the client lookups
  return nothing for a tenant with only its own profile, then exactly the new client after
  one was created. Their rows were removed afterwards.

## Still to do

- **No invoice UI yet.** `InvoiceLine.TenantRateID` exists, but nothing prices a line
  against a rate. When it does, the rate on the line should be the one valid at issue time,
  not a live lookup.
- **Deactivation is one-way.** A rate removed while an invoice line references it is
  deactivated (a hard delete is attempted first), and there is no API path to reactivate
  it. Re-adding a replacement is the intended flow.
- **`LookupFilters` is raw SQL.** The generator validates that the key is a real table, but
  not the columns or the expression; a mistake fails at query time, not generation time.
- **Legacy self-scoped rates.** A rate created before the lookup filter and scoped to the
  tenant's own company/contact would render without that name. No such rows exist.
- **Contacts are not narrowed by client.** The contact picker lists every client contact;
  choosing a company does not filter it to that company's contacts.
