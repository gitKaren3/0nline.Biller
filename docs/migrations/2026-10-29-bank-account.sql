-- Tenant bank details for invoice payment instructions.
--
-- A tenant receives payment by EFT, so an invoice must be able to say where the
-- money goes. The details live in their own table rather than on "Tenant" or
-- "Company" because a tenant may hold more than one deposit account (the Split
-- pay roadmap already calls for multiple configurable accounts), and because an
-- invoice should be able to reference the account that was valid when it was
-- issued without rewriting history when the tenant changes banks.
--
-- "TenantID" is the scoping column the query generator needs: without it the
-- generated provider would read every tenant's accounts. See docs/tenancy.md.

BEGIN;

CREATE TABLE IF NOT EXISTS "BankAccount"
(
    "ID"            integer GENERATED ALWAYS AS IDENTITY,
    "TenantID"      integer                NOT NULL,
    "AccountName"   character varying(100) NOT NULL,
    "BankName"      character varying(100) NOT NULL,
    "AccountNumber" character varying(34)  NOT NULL,
    "BranchCode"    character varying(20),
    "AccountType"   character varying(20),
    "SwiftCode"     character varying(11),
    -- Deliberately no DEFAULT: a column with a default is generated as
    -- [DatabaseGenerated(Computed)] and is then never written by DbService.
    "IsDefault"     boolean                NOT NULL,
    "Active"        boolean                NOT NULL DEFAULT TRUE,
    CONSTRAINT "BankAccount_pkey" PRIMARY KEY ("ID"),
    CONSTRAINT "FK_BankAccount_Tenant"
        FOREIGN KEY ("TenantID") REFERENCES "Tenant" ("ID") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_BankAccount_TenantID"
    ON "BankAccount" ("TenantID");

-- At most one default account per tenant. Deactivated rows keep the flag but
-- are excluded, so retiring an account does not block a replacement.
CREATE UNIQUE INDEX IF NOT EXISTS "UX_BankAccount_Default"
    ON "BankAccount" ("TenantID")
    WHERE "IsDefault" AND "Active";

COMMIT;
