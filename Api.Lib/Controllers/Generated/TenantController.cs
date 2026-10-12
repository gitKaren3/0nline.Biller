using Microsoft.AspNetCore.Mvc;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Auth.Contract;

/// This is auto-generated from a template. 
/// Do not modify this file directly. Instead, create a partial class in a separate file to add custom logic.
namespace _0nline.Biller.Api.Lib
{
    [Route("api/biller/[controller]")]
    public partial class TenantController : BillerBaseController<Tenant, int>
    {
        protected override ITenantService BaseService { get; } 

        protected IAuthenticationService AuthService { get; }

        public TenantController(ITenantService baseService, IAuthenticationService authService) : base(baseService) {
             BaseService = baseService;
             AuthService = authService;
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] Tenant entity) {
            var result = await BaseService.CreateAsync(entity);
            return Ok(result);
        }

        [HttpPut]
        public virtual async Task<IActionResult> Update([FromBody] Tenant entity) {
            var result = await BaseService.UpdateAsync(entity);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public virtual async Task<IActionResult> DeleteOrDeactivate(int id) {
            var result = await BaseService.DeleteOrDeactivateAsync(id);
            return Ok(result);
        }
    }
}