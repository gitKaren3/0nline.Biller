# Banking details

Where a tenant's clients pay invoices. Added 2026-10-31.

There was no payee data before this: an invoice carried a `PaymentRef`, but nothing said
which account the money should reach.

## The table

`BankAccount` holds one row per deposit account. It is tenant-owned (`TenantID`), which
is what makes the generated provider scope all five queries — see
[`tenancy.md`](tenancy.md#the-tenants-bank-accounts) for the scoping and default-account
rules.

| Column | Notes |
|---|---|
| `AccountName` | Account holder, as it must appear on the payment |
| `BankName` | |
| `AccountNumber` | Stored without spaces; max 34 (IBAN) |
| `BranchCode` | Optional |
| `AccountType` | Cheque / Savings / Transmission / Business |
| `SwiftCode` | Optional, upper-cased; for international payments |
| `IsDefault` | One default per tenant, enforced by a partial unique index |
| `Active` | Soft delete; the service hard-deletes when it can |

Schema: [`migrations/2026-10-29-bank-account.sql`](migrations/2026-10-29-bank-account.sql).
It has been applied to the development database.

## The invariant

A tenant with an active account always has exactly one default, and the default is the
account payment instructions use. `BankAccountService` maintains it:

- demote the others *before* promoting, because the unique index is checked per statement;
- promote the first active account after a delete or a failed write;
- re-read the row after a write and return that, because the invariant maintenance writes
  straight to the database. Without the reload the API replied `IsDefault = false` for an
  account it had just made the default — found by the live-database check below.

The database rejects a second default for the same tenant regardless.

## API and UI

- `api/biller/bankaccount` — the generated, tenant-scoped controller.
- `Wasm/Components/BankAccountSection.razor` — lists, adds, edits, removes and promotes.
  It loads and saves itself, so a host only renders it.
- `Wasm/Components/BankAccountForm.razor` — the fields and their `EditContext`.
- `Wasm/Pages/Banking.Page.razor` (`/banking`) — the standalone page, reachable from the
  **Banking** item in the nav menu.

`BankAccountSection` has `ShowHeading` (default `true`). The page passes `false` and
supplies its own heading; the planned Get Started wizard can render it the same way as
one step.

If the signed-in user has no tenant yet (a first login that has not visited the profile),
`/banking` sends them to `/tenant` instead of calling an API the tenant handler would
reject. A tenant with an existing workspace can save bank details before the profile is
complete, because the account only needs the tenant.

## Verification

- `dotnet build` on `Z_0nline.Biller.slnx` and `Z_0nline.Web.slnx`: 0 errors.
- `dotnet test` on `Z_0nline.Biller.slnx`: 21 tests pass. `Test/BankAccountTests.cs`
  covers form validation, masking and the empty state.
- A throwaway integration test (not committed) ran the real service against the live
  database and checked: the first account becomes the default, whitespace is trimmed,
  promoting demotes, a second tenant reads nothing and cannot write, and deleting the
  default promotes the survivor. Its rows were removed afterwards.

## Still to do

- **Invoices do not print the details yet** — there is no invoice UI. When it arrives,
  snapshot the payee details onto the invoice at issue time; pointing at the live row
  would rewrite payment instructions on invoices already sent.
- **One default, many accounts.** The table allows several accounts and the UI can manage
  them, but there is no per-currency or per-client selection yet. The Split pay roadmap
  is the reason the table is not a set of `Tenant` columns.
