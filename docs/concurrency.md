# Concurrency and audit stamps

Status of the optimistic-concurrency / audit rollout, as of 2026-11-15.

The **mechanism** is shared and is documented in `Shared/docs/db-layers.md`
("Concurrency" and "Audit stamps"). This file is the Biller instance of it: what is
live, what is deliberately not, and what is left. Read the shared section first —
it explains why the version is incremented inside the statement and why a timestamp
cannot replace it.

There are **no production instances** of this database, so no migration sequencing
has to be respected. Any dev copy needs
`docs/migrations/2026-11-15-audit-and-version.sql` before code that expects the
columns runs; the failure mode otherwise is a 500 on every save (the model has the
property, the table does not have the column).

## Live now

| Piece | Where |
|---|---|
| Columns | `Version integer NOT NULL DEFAULT 1`, `LastModifiedOn timestamptz NOT NULL`, `LastModifiedBy bigint` on the 8 tables below |
| Migration | `docs/migrations/2026-11-15-audit-and-version.sql` (idempotent) |
| Generator | `DL/DBModelGenerator.json`: `ConcurrencyColumn`, `AuditColumns`; `DL/DBModelGenerator.py`: marks + validates |
| Models | `BankAccount`, `Company`, `Contact`, `Image`, `Invoice`, `InvoiceLine`, `Tenant`, `TenantRate` — `IDbAuditable`, `[ConcurrencyCheck]` on `Version` |
| Conflict mapping | `DL/Services/Base/BillerBaseService.UpdateAsync` and `DL/Services/InvoiceService.UpdateWithLinesAsync` return `Result.Conflict` |
| UI clones | `Wasm/Pages/Clients.Page.razor` (`Clone(Company)` + nested contact), `Wasm/Components/BankAccountSection.razor` (`Clone(BankAccount)`) carry `Version` |

A table opts in by **having the columns**; the generator never guesses. That is why
the migration, not the config, decides participation.

### The one rule for UI code

An entity passed to `UpdateAsync` must carry the `Version` it was loaded with. Any
`new Invoice { ... }`, `Clone(...)` or field-by-field mapping that omits `Version`
saves as version 0 and is refused as a conflict. Prefer mutating the loaded instance;
when a copy is unavoidable, copy `Version` with it.

## Deliberately not covered

| Table | Why |
|---|---|
| `CompanyContactLink` | pair-scoped link, no update path (deletes only) |
| `File` | written by raw SQL only (`TenantService.PurgeAsync`) |
| `RateType` | read-only (`IDLReadableService`); add columns if it gains a write path |
| `InvoiceSnapshot` | own `CreatedAt`/`LastAccessedAt`/`RevokedAt`, updated by raw SQL |
| `TenantSetting` | raw `INSERT … ON CONFLICT` upsert bypasses the store — see below |
| `Tier`, `InvoiceStatus`, `TaxScheme` | lookup/enum tables, never written as models |
| `test` | stray scratch table, not part of the app |

Raw SQL also bypasses the version bump and the stamps: `TenantService.PurgeAsync`,
`InvoiceService`'s line-delete and counter advance, `InvoiceSnapshotService`'s access
count. A store update concurrent with one of those will not conflict. That is
accepted, not accidental.

## Outstanding

1. **Commit the Biller work.** The tree has the migration, the generator change, 8
   regenerated models, 2 services and 2 UI files uncommitted. Suggested split:
   generator + models + migration, then app wiring (services + UI).
2. **`TenantSetting`.** Convert `TenantSettingService.GetOrCreateAsync`/`SaveAsync`
   from the raw upsert to the store, then add the three columns. Until then its saves
   are last-write-wins and carry no stamp.
3. **SmartTicket and `Templates/0nline.App`.** Same generator change plus their own
   migrations. SmartTicket is a separate database; the template must emit the settings
   so new apps start correct.
4. **Membership/Auth database.** `User` and `UserSubscription` are hand-maintained
   (`Auth/Service/db/Base`). Adding `IDbAuditable` needs a partial plus the columns,
   and `AuthModelContractTests` must stay green. Not started, and lower value: that
   database is not tenant-scoped and is written mostly by `AuthService`.
5. **`DeleteAsync` / `DeActivateAsync` carry no version.** They take a key, not an
   entity, so a delete cannot be version-checked today. An entity-based overload in
   `DbStore` would fix both.
6. **No `ETag` / `If-Match`.** The version rides in the JSON body. A client that never
   sends it fails closed (always a conflict), so enabling a table is a breaking change
   for that table's `PUT`. An opaque tag would let the body stay clean.
7. **Cancellation tokens and transient retry** — the other two items from the original
   concurrency review, untouched. Retry needs per-provider SQLSTATE/error-number
   classification to stay dialect-neutral.
8. **`SqlQueryGenerator.py` not re-run.** The store composes writes itself, so the
   query providers did not need regenerating; but `Descriptor.EditableColumns` will
   not list the new columns until it is. Cosmetic today, worth a run for consistency.
9. **No database-backed test.** The shared tests assert statement text only. Nothing
   exercises two concurrent writers against a real database; the manual check on
   2026-11-15 (first update bumps 1→2, second with version 1 matches 0 rows) is the
   only end-to-end evidence.
