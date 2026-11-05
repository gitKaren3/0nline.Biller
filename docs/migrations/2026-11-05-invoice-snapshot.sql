-- Invoice snapshots: the invoice as it was when it was issued.
--
-- The rendered PDF is deliberately NOT stored. A snapshot keeps the render model
-- as text, so the PDF can be regenerated on demand and a layout change does not
-- rewrite history. It is also what an emailed link can serve anonymously.
--
-- "Token" is the capability that opens the public PDF endpoint. It is a GUID
-- because the link travels by email and must be unguessable, not enumerable. The
-- token belongs to the snapshot rather than to "Invoice": revoking a link must
-- not affect the invoice, and re-issuing a link must not change the old one.
--
-- There is no "TenantID". Like "InvoiceLine", a snapshot is reached through its
-- invoice, which is already tenant-filtered; the public route addresses it only
-- by token. See docs/tenancy.md.

BEGIN;

CREATE TABLE IF NOT EXISTS "InvoiceSnapshot"
(
    "ID"            integer                  GENERATED ALWAYS AS IDENTITY,
    "InvoiceID"     integer                  NOT NULL,
    "Token"         uuid                     NOT NULL DEFAULT gen_random_uuid(),
    "LayoutVersion" smallint                 NOT NULL DEFAULT 1,
    -- The render model as JSON. Text rather than jsonb: the snapshot is written
    -- and read whole, and a text parameter binds without an explicit cast.
    "Snapshot"      text                     NOT NULL,
    "CreatedAt"     timestamp with time zone NOT NULL DEFAULT now(),
    "RevokedAt"     timestamp with time zone,
    CONSTRAINT "InvoiceSnapshot_pkey" PRIMARY KEY ("ID"),
    CONSTRAINT "FK_InvoiceSnapshot_Invoice"
        FOREIGN KEY ("InvoiceID") REFERENCES "Invoice" ("ID") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "UX_InvoiceSnapshot_Token"
    ON "InvoiceSnapshot" ("Token");

CREATE INDEX IF NOT EXISTS "IX_InvoiceSnapshot_InvoiceID"
    ON "InvoiceSnapshot" ("InvoiceID");

COMMIT;
