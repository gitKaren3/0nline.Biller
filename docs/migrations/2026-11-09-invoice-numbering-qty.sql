-- Fractional quantities and per-tenant invoice numbering.
--
-- Design notes:
--   * "InvoiceLine.Qty" was smallint, which cannot express half or quarter hours.
--     It becomes numeric(9,2): quantities still render without trailing noise, and
--     the line's tax is still extracted from a tax-inclusive amount exactly as before.
--   * Numbering is per tenant and sequential. "TenantSetting.NextInvoiceNumber" is the
--     next number to assign; the invoice service increments it atomically when it
--     creates an invoice with a blank number. A tenant that has already invoiced
--     elsewhere raises this once in Settings so the next invoice continues from there.
--   * "InvoiceNumberPrefix" is the optional text printed in front of the number
--     (for example "INV-"). The default is empty, so a tenant that wants bare
--     numbers gets "84", and one that wants "INV-84" sets the prefix.
--   * (TenantID, InvoiceNumber) is unique: a number can never be saved twice.
--     The service checks before writing to return a friendly message, and this
--     index is the guarantee under concurrency.

BEGIN;

-- ---------------------------------------------------------------------------
-- Fractional quantities
-- ---------------------------------------------------------------------------

ALTER TABLE "InvoiceLine"
    ALTER COLUMN "Qty" TYPE numeric(9, 2) USING "Qty"::numeric(9, 2);

-- ---------------------------------------------------------------------------
-- Per-tenant invoice numbering
-- ---------------------------------------------------------------------------

ALTER TABLE "TenantSetting"
    ADD COLUMN IF NOT EXISTS "InvoiceNumberPrefix" character varying(20) NOT NULL DEFAULT '';

ALTER TABLE "TenantSetting"
    ADD COLUMN IF NOT EXISTS "NextInvoiceNumber" integer NOT NULL DEFAULT 1;

-- Continue after the highest purely numeric invoice number already issued, so a
-- tenant that has been invoicing elsewhere is not restarted at 1.
UPDATE "TenantSetting" ts
   SET "NextInvoiceNumber" = mx.max_num + 1
  FROM (
        SELECT "TenantID",
               MAX("InvoiceNumber"::integer) AS max_num
          FROM "Invoice"
         WHERE "InvoiceNumber" ~ '^[0-9]+$'
         GROUP BY "TenantID"
       ) mx
 WHERE mx."TenantID" = ts."TenantID"
   AND ts."NextInvoiceNumber" = 1;

-- ---------------------------------------------------------------------------
-- Invoice numbers are unique within a tenant
-- ---------------------------------------------------------------------------

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_indexes WHERE indexname = 'UX_Invoice_Tenant_Number'
  ) THEN
    CREATE UNIQUE INDEX "UX_Invoice_Tenant_Number"
        ON "Invoice" ("TenantID", "InvoiceNumber");
  END IF;
END $$;

COMMIT;
