using Microsoft.AspNetCore.Mvc;
using _0nline.Biller.DL.Contract.Models;

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

        /// <summary>
        /// The filtered invoice list. <c>text</c> matches the invoice number, contact
        /// name or company name; <c>from</c>/<c>to</c> bound the invoice date and
        /// <c>statuses</c> may be repeated.
        /// </summary>
        [HttpGet("page/search")]
        public async Task<IActionResult> SearchPage(
            [FromQuery] string? text = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] short[]? statuses = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 25)
        {
            var result = await BaseService.SearchPageAsync(
                new InvoiceSearch
                {
                    Text = text,
                    From = from,
                    To = to,
                    Statuses = statuses
                },
                page,
                pageSize);

            return Ok(result);
        }
    }
}
