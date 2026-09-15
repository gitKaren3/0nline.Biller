using _0nline.Auth.Contract;
using _0nline.Shared.Client.Services;
using _0nline.Shared.Contract.Client.Models;
using _0nline.Shared.Contract.Models;
using Blazored.LocalStorage;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace _0nline.Biller.Wasm.Client.Auth
{
    public class BillerAuthClient : AuthClientBase
    {
        private ILocalStorageService _localStorage { get; }
        public BillerAuthClient(HttpClient http, IOptions<AuthClientOptions> options, ILocalStorageService localStorage)
           : base (http, options.Value.AppId)
        {
            Http.BaseAddress = new Uri(options.Value.ApiBaseUrl.TrimEnd('/') + '/');
            _localStorage = localStorage;
        }

        public override async Task<Result<AuthResult>> AddUserSubscriptionAsync(string userId, string appId, int tenantId, int? tierId, int subscriptionStatusId = 1)
        {
            // Manually attach the token from local storage for this specific call
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token))
            {
                Http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var result = await base.AddUserSubscriptionAsync(userId, appId, tenantId, subscriptionStatusId);
            return result;
        }

    }
}
