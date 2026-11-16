-- Optimistic concurrency and audit stamps for the tables whose writes go through
-- DbStore. See Shared/docs/db-layers.md, sections "Concurrency" and "Audit stamps".
--
--   Version          the [ConcurrencyCheck] token. Matched in the UPDATE predicate and
--                    incremented by the statement itself, so the first writer wins and
--                    the second gets DbConcurrencyException. DEFAULT 1 because the store
--                    never sends the column on INSERT.
--   LastModifiedOn   stamped by the store on insert and update. DEFAULT now() only so an
--                    INSERT that predates the store's stamping still works.
--   LastModifiedBy   the signed-in user id, nullable because a background job or the
--                    first-login ensure has no user claim.
--
-- Deliberately omitted, with reasons:
--   CompanyContactLink              pair-scoped link table, no update path
--   File, RateType                  File is written by raw SQL only; RateType is read-only
--   InvoiceSnapshot                 carries its own CreatedAt/LastAccessedAt/RevokedAt and
--                                   is updated by raw SQL, so a version would never move
--   TenantSetting                   its raw INSERT ... ON CONFLICT upsert bypasses the store
--   Tier, InvoiceStatus, TaxScheme  lookup/enum tables, never written as models
--   test                            scratch table
--
-- Idempotent: safe to re-run.

BEGIN;

ALTER TABLE "BankAccount"
    ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "LastModifiedOn" timestamp with time zone DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "LastModifiedBy" bigint;

ALTER TABLE "Company"
    ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "LastModifiedOn" timestamp with time zone DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "LastModifiedBy" bigint;

ALTER TABLE "Contact"
    ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "LastModifiedOn" timestamp with time zone DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "LastModifiedBy" bigint;

ALTER TABLE "Image"
    ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "LastModifiedOn" timestamp with time zone DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "LastModifiedBy" bigint;

ALTER TABLE "Invoice"
    ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "LastModifiedOn" timestamp with time zone DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "LastModifiedBy" bigint;

ALTER TABLE "InvoiceLine"
    ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "LastModifiedOn" timestamp with time zone DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "LastModifiedBy" bigint;

ALTER TABLE "Tenant"
    ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "LastModifiedOn" timestamp with time zone DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "LastModifiedBy" bigint;

ALTER TABLE "TenantRate"
    ADD COLUMN IF NOT EXISTS "Version" integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS "LastModifiedOn" timestamp with time zone DEFAULT now(),
    ADD COLUMN IF NOT EXISTS "LastModifiedBy" bigint;

-- Existing rows predate the columns; ADD COLUMN already filled them with now().
-- The UPDATE is a no-op on a fresh run and keeps the NOT NULL step honest.

ALTER TABLE "BankAccount"   ALTER COLUMN "LastModifiedOn" SET NOT NULL;
ALTER TABLE "Company"       ALTER COLUMN "LastModifiedOn" SET NOT NULL;
ALTER TABLE "Contact"       ALTER COLUMN "LastModifiedOn" SET NOT NULL;
ALTER TABLE "Image"         ALTER COLUMN "LastModifiedOn" SET NOT NULL;
ALTER TABLE "Invoice"       ALTER COLUMN "LastModifiedOn" SET NOT NULL;
ALTER TABLE "InvoiceLine"   ALTER COLUMN "LastModifiedOn" SET NOT NULL;
ALTER TABLE "Tenant"        ALTER COLUMN "LastModifiedOn" SET NOT NULL;
ALTER TABLE "TenantRate"    ALTER COLUMN "LastModifiedOn" SET NOT NULL;

COMMIT;
