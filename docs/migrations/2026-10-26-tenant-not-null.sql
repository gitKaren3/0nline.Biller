-- Defence in depth for the tenant invariant.
--
-- The application now stamps TenantID from the token on every insert, and the
-- scoped UPDATE/DELETE predicates fail closed. These constraints make the same
-- rule true at the database, so a future code path that forgets the stamp
-- cannot create an unowned row.
--
-- Run after 2026-10-26-tenant-ownership.sql (which backfills the tenant's own
-- Contact/Company and adds the Tenant.UserId unique index).

BEGIN;

ALTER TABLE "Company"    ALTER COLUMN "TenantID" SET NOT NULL;
ALTER TABLE "Contact"    ALTER COLUMN "TenantID" SET NOT NULL;
ALTER TABLE "Image"      ALTER COLUMN "TenantID" SET NOT NULL;
ALTER TABLE "File"       ALTER COLUMN "TenantID" SET NOT NULL;
ALTER TABLE "Invoice"    ALTER COLUMN "TenantID" SET NOT NULL;
ALTER TABLE "TenantRate" ALTER COLUMN "TenantID" SET NOT NULL;

COMMIT;
