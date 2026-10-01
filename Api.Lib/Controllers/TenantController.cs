using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    public partial class TenantController 
    {
        private ITenantService TenantService => DbService;
            
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
    }
}