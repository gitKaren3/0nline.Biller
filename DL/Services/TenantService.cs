using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using Dapper;


namespace _0nline.Biller.DL.Services
{
    public class TenantService : BillerBaseService<Tenant>, ITenantService, IDLWritableService<Tenant>
    {
        public TenantService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<Tenant> queryService, ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }
  
        public new Task<Result<Tenant>> CreateAsync(Tenant entity) => base.CreateAsync(entity);
        public new Task<Result<Tenant>> UpdateAsync(Tenant entity) => base.UpdateAsync(entity);
        public new Task<Result<Tenant?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        public async Task<Result<Tenant>> CreateOrUpdateAsync(Tenant tenant)
        {
            using var conn = Connection;
            if (tenant.ID > 0)
            {
                return await UpdateAsync(tenant);
            }
            else if (tenant.UserId.HasValue)
            {
                var result = await GetTenantByUserIdAsync(tenant.UserId.Value);
                if (result.Successful && result.Value != null) {
                    var existingTenant = result.Value;
                    tenant.ID = existingTenant.ID;
                    tenant.ContactID = existingTenant.ContactID;
                    tenant.CompanyID = existingTenant.CompanyID;
                    return await UpdateAsync(tenant);
                }
            }
            return await CreateAsync(tenant);
        }

        public async Task<Result<Tenant?>> GetTenantByUserIdAsync(long userId)
        {             
            using var conn = Connection;
            string sql = $"SELECT * FROM {Quoted(_tableName)} WHERE {Quoted("UserId")} = @UserId";
            
            var entity = await conn.QueryFirstOrDefaultAsync<Tenant>(sql, new { UserId = userId });
            if (entity != null)
            { 
                await LoadNavigationPropertiesAsync(entity, conn);
                return Result<Tenant?>.Success(entity);
            }
            else
                return Result<Tenant?>.None();
        }
    }
}
