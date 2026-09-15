using Microsoft.AspNetCore.Mvc;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

/// This is auto-generated from a template. 
/// Do not modify this file directly. Instead, create a partial class in a separate file to add custom logic.
namespace _0nline.Biller.Api.Lib
{
    [Route("api/biller/[controller]")]
    public partial class ContactController : BillerBaseController<Contact, int>
    {
        protected override IContactService DbService { get; } 

        public ContactController(IContactService dbService) : base(dbService) {
             DbService = dbService;
        }

        [HttpGet]
        public virtual async Task<IActionResult> GetAll() {
            var result = await DbService.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("lookup")]
        public virtual async Task<IActionResult> GetLookup() {
            var result = await DbService.GetLookupAsync();
            return Ok(result);
        }

        [HttpPost]
        public virtual async Task<IActionResult> Create([FromBody] Contact entity) {
            var result = await DbService.CreateAsync(entity);
            return Ok(result);
        }

        [HttpPut]
        public virtual async Task<IActionResult> Update([FromBody] Contact entity) {
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