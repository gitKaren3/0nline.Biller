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
    ///
    /// <see cref="RateTypeID"/> is the category the user chose and may be null.
    /// <see cref="TenantRateID"/> is optional provenance: when it is set, the line was
    /// priced from that captured rate and must still equal its amount and type. A
    /// manually overridden price leaves it null.
    /// </remarks>
    public partial class InvoiceLine : IDbChildEntity<Invoice>
    {
    }
}
