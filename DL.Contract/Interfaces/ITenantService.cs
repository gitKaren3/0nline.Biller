using _0nline.Shared.Contract.Models;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    public interface ITenantService : IDLWritableService<Tenant>
    {
        // Add public tenant-specific methods here if needed
        Task<Result<Tenant>> CreateOrUpdateAsync(Tenant entity);

        Task<Result<TenantRegistrationResult>> RegisterAsync(Tenant entity);

        Task<Result<Tenant?>> GetTenantByUserIdAsync(long userId);

        /// <summary>
        /// Creates the minimal tenant for the signed-in user if one does not
        /// exist, ensures the free-tier subscription, and returns a refreshed
        /// token when the caller's token does not yet carry the tenantId claim.
        /// </summary>
        Task<Result<TenantRegistrationResult>> EnsureAsync(string? currentToken = null);

        /// <summary>
        /// Points the tenant at its own contact and/or company. Both rows must
        /// already belong to the caller's tenant.
        /// </summary>
        Task<Result<Tenant>> AttachProfileAsync(int? contactId, int? companyId);

        /// <summary>
        /// Detaches the user and deactivates the workspace without deleting the
        /// tenant or its invoices.
        /// </summary>
        Task<Result> CloseAsync(int tenantId);

        /// <summary>
        /// Permanently removes the tenant and every row that belongs to it. This is
        /// a privileged back-office operation: it has no HTTP route and must not be
        /// exposed to a tenant or an autonomous agent without a separate policy.
        /// </summary>
        Task<Result> PurgeAsync(int tenantId);
    }
}
