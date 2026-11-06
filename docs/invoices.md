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

If `InvoiceNumber` is blank on create, the server assigns
`INV-{year}-{invoiceId:D5}`, e.g. `INV-2026-00042`. This is deliberate placeholder
policy, not a domain decision: it is globally sequential and reveals the row id.
Replace it with the tenant's real scheme (and a uniqueness constraint) when that is
decided. A number supplied by the client is kept.

## Preview

The editor must save before previewing: `preview.pdf` renders the **saved** invoice.
That is what makes the preview and the mailed document identical. The response is a
PDF; the client helper is `InvoiceServiceClient.PreviewAsync(id)`, which returns bytes
and can be turned into a blob URL in the browser.

## Tax and currency

Still open. `InvoiceLine.TotalTax` is stored per line and must be supplied by the UI;
there is no tenant VAT rate or currency in the schema. The PDF prints `N2` with no
symbol. Decide these before invoices are considered correct.

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
