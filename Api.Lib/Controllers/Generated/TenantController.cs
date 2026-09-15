using Microsoft.AspNetCore.Mvc;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

/// This is auto-generated from a template. 
/// Do not modify this file directly. Instead, create a partial class in a separate file to add custom logic.
namespace _0nline.Biller.Api.Lib
{
    [Route("api/biller/[controller]")]
    public partial class TenantController : BillerBaseController<Tenant, int>
    {
        protected override ITenantService DbService { get; } 

        public TenantController(ITenantService dbService) : base(dbService) {
             DbService = dbService;
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] Tenant entity) {
            var result = await DbService.CreateAsync(entity);
            return Ok(result);
        }

        [HttpPut]
        public virtual async Task<IActionResult> Update([FromBody] Tenant entity) {
            var result = await DbService.UpdateAsync(entity);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> DeleteOrDeactivate(int id) {
            var result = await DbService.DeleteOrDeactivateAsync(id);
            return Ok(result);
        }
    }
}