using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    /// <summary>
    /// The invoice list endpoint. The generic page route is key-ordered; this one
    /// applies Biller's own order, so a client can ask for the newest invoices first.
    /// </summary>
    public partial class InvoiceController
    {
        [HttpGet("page/recent")]
        public async Task<IActionResult> GetRecentPage(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var result = await BaseService.GetRecentPageAsync(page, pageSize);
            return Ok(result);
        }
    }
}
