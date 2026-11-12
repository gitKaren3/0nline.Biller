using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Services
{
    /// <summary>
    /// Rates are captured records. A mistake is corrected by deleting and recapturing
    /// while nothing references the row; once an invoice line points at it, the FK
    /// stops a hard delete and <see cref="DeleteOrDeactivateAsync"/> archives it.
    /// There is deliberately no update path.
    /// </summary>
    public class TenantRateService : BillerBaseService<TenantRate>, ITenantRateService, IDLReadableService<TenantRate>, IDLWritableService<TenantRate>
    {
        public TenantRateService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<TenantRate> queryService, ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }

        public new Task<Result<TenantRate[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new async Task<Result<TenantRate>> CreateAsync(TenantRate entity)
        {
            if (entity.RateTypeID <= 0)
                return Result<TenantRate>.Failure(
                    ErrorCode.ValidationFailed, "Choose a rate type.");

            if (entity.RateIncl is null or < 0)
                return Result<TenantRate>.Failure(
                    ErrorCode.ValidationFailed, "Enter a rate that is zero or more.");

            entity.Note = string.IsNullOrWhiteSpace(entity.Note) ? null : entity.Note.Trim();
            entity.CapturedOn = DateTime.UtcNow;
            entity.Active = true;

            return await base.CreateAsync(entity);
        }

        public new Task<Result<TenantRate>> UpdateAsync(TenantRate entity)
            => Task.FromResult(Result<TenantRate>.Failure(
                ErrorCode.ValidationFailed,
                "A captured rate cannot be edited. Delete it and capture a new rate instead."));

        public new Task<Result<TenantRate?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
    }
}
