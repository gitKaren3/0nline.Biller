using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
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

        public new Task<Result<Tenant>> CreateAsync(Tenant entity)
        {
            entity.Tier = null; // Tier should not be updated.
            return base.CreateAsync(entity); 
        }

        public new Task<Result<Tenant?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
        public new Task<Result<Tenant>> UpdateAsync(Tenant entity) 
        { 
            entity.Tier = null; // Tier should not be updated.
            return base.UpdateAsync(entity); 
        }

        public Task<Result<Tenant>> CreateOrUpdateAsync(Tenant entity)
        {  
            entity.Tier = null; // Tier should not be updated.
            return PostAndHandleAsync<Tenant, Tenant>("createorupdate", entity); 
        }

        public Task<Result<TenantRegistrationResult>> RegisterAsync(Tenant entity)
        { 
            entity.Tier = null; // Tier should not be updated.
            return PostAndHandleAsync<Tenant, TenantRegistrationResult>("register", entity);
        }

        public Task<Result<TenantRegistrationResult>> EnsureAsync(string? currentToken = null)
            => PostAndHandleAsync<object, TenantRegistrationResult>("ensure", new { });

        public Task<Result<Tenant>> AttachProfileAsync(int? contactId, int? companyId)
            => PostAndHandleAsync<TenantProfileRequest, Tenant>(
                "attach", new TenantProfileRequest { ContactID = contactId, CompanyID = companyId });

        public async Task<Result> CloseAsync(int tenantId)
            => await PostAndHandleAsync<object, object>($"{tenantId}/close", new { });

        public Task<Result> PurgeAsync(int tenantId)
            => throw new NotSupportedException(
                "Purge is a privileged back-office operation; there is no client endpoint.");
    }
}
