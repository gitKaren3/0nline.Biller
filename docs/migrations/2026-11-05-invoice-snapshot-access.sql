-- Delivery telemetry for invoice snapshots.
--
-- "Downloaded" is not observable over HTTP: the endpoint serves the PDF inline,
-- and a viewer may issue many Range requests for one open. These columns record
-- "the link was opened" instead:
--   * FirstAccessedAt is set once, on the first successful render;
--   * LastAccessedAt is refreshed on every successful render;
--   * AccessCount is incremented only outside a debounce window, so one open is
--     one count even when the viewer fetches ranges (see
--     InvoiceSnapshotService.RecordAccessAsync).
--
-- Apply 2026-11-05-invoice-snapshot.sql first.

BEGIN;

ALTER TABLE "InvoiceSnapshot"
    ADD COLUMN IF NOT EXISTS "FirstAccessedAt" timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "LastAccessedAt"  timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "AccessCount"     integer NOT NULL DEFAULT 0;

COMMIT;
