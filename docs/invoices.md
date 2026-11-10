# Invoices: the API the UI is built on

This is the contract the invoice UI consumes. It assumes the shared PDF library and
the snapshot table are in place; see [`tenancy.md`](tenancy.md#invoice-snapshots-and-public-links).

## Status flow

```
Draft ──finalize──► Finalized ──send──► Issued
  │                    │                  │
  └── delete           └── locked         └── Paid / Cancelled (not implemented yet)
```

- **Draft** is the only editable state. `PUT` rejects any other status with `Conflict`.
- **Finalize** (`POST api/biller/invoice/{id}/finalize`) requires at least one line and
  moves the invoice to `Finalized`. After that, edits and deletes are rejected.
- **Send** is `POST api/biller/invoicesnapshot/invoice/{id}`. It refuses a `Draft` or
  `Cancelled` invoice, creates (or reuses) the snapshot, and moves `Finalized → Issued`.
- `Paid` and `Cancelled` have no transition endpoint yet. Do not set them through `PUT`;
  it forces the status back to `Draft`.

## Endpoints

| Method and route | Purpose |
|---|---|
| `GET api/biller/invoice` | List. No lines loaded; use the detail read. |
| `GET api/biller/invoice/{id}` | Detail **with `InvoiceLines`**, contact and status navigation. |
| `POST api/biller/invoice` | Create the invoice **and its lines** in one transaction. |
| `PUT api/biller/invoice` | Update a draft and reconcile its lines (see below). |
| `POST api/biller/invoice/{id}/finalize` | Draft → Finalized. |
| `GET api/biller/invoice/{id}/preview.pdf` | Render the saved invoice. No snapshot, no access count. |
| `GET api/biller/invoice/next-number` | The number the next invoice will draw. Does **not** consume it. |
| `GET api/biller/invoice/number-exists?number=…&id=…` | Whether the number is already used. Advisory; the save is the guarantee. |
| `POST api/biller/invoicesnapshot/invoice/{id}[?regenerate=true]` | Create/reuse the link; marks the invoice Issued. |
| `GET api/biller/invoicesnapshot/invoice/{id}/activity` | First/last open and open count. |
| `POST api/biller/invoicesnapshot/{snapshotId}/revoke` | Kill a link. |

All of the above require a bearer token. The only anonymous route is
`GET api/biller/invoicesnapshot/{token}/pdf`, which is what the mailed link opens.

## Saving an invoice and its lines

`POST`/`PUT` carry the `Invoice` with `InvoiceLines`. The service does not trust the
navigation graph: it copies only the line's own columns and writes the invoice and
lines in **one transaction**.

Line reconciliation on `PUT`:

- `ID == 0` → insert.
- `ID > 0` and it belongs to this invoice → update.
- An existing line that is absent from the payload → delete.
- An `ID` that belongs to another invoice → validation failure.

So the UI can send the whole line table on every save; it does not need per-line
endpoints. The response is the reloaded detail, including the ids the database
assigned.

Validation (server-side):

- a contact is required and must belong to the caller's tenant;
- an optional company must belong to the caller's tenant;
- every line needs a description (≤200), quantity > 0, a rate, and non-negative
  billed rate and tax;
- every rate must belong to the caller's tenant;
- invoice number ≤30, payment reference ≤250.

## Numbering

Numbers are per tenant and sequential. `TenantSetting.NextInvoiceNumber` is the next
number to assign and `TenantSetting.InvoiceNumberFormat` is the literal text around a
single `{n}` token:

```
#{n:00000}      -> #00084
INV-{n:0000}    -> INV-0084
{n}             -> 84
2026/{n:0000}   -> 2026/0084
```

A run of zeros after the colon sets the minimum width, and the number widens rather than
truncating, so `#99999` is followed by `#100000`. Everything outside a token is printed
as typed, which is why the `0` in `2026` is not a placeholder. An optional `{yyyy}`,
`{y:2}` or `{y:4}` token prints the invoice's year; it does **not** reset the sequence.
The format is capped at 30 characters and 9 digits because `Invoice.InvoiceNumber` is
`varchar(30)` and the counter is an `int`. `InvoiceNumberFormat.TryParse` owns the rules
and the messages, and `TenantSettingService.Validate` calls it. A new tenant's row is
seeded with `InvoiceNumberFormat.DefaultFormat`, which is `#{n:00000}`.

When `InvoiceNumber` is blank on create the service draws the number, formats it and
increments the counter in one statement inside the invoice's transaction, so a failed
create does not consume a number. If the counter has fallen behind numbers already issued
(the tenant reset it, or typed numbers by hand) the claim skips them and heals the
counter.

A number supplied by the client is kept as typed, and it moves the counter with it when
its trailing digits are at or above the counter, so the next automatic number continues
after it. A lower number is kept as typed and leaves the counter alone — resetting the
sequence is the tenant's choice. The format is a generator, not a validator: a typed
number does not have to match it.

A tenant that has already invoiced elsewhere raises the next number once in Settings.
`(TenantID, InvoiceNumber)` is unique, so the same number cannot be saved twice; the
service checks first to return a friendly message and the unique index is the guarantee
under concurrency.

The editor shows the number before saving: a new invoice opens with the value from
`next-number`, which peeks (skipping numbers already issued) without consuming anything.
The number is then pinned, so what the user sees is what is sent. Leaving the field runs
`number-exists` and annotates a clash in place; a clash only discovered by the save is
annotated the same way. The refresh icon re-reads `next-number`. There is no automatic
renumbering: a taken number is always the user's to change or regenerate. The payment
reference follows the invoice number — prefilled on a new invoice and updated whenever
the number changes — because the two are almost always the same; it can still be edited
afterwards, and changing the number again brings it back into line.

## Preview

The editor must save before previewing: `preview.pdf` renders the **saved** invoice.
That is what makes the preview and the mailed document identical. The response is a
PDF; the client helper is `InvoiceServiceClient.PreviewAsync(id)`, which returns bytes
and can be turned into a blob URL in the browser.

## Tax and currency

`TenantSetting` holds the tenant's billing defaults: `CurrencyCode`,
`CurrencyMinorUnits`, `TimeZoneId` and `DefaultTaxRate`. There is one row per
tenant (`TenantID` is unique) and it is created by the first-login ensure step.

- **Currency is per invoice.** `InvoiceService` copies the settings currency onto
  the invoice at create time; changing the setting later does not reinterpret
  existing invoices. The invoice stores the code and minor units.
- **Tax is gated by the issuer's company.** A business with
  `Company.TaxSchemeID IS NULL` is not tax-registered and may not charge tax.
  When it is set, lines default to that scheme and to `DefaultTaxRate`.
- **Tax is inclusive.** `InvoiceLine.BilledRateIncl` already contains the tax.
  The service extracts it from the gross at `TaxRate` (rounded to the
  currency's minor units) and stores the result in `TotalTax`. The rate, scheme
  and optional note are stored on the line, so a later settings change cannot
  alter an issued invoice.
- **The snapshot freezes all of it.** `InvoicePdfModel` layout version 2 carries
  the currency code, symbol, minor units, the tax scheme name and the seller's
  tax number. The PDF prints the symbol and the tax breakdown; it never reads
  settings at render time.
- **The invoice date is a calendar date.** `Invoice.InvoiceDate` is a `date`,
  and the tenant's time zone decides which date "today" is. Stored instants
  elsewhere remain UTC.

One default rate is not a tax engine: reverse charge, exports, exemptions and
customer-country rules are deliberately out of scope for now. The per-line
scheme and rate are stored so those rules can be added without rewriting
historical lines.

## Clients the UI uses

- `IInvoiceService` / `InvoiceServiceClient` — CRUD, `FinalizeAsync`, `PreviewAsync`.
- `IInvoiceSnapshotClient` / `InvoiceSnapshotServiceClient` — link, activity, revoke.
- Existing pickers: `ICompanyService.ListClientsAsync`, `IContactService`,
  `ITenantRateService`, `IRateTypeService`, `IBankAccountService`, `IInvoiceStatusService`.

All are registered in `Wasm/Program.cs` with the auth and tenant handlers.

## Sending

The server never sends email. `CreateLinkAsync` returns
`InvoiceSnapshotLink { Token, Url, CreatedAt }` with an absolute URL. The UI builds a
`mailto:` from it (see the note in the handover) and opens the user's mail client.
