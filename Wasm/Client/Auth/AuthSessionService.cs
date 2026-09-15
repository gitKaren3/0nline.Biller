using _0nline.Shared.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using Microsoft.Extensions.DependencyInjection;

namespace _0nline.Biller.Wasm.Client.Auth
{
    public class AuthSessionService
    {
        private readonly UserContext _userContext;
        private readonly NavigationManager _nav;
        private readonly ISnackbar _snackbar;
        private readonly IServiceProvider _serviceProvider;



        public AuthSessionService(
            UserContext userContext,
            IServiceProvider serviceProvider,
            NavigationManager nav,
            ISnackbar snackbar)
        {
            _userContext = userContext;
            _serviceProvider = serviceProvider;
            _nav = nav;
            _snackbar = snackbar;
        }

        private JwtAuthStateProvider AuthProvider =>
            _serviceProvider.GetRequiredService<JwtAuthStateProvider>();

        public async Task HandleSessionExpiredAsync(string message)
        {
            _snackbar.Add(message, Severity.Warning);

            await LogoutAsync();

            await Task.Delay(500);
            _nav.NavigateTo("/login");
        }

        public async Task HandleTenantRequiredAsync(string message)
        {
            _snackbar.Add(message, Severity.Info);

            await Task.Delay(500);
            _nav.NavigateTo("/tenant");
        }

        public async Task LogoutAsync()
        {
            _userContext.Clear();
            await AuthProvider.NotifyUserLogoutAsync();
        }
    }
}
