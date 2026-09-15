using Microsoft.AspNetCore.Mvc;
using _0nline.Shared.Contract.Interfaces.DL;

/// This is auto-generated from a template - do not modify.
namespace _0nline.Biller.Api.Lib
{
    [ApiController]
    [Route("api/biller/[controller]")]
    public abstract class BillerBaseController<T, TKey> : ControllerBase where T : class
    {
        protected virtual IDLBaseService<T> DbService { get; }

        public BillerBaseController(IDLBaseService<T> baseService) {
            DbService = baseService;
        }

        [HttpGet("{id}")]
        public virtual async Task<IActionResult> GetById(int id) {
            var result = await DbService.GetOneAsync(id);
            return Ok(result);
        }
    }
}
