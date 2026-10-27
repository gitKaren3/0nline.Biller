using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    /// <summary>
    /// Child table access. A line has no TenantID, so it must be reached through
    /// its parent invoice; <see cref="GetByInvoiceIdAsync"/> is the only read used
    /// by the API. Do not expose this service by its own key.
    /// </summary>
    /// <remarks>
    /// Lives under <c>Interfaces/Internal</c> on purpose: the controller and HTTP
    /// generators scan only the top level of <c>Interfaces</c>, so they cannot
    /// emit an <c>InvoiceLine</c> endpoint for this child table.
    /// </remarks>
    public interface IInvoiceLineService : IDLBaseService<InvoiceLine>
    {
        Task<Result<InvoiceLine[]>> GetByInvoiceIdAsync(int invoiceId);
    }
}