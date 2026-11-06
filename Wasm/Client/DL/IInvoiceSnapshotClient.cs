using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Models;

namespace _0nline.Biller.Wasm.Client.DL
{
    /// <summary>
    /// The client-side half of the snapshot flow. It deliberately does not
    /// implement <c>IInvoiceSnapshotService</c>: rendering and access recording
    /// are server-only operations.
    /// </summary>
    public interface IInvoiceSnapshotClient
    {
        Task<Result<InvoiceSnapshotLink>> CreateLinkAsync(int invoiceId, bool regenerate = false);

        Task<Result<InvoiceSnapshotActivity>> GetActivityAsync(int invoiceId);

        Task<Result<InvoiceSnapshot?>> RevokeAsync(int snapshotId);
    }
}
