using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    public interface IInvoiceService : IDLBaseService<Invoice>, IDLReadableService<Invoice>, IDLWritableService<Invoice>
    {
        /// <summary>
        /// Finalizes a draft. A finalized invoice can no longer be edited and is
        /// the state from which a link can be sent.
        /// </summary>
        Task<Result<Invoice>> FinalizeAsync(int id);

        /// <summary>
        /// Renders the current saved invoice as a PDF without creating a snapshot
        /// and without recording an access. Used by the editor's preview.
        /// </summary>
        Task<Result<byte[]>> PreviewAsync(int id);
    }
}
