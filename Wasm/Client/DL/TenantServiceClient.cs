using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class TenantServiceClient : DLHttpClient<Tenant>, ITenantService
    {
        public TenantServiceClient(HttpClient http, IOptions<DLClientOptions> options) 
            : base(http, options, "tenant")
        {
        }

        public Task<Result<Tenant?>> GetTenantByUserIdAsync(long userId)
                => GetAndHandleAsync<Tenant?>($"foruser/{userId}");

        public new Task<Result<Tenant>> CreateAsync(Tenant entity) => base.CreateAsync(entity);
        public new Task<Result<Tenant?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
        public new Task<Result<Tenant>> UpdateAsync(Tenant entity) => base.UpdateAsync(entity);

        public Task<Result<Tenant>> CreateOrUpdateAsync(Tenant entity)
                => PostAndHandleAsync<Tenant, Tenant>("createorupdate", entity);
    }
}
