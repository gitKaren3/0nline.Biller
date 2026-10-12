using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    public partial class TenantController
    {
        private ITenantService TenantService => BaseService;


        [HttpPost("createorupdate")]
        public virtual async Task<IActionResult> CreateOrUpdateAsync([FromBody] Tenant entity)
        {
            var result = await TenantService.CreateOrUpdateAsync(entity);
            return Ok(result);
        }


        [HttpGet("foruser/{userid}")]
        public virtual async Task<IActionResult> GetByUserId(long userid)
        {
            var result = await TenantService.GetTenantByUserIdAsync(userid);
            return Ok(result);
        }

        [HttpPost("register")]
        public virtual async Task<IActionResult> Register([FromBody] Tenant entity)
        {
            var result = await TenantService.RegisterAsync(entity);
            return Ok(result);
        }
    }
}