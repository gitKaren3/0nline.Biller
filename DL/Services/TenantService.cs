using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Auth.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using Dapper;


namespace _0nline.Biller.DL.Services
{
    public class TenantService : BillerBaseService<Tenant>, ITenantService, IDLWritableService<Tenant>
    {
        private IAuthenticationService AuthService { get;}
        private ITierService TierService { get;  }
        public TenantService(IDbContextProvider<BillerDbConfig> ctx 
                            , ISqlQueryProvider<Tenant> queryService
                            , ITierService tierService
                            , IAuthenticationService authService
                            , ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
            AuthService = authService;
            TierService = tierService;
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
            if (tenant.TierID == 0)
            {
                var tierresult = await TierService.GetLookupAsync();
                if (tierresult.Successful && tierresult.Value is IDictionary<int, string> tiers)
                {
                    short mintier = Convert.ToInt16(tiers.Min(t => t.Key));
                    tenant.TierID = mintier;
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

        public async Task<Result<TenantRegistrationResult>> RegisterAsync(Tenant entity)
        {
            var result = await CreateOrUpdateAsync(entity);
            if (result.Successful && result.Value is Tenant savedTenant)
            {
                var subResult = await AuthService.AddUserSubscriptionAsync(
                    entity.UserId?.ToString() ?? string.Empty,
                    _tenantContext.AppId,
                    savedTenant.ID,
                    savedTenant.TierID);

                if (subResult.Successful && subResult.Value is AuthResult authResult)
                    return Result<TenantRegistrationResult>.Success(
                        new TenantRegistrationResult
                        {
                            Tenant = savedTenant,
                            Token = authResult.Token
                        }
                    );
                
                else if (subResult.IsNone)
                    return Result<TenantRegistrationResult>.Success(
                        new TenantRegistrationResult
                        {
                            Tenant = savedTenant,
                            Token = null
                        }
                    );

                else
                    // Leave the orphaned tenant record in place — the UI sees one failure.
                    return Result<TenantRegistrationResult>.Failure(subResult.ErrorCode, subResult.Errors.ToArray());
            }
            else
            {
                return Result<TenantRegistrationResult>.Failure(result.ErrorCode, result.Errors.ToArray());
            }
            
        }
    }
}
