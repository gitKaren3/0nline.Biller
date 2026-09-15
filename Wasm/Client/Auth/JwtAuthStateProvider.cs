using _0nline.Shared.Client.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace _0nline.Biller.Wasm.Client.Auth
{
    public class JwtAuthStateProvider : AuthenticationStateProvider
    {
        private readonly ILocalStorageService _localStorage;
        private readonly UserSession _userSession;

        public JwtAuthStateProvider(ILocalStorageService localStorage, UserSession userSession)
        {
            _localStorage = localStorage;
            _userSession = userSession;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");

            if (string.IsNullOrEmpty(token))
            {
                var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
                return new AuthenticationState(anonymous);
            }

            // Parse JWT into claims
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            PopulateUserSession(user);

            return new AuthenticationState(user);
        }

        public async Task NotifyUserAuthenticationAsync(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwt = handler.ReadJwtToken(token);

            await _localStorage.SetItemAsync("authToken", token);
            await _localStorage.SetItemAsync("expiresAt", jwt.ValidTo);

            var identity = new ClaimsIdentity(jwt.Claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            PopulateUserSession(user);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }

        public async Task NotifyUserLogoutAsync()
        {
            // 1. Wipe the session state immediately
            _userSession.Clear();

            // 2. Clear the token from storage
            await _localStorage.RemoveItemAsync("authToken");
            await _localStorage.RemoveItemAsync("expiresAt");

            // 3. Tell Blazor the user is now anonymous
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
        }

        private void PopulateUserSession(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst("sub");
            var tenantIdClaim = user.FindFirst("tenantId");
            var appIdClaim = user.FindFirst("app");
            if (userIdClaim != null && Int64.TryParse(userIdClaim.Value, out long userId))
            {
                int tenantId = 0;
                if (tenantIdClaim != null)
                {
                    int.TryParse(tenantIdClaim.Value, out tenantId);
                }
                string appId = appIdClaim?.Value ?? string.Empty;
                _userSession.Initialize(userId, appId, tenantId);
            }
        }
    }

}
