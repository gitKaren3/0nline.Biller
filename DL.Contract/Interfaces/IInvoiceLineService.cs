using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces.DL;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    /// <summary>
    /// Child table access. A line has no TenantID, so it must be reached through the
    /// invoice that was already tenant-filtered; there is no by-id operation and no
    /// controller for this table.
    /// </summary>
    /// <remarks>
    /// Inherits <see cref="IDLChildService{TChild,TParent}"/> rather than
    /// <c>IDLBaseService</c>, <c>IDLReadableService</c> or <c>IDLWritableService</c>, so
    /// the controller and HTTP generators recognise it and emit no top-level endpoint.
    /// Do not expose this service by its own key.
    /// </remarks>
    public interface IInvoiceLineService : IDLChildService<InvoiceLine, Invoice>
    {
    }
}
