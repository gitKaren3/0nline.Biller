using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Contract.Models.db
{
    /// <summary>
    /// Rate types are reference data: a tenant rate points at one, but saving the
    /// rate must not insert or update the referenced type. They are deliberately
    /// not an enum, because users may add their own rate types later.
    /// </summary>
    public partial class RateType : IDbLookupEntity
    {
    }
}
