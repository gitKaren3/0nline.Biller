using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    /// <summary>
    /// The tenant's billing defaults: currency, time zone and the tax rate new
    /// invoice lines start from. There is exactly one row per tenant, so the
    /// surface is a read/save pair rather than generic CRUD.
    /// </summary>
    public interface ITenantSettingService : IDLBaseService<TenantSetting>
    {
        /// <summary>
        /// Returns the caller's settings, creating the default row first when the
        /// tenant predates the table. The caller's tenant claim supplies the id.
        /// </summary>
        Task<Result<TenantSetting>> GetForTenantAsync();

        /// <summary>
        /// Creates the default settings for a tenant that has none, without requiring
        /// a tenantId claim. Called by first-login ensure before the token carries one.
        /// </summary>
        Task<Result<TenantSetting>> EnsureForTenantAsync(int tenantId, long? userId);

        /// <summary>
        /// Inserts or updates the caller's single settings row. The tenant id comes
        /// from the token and the body cannot move the row.
        /// </summary>
        Task<Result<TenantSetting>> SaveAsync(TenantSetting setting);
    }
}
