using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Contract.Models.db
{
    /// <summary>
    /// An invoice line belongs to its invoice. It has no TenantID of its own, so it is
    /// only reachable through an invoice that was already tenant-filtered.
    /// </summary>
    /// <remarks>
    /// The generated half of this class is in <c>Generated/InvoiceLine.cs</c>; this part
    /// carries the hand-written declaration that the row is a child.
    /// </remarks>
    public partial class InvoiceLine : IDbChildEntity<Invoice>
    {
    }
}
