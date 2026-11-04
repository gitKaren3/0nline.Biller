using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    /// <summary>
    /// Report-shaped reads for companies. Kept in a partial so the generated CRUD
    /// controller stays untouched and can be regenerated safely.
    /// </summary>
    public partial class CompanyController
    {
        /// <summary>
        /// The tenant's clients: companies excluding its own profile, each with its
        /// billing contact. This is not <c>GetAll</c>; the rule and the join belong
        /// to the client list, not to the company table.
        /// </summary>
        [HttpGet("clients")]
        public virtual async Task<IActionResult> ListClients()
        {
            var result = await BaseService.ListClientsAsync();
            return Ok(result);
        }
    }
}
