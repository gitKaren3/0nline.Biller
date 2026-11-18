using System.Data;

namespace _0nline.Biller.DL.Numbering
{
    /// <summary>
    /// The tenant's invoice-number counter.
    /// </summary>
    /// <remarks>
    /// The counter is a <c>TenantSetting</c> column, not an invoice column, so it has
    /// its own type. It is claimed inside the caller's transaction, which is what makes
    /// a failed create leave the number unconsumed. Rendering the number and skipping
    /// ones already issued are invoice rules, and stay in <c>InvoiceService</c>.
    ///
    /// It lives in <c>DL</c>, not <c>DL.Contract</c>: the members take a connection and
    /// transaction so the claim joins the caller's unit of work, and nothing exposes the
    /// counter over HTTP. <c>DL.Contract</c> holds what the client implements.
    /// </remarks>
    public interface IInvoiceNumberAllocator
    {
        /// <summary>
        /// Claims the next counter value inside the caller's transaction. Null when the
        /// tenant has no settings row, which is the "complete your settings" case.
        /// </summary>
        Task<InvoiceNumberClaim?> ClaimAsync(IDbConnection conn, IDbTransaction tran);

        /// <summary>
        /// Moves the counter forward to at least <paramref name="used"/> + 1, so an
        /// automatic number continues after one the tenant typed. A number below the
        /// counter leaves it alone: reusing a live number is the unique index's job.
        /// </summary>
        Task AdvanceAsync(IDbConnection conn, IDbTransaction tran, int used);
    }

    /// <summary>One claimed counter value, with the format to render it with.</summary>
    public readonly record struct InvoiceNumberClaim(string? Format, int Number);
}
