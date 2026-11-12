-- Rates are captured records, not maintained prices.
--
-- A rate is immutable once captured. Whether it is still offered is the Active
-- flag: false means archived. An invoice line may point at the captured rate it
-- was priced from (TenantRateID, now nullable) or carry only the chosen rate
-- type and a manually entered price.
--
-- StartDate/EndDate are gone: there is no effective-date window. CapturedOn is
-- the audit instant, and the newest captured rate wins when two rates have the
-- same scope. Note is an optional free-form caption for otherwise-similar rows.

BEGIN;

ALTER TABLE "TenantRate" RENAME COLUMN "DefaultRate" TO "RateIncl";

ALTER TABLE "TenantRate"
    ADD COLUMN "CapturedOn" timestamp with time zone NOT NULL DEFAULT now();

ALTER TABLE "TenantRate"
    ADD COLUMN "Note" character varying(500) NULL;

-- The old StartDate is the closest thing we have to a capture date.
UPDATE "TenantRate"
   SET "CapturedOn" = "StartDate"::timestamp with time zone
 WHERE "StartDate" IS NOT NULL;

ALTER TABLE "TenantRate" DROP COLUMN "EndDate";
ALTER TABLE "TenantRate" DROP COLUMN "StartDate";

-- A line names the rate type even when the price was typed by hand.
ALTER TABLE "InvoiceLine"
    ADD COLUMN "RateTypeID" smallint NULL;

UPDATE "InvoiceLine" il
   SET "RateTypeID" = tr."RateTypeID"
  FROM "TenantRate" tr
 WHERE tr."ID" = il."TenantRateID";

-- The source rate is optional now. The FK stays restrictive: a rate referenced
-- by any line cannot be hard-deleted, so DeleteOrDeactivate archives it.
ALTER TABLE "InvoiceLine"
    ALTER COLUMN "TenantRateID" DROP NOT NULL;

ALTER TABLE "InvoiceLine"
    ADD CONSTRAINT "FK_InvoiceLine_RateType"
        FOREIGN KEY ("RateTypeID") REFERENCES "RateType" ("ID");

-- Generic categories for work that has no more specific type.
INSERT INTO "RateType" ("Name")
SELECT 'Service'
 WHERE NOT EXISTS (SELECT 1 FROM "RateType" WHERE "Name" = 'Service');

INSERT INTO "RateType" ("Name")
SELECT 'Ad-hoc'
 WHERE NOT EXISTS (SELECT 1 FROM "RateType" WHERE "Name" = 'Ad-hoc');

COMMIT;
