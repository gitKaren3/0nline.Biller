using _0nline.Shared.Contract.Models;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    public interface ITenantService : IDLWritableService<Tenant>
    {
        // Add public tenant-specific methods here if needed
        Task<Result<Tenant>> CreateOrUpdateAsync(Tenant entity);

        Task<Result<Tenant?>> GetTenantByUserIdAsync(string userId);
    }
}
