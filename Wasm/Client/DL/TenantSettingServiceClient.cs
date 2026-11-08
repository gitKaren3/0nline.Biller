using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class TenantSettingServiceClient : DLHttpClient<TenantSetting>, ITenantSettingService
    {
        public TenantSettingServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "tenantsetting")
        {
        }

        public Task<Result<TenantSetting>> GetForTenantAsync()
            => GetAndHandleAsync<TenantSetting>("mine");

        public Task<Result<TenantSetting>> SaveAsync(TenantSetting setting)
            => PutAndHandleAsync<TenantSetting, TenantSetting>("", setting);

        public Task<Result<TenantSetting>> EnsureForTenantAsync(int tenantId, long? userId)
            => throw new NotSupportedException(
                "Settings are created by the first-login ensure step on the server.");
    }
}
