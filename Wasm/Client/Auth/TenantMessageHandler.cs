using _0nline.Shared.Client.Services;
using System.Net;

namespace _0nline.Biller.Wasm.Client.Auth
{
    public class TenantMessageHandler : DelegatingHandler
    {
        private readonly UserSession _userSession;

        public TenantMessageHandler(UserSession userSession)
        {
            _userSession = userSession;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {

            if (request.RequestUri == null
               //// 1. Skip tenant check for public endpoints or tenant-selection endpoints
               //// Adjust the path check based on your actual API routes:
               //   || request.RequestUri.AbsolutePath.Contains("/api/auth")
               //   || request.RequestUri.AbsolutePath.Contains("/api/tenants/list")
               //// commented out because attaching only to specific services
               )
            {
                return await base.SendAsync(request, cancellationToken);
            }

            // 2. Check UserSession for the tenant
            if (_userSession.TenantId == 0)
            {
                // Trigger the event that MainLayout is listening to
                _userSession.RaiseTenantRequired("Please complete the registration process to continue.");

                // Stop the request from hitting the server to avoid unnecessary 400/500 errors
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Tenant ID missing")
                };
            }

            //commented out as it could be hacked:
            //request.Headers.Add("X-Tenant-Id", _userSession.TenantId.ToString());

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
