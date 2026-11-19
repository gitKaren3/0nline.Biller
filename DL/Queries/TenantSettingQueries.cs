using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;

namespace _0nline.Biller.DL.Queries
{
    /// <summary>
    /// The settings row read that the entity store cannot address: it is keyed by
    /// tenant, not by its own id.
    /// </summary>
    public interface ITenantSettingQueries
    {
        /// <summary>
        /// The settings row for a tenant, or null when it does not exist yet.
        /// </summary>
        /// <remarks>
        /// The tenant id is a parameter rather than the claim on purpose: the first-login
        /// ensure runs before the request carries a tenant claim, and it is the caller
        /// that knows which tenant it is creating rows for.
        /// </remarks>
        Task<TenantSetting?> GetForTenantAsync(int tenantId);
    }

    public sealed class TenantSettingQueries : DbSession, ITenantSettingQueries
    {
        public TenantSettingQueries(
            IDbContextProvider<BillerDbConfig> ctx,
            ITenantContext tenancyContext)
            : base(ctx, tenancyContext)
        {
        }

        public async Task<TenantSetting?> GetForTenantAsync(int tenantId)
        {
            // @ForTenantId, not @TenantId: the session binds @TenantId from the claim,
            // and this read has to work while there is no claim to bind.
            var sql = $@"SELECT *
                           FROM {Quoted("TenantSetting")}
                          WHERE {Quoted("TenantID")} = @ForTenantId";

            var rows = await QueryAsync<TenantSetting>(sql, new { ForTenantId = tenantId });
            return rows.FirstOrDefault();
        }
    }
}
