using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Models;

namespace _0nline.Biller.DL.Reports
{
    /// <summary>
    /// Builds Biller's render model from live rows and renders it. Shared by the
    /// snapshot path (issue/send) and the authenticated preview, so both always
    /// show the same document.
    /// </summary>
    public interface IInvoicePdfService
    {
        Task<InvoicePdfModel> BuildModelAsync(Invoice invoice);

        Task<Result<byte[]>> RenderAsync(Invoice invoice, CancellationToken cancellationToken = default);

        Task<Result<byte[]>> RenderAsync(InvoicePdfModel model, CancellationToken cancellationToken = default);
    }
}
