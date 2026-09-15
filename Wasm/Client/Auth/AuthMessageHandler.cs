using _0nline.Auth.Contract;
using _0nline.Shared.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using System.Net;

namespace _0nline.Biller.Wasm.Client.Auth
{
    public class AuthMessageHandler : DelegatingHandler
    {
        private readonly ILocalStorageService _localStorage;
        private readonly IAuthenticationService _authService;
        private readonly UserSession _userSession;

        public AuthMessageHandler(ILocalStorageService localStorage, 
            IAuthenticationService AuthService,
            UserSession userSession)
        {
            _localStorage = localStorage;
            _authService = AuthService;
            _userSession = userSession;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
      
            if (!string.IsNullOrEmpty(token))
            {
                var expiresAt = await _localStorage.GetItemAsync<DateTime>("expiresAt");

                if (DateTime.UtcNow >= expiresAt)
                {
                    // Token already expired → clear storage and redirect
                    await _localStorage.RemoveItemAsync("authToken");
                    await _localStorage.RemoveItemAsync("expiresAt");

                    _userSession.RaiseSessionExpired("Your session has expired.");

                    // Optionally return Unauthorized so the pipeline stops
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
                else if(DateTime.UtcNow >= expiresAt.AddMinutes(-5))
                {
                    // Call auth API with current token to renew
                    var result = await _authService.RenewTokenAsync(token);
                    if (result != null && result.Successful && result.Value is AuthResult)
                    {
                        AuthResult authResult = result.Value;
                        string newToken = authResult.Token ?? string.Empty;
                        await _localStorage.SetItemAsync("authToken", newToken);
                        await _localStorage.SetItemAsync("expiresAt", authResult.ExpiresAt);
                        token = newToken;
                    }

                }
                
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
