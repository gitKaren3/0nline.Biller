using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    public partial class TenantSettingController
    {
        private ITenantSettingService TenantSettingService => BaseService;

        /// <summary>
        /// The caller's single settings row. The tenant id comes from the token; the
        /// response never includes another tenant's row because the read is scoped.
        /// </summary>
        [HttpGet("mine")]
        public virtual async Task<IActionResult> GetMine()
        {
            var result = await TenantSettingService.GetForTenantAsync();
            return Ok(result);
        }

        /// <summary>
        /// Inserts or updates the caller's row. The body id and tenant id are ignored;
        /// the service writes the tenant named by the token.
        /// </summary>
        [HttpPut]
        public virtual async Task<IActionResult> Save([FromBody] TenantSetting entity)
        {
            var result = await TenantSettingService.SaveAsync(entity);
            return Ok(result);
        }
    }
}
