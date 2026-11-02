using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces.DL;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    /// <summary>
    /// Company-to-contact links. A link is addressed by the company and the contact it
    /// joins, never by its own key, and there is no controller for this table.
    /// </summary>
    /// <remarks>
    /// Inherits <see cref="IDLLinkService{TLink,TParent,TTarget}"/> so the controller and
    /// HTTP generators recognise it and emit no top-level endpoint.
    /// </remarks>
    public interface ICompanyContactLinkService : IDLLinkService<CompanyContactLink, Company, Contact>
    {
    }
}
