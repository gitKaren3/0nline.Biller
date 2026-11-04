using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;


namespace _0nline.Biller.DL.Contract.Interfaces
{
    public interface ICompanyService : IDLBaseService<Company>, IDLReadableService<Company>, IDLWritableService<Company>
    {
        /// <summary>
        /// The tenant's clients: every company it owns except its own profile.
        /// Each row carries its billing contact, so a list does not show a bare id.
        /// </summary>
        /// <remarks>
        /// A default fallback keeps existing implementations source-compatible: a
        /// service that has not adopted the client report still answers with the
        /// table read instead of becoming an abstract type.
        /// </remarks>
        async Task<Result<Company[]>> ListClientsAsync() => await GetAllAsync();
    }
}
