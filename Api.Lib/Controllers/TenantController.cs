using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace _0nline.Biller.Api.Lib
{
    public partial class TenantController
    {
        private ITenantService TenantService => BaseService;

        /// <summary>
        /// Idempotent first-login step. The user id comes from the validated token,
        /// never from the body. Returns a refreshed token when the caller's token
        /// does not yet carry the tenantId claim.
        /// </summary>
        [HttpPost("ensure")]
        public virtual async Task<IActionResult> Ensure()
        {
            var result = await TenantService.EnsureAsync(GetBearerToken());
            return Ok(result);
        }

        /// <summary>
        /// Points the tenant at its own contact and company. The service verifies
        /// that both rows belong to the caller's tenant.
        /// </summary>
        [HttpPost("attach")]
        public virtual async Task<IActionResult> Attach([FromBody] TenantProfileRequest entity)
        {
            var result = await TenantService.AttachProfileAsync(entity?.ContactID, entity?.CompanyID);
            return Ok(result);
        }

        /// <summary>
        /// Detaches the user and deactivates the workspace. Invoices are retained.
        /// </summary>
        [HttpPost("{id:int}/close")]
        public virtual async Task<IActionResult> Close(int id)
        {
            var result = await TenantService.CloseAsync(id);
            return Ok(result);
        }

        /// <summary>
        /// Destructive purge for retention/erasure requests. Owner-only.
        /// </summary>
        // Intentionally no HTTP route: a hard purge is a privileged back-office
        // job, not something a tenant (or an autonomous agent) can call.
        // The ordered implementation lives in TenantService.PurgeAsync.

        /// <summary>
        /// The tenant table has no TenantID, so the generated read is unscoped.
        /// Restrict it to the caller's own tenant.
        /// </summary>
        public override async Task<IActionResult> GetById(int id)
        {
            var result = await TenantService.GetOneAsync(id);

            if (result.Successful && result.Value is Tenant tenant)
            {
                // Fail closed if the token has no user id. A closed tenant has
                // UserId = NULL, which must not match a NULL caller.
                var signedIn = GetSignedInUserId();
                if (signedIn is not long userId
                    || tenant.UserId != userId
                    || tenant.Active == false)
                {
                    return Ok(Result<Tenant?>.Failure(
                        ErrorCode.Unauthorized, "You may only read your own tenant."));
                }
            }

            return Ok(result);
        }

        [HttpPost("createorupdate")]
        public virtual async Task<IActionResult> CreateOrUpdateAsync([FromBody] Tenant entity)
        {
            // Identity and ownership in the body are ignored by the service.
            var result = await TenantService.CreateOrUpdateAsync(entity);
            return Ok(result);
        }

        [HttpGet("foruser/{userid}")]
        public virtual async Task<IActionResult> GetByUserId(long userid)
        {
            // The URL id must be the caller. Onboarding no longer needs this lookup:
            // ensure resolves the tenant from the token.
            if (GetSignedInUserId() != userid)
                return Ok(Result<Tenant?>.Failure(
                    ErrorCode.Unauthorized, "You may only read your own tenant."));

            var result = await TenantService.GetTenantByUserIdAsync(userid);
            return Ok(result);
        }

        [HttpPost("register")]
        public virtual async Task<IActionResult> Register([FromBody] Tenant entity)
        {
            // The body is ignored; registration is the ensure step.
            var result = await TenantService.EnsureAsync(GetBearerToken());
            return Ok(result);
        }

        private long? GetSignedInUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("sub")?.Value;

            return long.TryParse(value, out long id) ? id : null;
        }

        private string? GetBearerToken()
        {
            var value = Request.Headers.Authorization.ToString();
            const string prefix = "Bearer ";

            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? value[prefix.Length..].Trim()
                : null;
        }
    }
}
