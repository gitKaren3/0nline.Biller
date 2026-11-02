using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Contract.Models.db
{
    /// <summary>
    /// The many-to-many link between a company and its contacts. The pair is the
    /// identity; there is no key to address the row by.
    /// </summary>
    public partial class CompanyContactLink : IDbLinkEntity<Company, Contact>
    {
    }
}
