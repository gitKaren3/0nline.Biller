-- Tenant ownership invariant.
--
-- The tenant is created at first login, before it has a contact or company, so
-- both pointers must be nullable. Once the profile is saved the pointer is set
-- and the child row carries a non-null TenantID, which is what the scoped
-- UPDATE/DELETE predicates rely on.
--
-- Closing a tenant detaches the user (UserId = NULL) so the next login starts a
-- new workspace while the old tenant and its invoices remain for retention.
-- A unique index keeps the "one tenant per user" rule that makes ensure idempotent.

BEGIN;

-- Existing rows were created before the tenant id was available: adopt them
-- through the tenant's own pointer, which is the ownership proof.
UPDATE "Contact" c
   SET "TenantID" = t."ID"
  FROM "Tenant" t
 WHERE t."ContactID" = c."ID"
   AND c."TenantID" IS NULL;

UPDATE "Company" c
   SET "TenantID" = t."ID"
  FROM "Tenant" t
 WHERE t."CompanyID" = c."ID"
   AND c."TenantID" IS NULL;

-- A tenant without a profile is a valid state (Get Started), and a closed
-- tenant keeps its history but no longer belongs to a user.
ALTER TABLE "Tenant" ALTER COLUMN "ContactID" DROP NOT NULL;
ALTER TABLE "Tenant" ALTER COLUMN "UserId"    DROP NOT NULL;

-- One workspace per user. Multiple NULLs are allowed, so closed tenants do not
-- violate it.
CREATE UNIQUE INDEX IF NOT EXISTS "UX_Tenant_UserId" ON "Tenant" ("UserId");

-- Deleting a profile row must null the tenant pointer, not delete or block the
-- tenant itself.
ALTER TABLE "Tenant" DROP CONSTRAINT IF EXISTS "FK_Tenant_Contact";
ALTER TABLE "Tenant"
    ADD CONSTRAINT "FK_Tenant_Contact"
    FOREIGN KEY ("ContactID") REFERENCES "Contact" ("ID") ON DELETE SET NULL;

ALTER TABLE "Tenant" DROP CONSTRAINT IF EXISTS "FK_Tenant_Company";
ALTER TABLE "Tenant"
    ADD CONSTRAINT "FK_Tenant_Company"
    FOREIGN KEY ("CompanyID") REFERENCES "Company" ("ID") ON DELETE SET NULL;

COMMIT;
