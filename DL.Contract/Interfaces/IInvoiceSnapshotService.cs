using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    /// <summary>
    /// Snapshots are addressed through their invoice while signed in, and through
    /// their token when anonymous. Deliberately a child service so the controller
    /// generator emits no top-level CRUD surface for the table.
    /// </summary>
    public interface IInvoiceSnapshotService : IDLChildService<InvoiceSnapshot, Invoice>
    {
        /// <summary>
        /// Freezes the invoice as it stands now. Unless <paramref name="regenerate"/>
        /// is set, an existing live snapshot is returned instead, so the same link can
        /// be re-sent without invalidating the previous one. Regenerating creates a new
        /// snapshot and leaves the old link valid until it is revoked explicitly.
        /// </summary>
        Task<Result<InvoiceSnapshot>> CreateForInvoiceAsync(int invoiceId, bool regenerate = false);

        /// <summary>The live snapshot for a token, or none when it is unknown or revoked.</summary>
        Task<Result<InvoiceSnapshot?>> GetByTokenAsync(Guid token);

        /// <summary>Renders the snapshot behind a token; the caller never sees the model.</summary>
        Task<Result<byte[]>> RenderPdfAsync(Guid token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Records that the public link served the PDF. A failure here is logged
        /// and swallowed: telemetry must never deny the document.
        /// </summary>
        Task RecordAccessAsync(Guid token, CancellationToken cancellationToken = default);

        /// <summary>
        /// The delivery evidence for an invoice: the live snapshot when there is
        /// one, otherwise the most recent snapshot. Tenant-scoped through the invoice.
        /// </summary>
        Task<Result<InvoiceSnapshotActivity>> GetActivityAsync(int invoiceId);

        /// <summary>
        /// Revokes a link. Scoped through the snapshot's invoice, because the child
        /// table has no tenant column of its own.
        /// </summary>
        Task<Result<InvoiceSnapshot?>> RevokeAsync(int snapshotId);
    }
}
