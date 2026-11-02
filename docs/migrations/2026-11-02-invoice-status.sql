-- Invoice status lookup (Draft, Finalized, Issued, Paid, Cancelled).
--
-- Global lookup, not tenant data: same for every tenant. Stable keys (no
-- identity) so the values are addressable by their well-known IDs.

BEGIN;

CREATE TABLE IF NOT EXISTS "InvoiceStatus"
(
    "ID"   smallint NOT NULL,
    "Name" character varying(50) NOT NULL,
    CONSTRAINT "InvoiceStatus_pkey" PRIMARY KEY ("ID")
);

INSERT INTO "InvoiceStatus" ("ID", "Name") VALUES
    (1, 'Draft'),
    (2, 'Finalized'),
    (3, 'Issued'),
    (4, 'Paid'),
    (5, 'Cancelled')
ON CONFLICT ("ID") DO NOTHING;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.table_constraints
    WHERE constraint_name = 'FK_Invoice_InvoiceStatus' AND table_name = 'Invoice'
  ) THEN
    ALTER TABLE "Invoice"
      ADD CONSTRAINT "FK_Invoice_InvoiceStatus"
      FOREIGN KEY ("InvoiceStatusID") REFERENCES "InvoiceStatus"("ID");
  END IF;
END $$;

COMMIT;