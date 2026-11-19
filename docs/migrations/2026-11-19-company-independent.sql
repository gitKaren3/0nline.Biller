-- ---------------------------------------------------------------------------
-- Company identity
--
-- A company can be a registered business or an independent / sole trader. The
-- invoice layout uses this to decide whether the company name or the person's
-- name leads. Existing rows default to a registered company so their invoices
-- keep the current company-first appearance.
-- ---------------------------------------------------------------------------

ALTER TABLE "Company"
    ADD COLUMN IF NOT EXISTS "IsIndependent" boolean NOT NULL DEFAULT false;
