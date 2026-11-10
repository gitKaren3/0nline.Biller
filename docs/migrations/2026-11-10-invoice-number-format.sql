-- A tenant-supplied invoice number format, replacing the fixed prefix.
--
-- Design notes:
--   * "InvoiceNumberFormat" is literal text with at most one {n} token. The token
--     renders the counter; a zero run after a colon sets the width:
--
--         {n}           -> 84
--         {n:00000}     -> 00084
--         #{n:00000}    -> #00084
--         INV-{n:0000}  -> INV-0084
--         2026/{n:0000} -> 2026/0084
--
--     Everything outside a token is printed as typed, so a literal 0 (a year, for
--     instance) is not a placeholder. The width is a minimum: the number never
--     truncates, it widens.
--   * An optional {yyyy} / {y:2} / {y:4} token renders the invoice's year. It does
--     NOT reset the sequence: 2026/0084 is followed by 2027/0085 unless the tenant
--     raises the next number in Settings.
--   * The format is a generator, not a validator. A hand-typed invoice number is
--     kept as typed; (TenantID, InvoiceNumber) is still the only uniqueness rule.
--   * The old prefix maps to "{prefix}{n}", which reproduces exactly the numbers
--     that prefix produced today, so no existing invoice is renumbered.
--   * "InvoiceNumber" is varchar(30), so the format is capped at 30 characters and
--     the zero run at 9 digits (the counter is an integer). The service validates
--     both; the CHECK below is the backstop.
--   * Year tokens are accepted by the parser now; per-year resequencing is not
--     implemented. The counter remains the source of truth, so nothing has to be
--     re-derived from existing rows.

BEGIN;

-- ---------------------------------------------------------------------------
-- Prefix becomes a format
-- ---------------------------------------------------------------------------

ALTER TABLE "TenantSetting"
    ADD COLUMN IF NOT EXISTS "InvoiceNumberFormat" character varying(30) NOT NULL DEFAULT '';

UPDATE "TenantSetting"
   SET "InvoiceNumberFormat" = "InvoiceNumberPrefix" || '{n}'
 WHERE "InvoiceNumberFormat" = '';

ALTER TABLE "TenantSetting"
    ALTER COLUMN "InvoiceNumberFormat" SET DEFAULT '#{n:00000}';

ALTER TABLE "TenantSetting"
    DROP COLUMN IF EXISTS "InvoiceNumberPrefix";

-- ---------------------------------------------------------------------------
-- The format must be printable and short enough for Invoice.InvoiceNumber
-- ---------------------------------------------------------------------------

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint
     WHERE conname = 'CK_TenantSetting_InvoiceNumberFormat'
       AND conrelid = '"TenantSetting"'::regclass
  ) THEN
    ALTER TABLE "TenantSetting"
        ADD CONSTRAINT "CK_TenantSetting_InvoiceNumberFormat"
        CHECK (length("InvoiceNumberFormat") BETWEEN 1 AND 30);
  END IF;
END $$;

COMMIT;
