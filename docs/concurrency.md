# Concurrency and audit stamps

Status of the optimistic-concurrency / audit rollout, as of 2026-11-16.

The **mechanism** is shared: read `Shared/docs/db-layers.md` ("Concurrency", "Audit
stamps", and "Which tables participate"). This file is only the Biller **decision log** —
why some writes are deliberately unprotected, and what is left.

**Do not list the participating tables here.** Participation is a fact of the schema, so
read it from the schema with the query in the shared document. A model can only be marked
when its table has the columns, and the generator proves that at build time, so a prose
copy would be a third source of truth that drifts.

There are **no production instances** of this database, so no migration sequencing has to
be respected. A dev copy needs `docs/migrations/2026-11-15-audit-and-version.sql`; the
failure mode without it is a 500 on every save (the model has the property, the table does
not have the column). The live dev database has it — verified 2026-11-16. SmartTicket
followed on the same day; see `SmartTicket/docs/concurrency.md`.

## The one rule for UI code

An entity passed to `UpdateAsync` must carry the `Version` it was loaded with. Any
`new Invoice { ... }`, `Clone(...)` or field-by-field mapping that omits `Version` saves as
version 0 and is refused as a conflict. Prefer mutating the loaded instance; when a copy
is unavoidable, copy `Version` with it.

## Deliberate exclusions

The header of `docs/migrations/2026-11-15-audit-and-version.sql` lists every table left
out and why: the pair-scoped link table, the raw-SQL writer, the read-only lookup, the
snapshot with its own timestamps, the enum tables and the scratch table. That is the
record — keep the reason at the migration that decided it. A table without the columns is
not protected, and nothing in the schema distinguishes "deliberately excluded" from
"forgotten", which is exactly why the reason has to live somewhere.

## Accepted bypasses

Raw SQL bypasses the version bump and the stamps: `TenantService.PurgeAsync`,
`InvoiceService`'s line-delete and counter advance, `InvoiceSnapshotService`'s access
count. A store update racing one of those will not conflict. That is accepted, not
accidental.

## Decisions that look like bugs

- **Delete carries no version on the wire.** The store has version-carrying
  `DeleteAsync(T)` / `DeActivateAsync(T)`, and both app bases map the conflict to
  `Result.Conflict`. But `IDLWritableService<T>` exposes only
  `DeleteOrDeactivateAsync(int)` and every generated controller calls that, so no client
  sends a version on delete. Id-only deletes are intentional for now; add an entity route
  when the delete UI needs the check.
- **The version rides in the JSON body, not in an `ETag`.** There is no `If-Match`; the
  version is matched in the `UPDATE` predicate instead. A client that never sends it fails
  closed — always a conflict — so enabling a table is a breaking change for that table's
  `PUT`. An opaque tag would let the body stay clean.
- **A copy that omits `Version` saves as 0.** That is the one rule above: refusal by
  design, not null handling.

## Left to do

1. **Membership/Auth database.** `User` and `UserSubscription` are hand-maintained
   (`Auth/Service/db/Base`); adding `IDbAuditable` needs a partial plus the columns, and
   `AuthModelContractTests` must stay green. Not started, and lower value: that database
   is not tenant-scoped and is written mostly by `AuthService`.

Cancellation and transient retry are done in the shared store — an ambient request token
and dialect-classified retry; see `Shared/docs/db-layers.md` § "Cancellation and transient
retry".

`Templates/0nline.App` is out of scope: the owner is considering retiring it, and Biller
is the reference implementation (2026-11-16).
