using Microsoft.AspNetCore.Mvc;
using _0nline.Shared.Contract.Interfaces.DL;

/// This is auto-generated from a template - do not modify.
namespace _0nline.Biller.Api.Lib
{
    [ApiController]
    [Route("api/biller/[controller]")]
    public abstract class BillerBaseController<T, TKey> : ControllerBase where T : class
    {
        protected virtual IDLBasicService<T> BaseService { get; }

        public BillerBaseController(IDLBasicService<T> baseService) {
            BaseService = baseService;
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(int id) {
            var result = await BaseService.GetOneAsync(id);
            return Ok(result);
        }
    }
}
