using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Services
{
    public class TenantRateService : BillerBaseService<TenantRate>, ITenantRateService, IDLReadableService<TenantRate>, IDLWritableService<TenantRate>
    {
        public TenantRateService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<TenantRate> queryService, ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }

        public new Task<Result<TenantRate[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<TenantRate>> CreateAsync(TenantRate entity) => base.CreateAsync(entity);
        public new Task<Result<TenantRate>> UpdateAsync(TenantRate entity) => base.UpdateAsync(entity);
        public new Task<Result<TenantRate?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
    }
}