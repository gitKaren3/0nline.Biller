-- An invoice line is a child of its invoice: it has no independent existence and no
-- TenantID of its own. The original FK had no ON DELETE action, so deleting a draft
-- invoice with lines was rejected by the database ("still referenced from InvoiceLine")
-- and the service's delete-or-deactivate fallback returned an empty result.
--
-- "InvoiceSnapshot" already cascades from "Invoice"; this makes "InvoiceLine" match it,
-- so a draft delete removes the invoice and its lines in one statement.

BEGIN;

ALTER TABLE "InvoiceLine" DROP CONSTRAINT IF EXISTS "FK_InvoiceLine_Invoice";

ALTER TABLE "InvoiceLine"
    ADD CONSTRAINT "FK_InvoiceLine_Invoice"
        FOREIGN KEY ("InvoiceID") REFERENCES "Invoice" ("ID") ON DELETE CASCADE;

COMMIT;
