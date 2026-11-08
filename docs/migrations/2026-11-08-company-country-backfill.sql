-- Backfill the ISO country code on existing companies.
--
-- Biller's original company form stored only the display name. The currency and
-- tax-number checks need the ISO code, and the invoice prints the canonical name.
-- This handles the two spellings already present in the dev database; new rows are
-- written with both fields by CountryField.

BEGIN;

UPDATE "Company"
   SET "CountryCode" = 'ZA',
       "AddressCountry" = 'South Africa'
 WHERE "CountryCode" IS NULL
   AND "AddressCountry" IN ('South Africa', 'S Africa');

COMMIT;
