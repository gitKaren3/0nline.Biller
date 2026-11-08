-- Tenant billing settings, currency and per-line tax.
--
-- This closes the three gaps that blocked invoicing: the business time zone,
-- the currency an invoice is denominated in, and the tax treatment a line was
-- priced under.
--
-- Design notes:
--   * "TenantSetting" is a child of "Tenant" (one row, unique TenantID) so the
--     generated query provider scopes it automatically and an insert is stamped
--     from the token. It is not a pointer from "Tenant": that table has
--     hand-written write rules and a settings pointer would need another one.
--   * "TaxScheme" is a global lookup, like "Tier" and "InvoiceStatus". It has
--     stable keys (no identity) because the generated enum is derived from the
--     rows. There is deliberately no "None" row: absence of tax is represented
--     by "TaxSchemeID IS NULL", so a foreign key never holds the sentinel 0
--     that DbService treats as "create this row".
--   * "Company.TaxRegistered" is removed. A company is tax-registered when it
--     has a scheme; keeping a separate boolean would let the two disagree. The
--     UI shows a switch that writes "TaxSchemeID" (and clears it when off).
--   * "Company.TaxNumber" is separate from "Registration": Registration is the
--     company-registration number and is already printed as such on invoices.
--   * "Invoice.CurrencyCode" and "CurrencyMinorUnits" are frozen on the invoice,
--     not looked up from settings at render time. "TenantSetting" supplies the
--     default for new invoices only.
--   * "InvoiceLine" keeps "TotalTax" (already numeric) and adds the rate and
--     scheme so the breakdown is not lost. Unit prices remain tax-inclusive;
--     tax is extracted from the gross at the stored rate.
--   * "Invoice.InvoiceDate" becomes a calendar date. It was a timestamp without
--     a zone, which made "today" depend on the server clock. The tenant's time
--     zone decides which date is today.

BEGIN;

-- ---------------------------------------------------------------------------
-- Tax scheme lookup
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS "TaxScheme"
(
    "ID"   smallint             NOT NULL,
    "Name" character varying(50) NOT NULL,
    CONSTRAINT "TaxScheme_pkey" PRIMARY KEY ("ID")
);

INSERT INTO "TaxScheme" ("ID", "Name") VALUES
    (1, 'VAT'),
    (2, 'GST'),
    (3, 'Sales Tax'),
    (4, 'Other')
ON CONFLICT ("ID") DO NOTHING;

-- ---------------------------------------------------------------------------
-- Tenant settings
-- ---------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS "TenantSetting"
(
    "ID"                 integer      GENERATED ALWAYS AS IDENTITY,
    "TenantID"           integer      NOT NULL,
    "CurrencyCode"       character(3) NOT NULL,
    "CurrencyMinorUnits" smallint     NOT NULL,
    "TimeZoneId"         character varying(64) NOT NULL,
    -- Percentage, 0.00 to 100.00. The rate is a default for new lines, not a
    -- historical fact: the rate actually used is copied onto the line.
    "DefaultTaxRate"     numeric(5,2) NOT NULL,
    CONSTRAINT "TenantSetting_pkey" PRIMARY KEY ("ID"),
    CONSTRAINT "FK_TenantSetting_Tenant"
        FOREIGN KEY ("TenantID") REFERENCES "Tenant" ("ID") ON DELETE CASCADE,
    CONSTRAINT "UX_TenantSetting_TenantID" UNIQUE ("TenantID"),
    CONSTRAINT "CK_TenantSetting_CurrencyCode"
        CHECK ("CurrencyCode" ~ '^[A-Z]{3}$'),
    CONSTRAINT "CK_TenantSetting_MinorUnits"
        CHECK ("CurrencyMinorUnits" BETWEEN 0 AND 4),
    CONSTRAINT "CK_TenantSetting_DefaultTaxRate"
        CHECK ("DefaultTaxRate" BETWEEN 0 AND 100)
);

-- ---------------------------------------------------------------------------
-- Company tax identity
-- ---------------------------------------------------------------------------

ALTER TABLE "Company" DROP COLUMN IF EXISTS "TaxRegistered";

ALTER TABLE "Company" ADD COLUMN IF NOT EXISTS "TaxNumber" character varying(50);
ALTER TABLE "Company" ADD COLUMN IF NOT EXISTS "TaxSchemeID" smallint;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.table_constraints
    WHERE constraint_name = 'FK_Company_TaxScheme' AND table_name = 'Company'
  ) THEN
    ALTER TABLE "Company"
      ADD CONSTRAINT "FK_Company_TaxScheme"
      FOREIGN KEY ("TaxSchemeID") REFERENCES "TaxScheme" ("ID");
  END IF;
END $$;

-- ---------------------------------------------------------------------------
-- Invoice currency and calendar date
-- ---------------------------------------------------------------------------

ALTER TABLE "Invoice" ADD COLUMN IF NOT EXISTS "CurrencyCode" character(3);
ALTER TABLE "Invoice" ADD COLUMN IF NOT EXISTS "CurrencyMinorUnits" smallint;

UPDATE "Invoice" i
   SET "CurrencyCode" = COALESCE(ts."CurrencyCode", 'ZAR'),
       "CurrencyMinorUnits" = COALESCE(ts."CurrencyMinorUnits", 2)
  FROM "TenantSetting" ts
 WHERE ts."TenantID" = i."TenantID"
   AND i."CurrencyCode" IS NULL;

UPDATE "Invoice"
   SET "CurrencyCode" = 'ZAR', "CurrencyMinorUnits" = 2
 WHERE "CurrencyCode" IS NULL;

ALTER TABLE "Invoice" ALTER COLUMN "CurrencyCode" SET NOT NULL;
ALTER TABLE "Invoice" ALTER COLUMN "CurrencyMinorUnits" SET NOT NULL;

ALTER TABLE "Invoice"
    ALTER COLUMN "InvoiceDate" TYPE date
    USING "InvoiceDate"::date;

-- ---------------------------------------------------------------------------
-- Per-line tax
-- ---------------------------------------------------------------------------

ALTER TABLE "InvoiceLine" ADD COLUMN IF NOT EXISTS "TaxSchemeID" smallint;
ALTER TABLE "InvoiceLine" ADD COLUMN IF NOT EXISTS "TaxRate" numeric(5,2);
ALTER TABLE "InvoiceLine" ADD COLUMN IF NOT EXISTS "TaxNote" character varying(250);

-- Existing lines have no rate. Store 0 rather than leaving a nullable column:
-- a line that predates this migration carries no tax information, and 0 is the
-- only value that cannot change its historical total.
UPDATE "InvoiceLine" SET "TaxRate" = 0 WHERE "TaxRate" IS NULL;
ALTER TABLE "InvoiceLine" ALTER COLUMN "TaxRate" SET NOT NULL;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.table_constraints
    WHERE constraint_name = 'FK_InvoiceLine_TaxScheme' AND table_name = 'InvoiceLine'
  ) THEN
    ALTER TABLE "InvoiceLine"
      ADD CONSTRAINT "FK_InvoiceLine_TaxScheme"
      FOREIGN KEY ("TaxSchemeID") REFERENCES "TaxScheme" ("ID");
  END IF;
END $$;

COMMIT;
