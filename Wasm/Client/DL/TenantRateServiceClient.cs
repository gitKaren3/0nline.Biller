using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class TenantRateServiceClient : DLHttpClient<TenantRate>, ITenantRateService
    {
        public TenantRateServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "TenantRate")
        {
        }

        public new Task<Result<TenantRate[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<TenantRate>> CreateAsync(TenantRate entity) => base.CreateAsync(entity);
        public new Task<Result<TenantRate?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
        public new Task<Result<TenantRate>> UpdateAsync(TenantRate entity) => base.UpdateAsync(entity);
    }
}