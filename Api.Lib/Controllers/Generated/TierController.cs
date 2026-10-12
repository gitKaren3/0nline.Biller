using Microsoft.AspNetCore.Mvc;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

/// This is auto-generated from a template. 
/// Do not modify this file directly. Instead, create a partial class in a separate file to add custom logic.
namespace _0nline.Biller.Api.Lib
{
    [Route("api/biller/[controller]")]
    public partial class TierController : BillerBaseController<Tier, int>
    {
        protected override ITierService BaseService { get; } 

        public TierController(ITierService baseService) : base(baseService) {
             BaseService = baseService;
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetAll() {
            var result = await BaseService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("lookup")]
        public virtual async Task<IActionResult> GetLookup() {
            var result = await BaseService.GetLookupAsync();
            return Ok(result);
        }
    }
}