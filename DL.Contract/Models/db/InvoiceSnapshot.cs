using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Contract.Models.db
{
    /// <summary>
    /// A snapshot belongs to its invoice. It has no API surface of its own while
    /// signed in; the only other way in is the public token, never the id.
    /// </summary>
    public partial class InvoiceSnapshot : IDbChildEntity<Invoice>
    {
    }
}
