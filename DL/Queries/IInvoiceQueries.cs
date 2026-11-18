using System.Data;
using _0nline.Biller.DL.Contract.Models;

namespace _0nline.Biller.DL.Queries
{
    /// <summary>
    /// The invoice questions the entity store cannot express: a search across three
    /// tables, the last quantity for a client, the issuer's tax scheme, and the line
    /// rows a save has to remove.
    /// </summary>
    /// <remarks>
    /// These are invoice shapes, not generic capabilities, so they live here rather than
    /// on the shared readable surface. The implementation derives from
    /// <c>DbSession</c>, which is what keeps cancellation, retry and the tenant binding
    /// without a <c>DbConnection</c> in the service.
    ///
    /// The type lives in <c>DL</c>, not <c>DL.Contract</c>, because it is not part of the
    /// API surface: the transaction-scoped members take a connection and transaction, and
    /// no client implements it. <c>DL.Contract</c> is what the browser compiles against
    /// and what a generated controller exposes; a collaborator that only means something
    /// inside another service's unit of work is not either of those.
    /// </remarks>
    public interface IInvoiceQueries
    {
        /// <summary>
        /// The predicate and parameters behind <c>SearchPageAsync</c>. A complete
        /// predicate, ready for the store's page read.
        /// </summary>
        (string? Predicate, IReadOnlyDictionary<string, object?>? Parameters)
            SearchPredicate(InvoiceSearch search);

        /// <summary>
        /// Which of the candidate numbers are already on an invoice in this tenant.
        /// </summary>
        Task<string[]> IssuedNumbersAsync(IReadOnlyCollection<string> candidates);

        /// <summary>
        /// Whether a number is already used, inside the caller's transaction. Advisory:
        /// the save re-checks and the unique index is the guarantee.
        /// </summary>
        Task<bool> NumberTakenAsync(string number, IDbConnection conn, IDbTransaction tran);

        /// <summary>
        /// The quantity last used for this contact, company and rate type. A draft or
        /// cancelled invoice is not a precedent. Null means there is no history.
        /// </summary>
        Task<decimal?> LastQuantityAsync(
            int contactId, int? companyId, short rateTypeId, int excludeInvoiceId);

        /// <summary>
        /// The tax scheme of the tenant's own company, or null when it has not
        /// registered for tax. That is a business fact, not an error.
        /// </summary>
        Task<short?> IssuerTaxSchemeAsync();

        /// <summary>
        /// Deletes the given lines of one invoice inside the caller's transaction. The
        /// invoice id is part of the predicate, so an id from another invoice matches
        /// nothing.
        /// </summary>
        Task DeleteLinesAsync(
            int invoiceId,
            IReadOnlyCollection<int> lineIds,
            IDbConnection conn,
            IDbTransaction tran);
    }
}
