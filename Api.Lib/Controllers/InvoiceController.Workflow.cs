using _0nline.Shared.Contract;
using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    /// <summary>
    /// Editor actions that are not plain CRUD. The generated controller already
    /// owns the create/update routes; these hang off the same invoice route.
    /// </summary>
    public partial class InvoiceController
    {
        /// <summary>
        /// Draft → Finalized. From here the invoice is locked and can be sent.
        /// </summary>
        [HttpPost("{id:int}/finalize")]
        public async Task<IActionResult> Finalize(int id)
        {
            var result = await BaseService.FinalizeAsync(id);

            if (result.Successful)
                return Ok(result);

            if (result.IsNone)
                return NotFound(result);

            return result.ErrorCode == ErrorCode.Conflict
                ? Conflict(result)
                : BadRequest(result);
        }

        /// <summary>
        /// The editor's preview: renders the current saved invoice without making
        /// a snapshot and without counting an access.
        /// </summary>
        [HttpGet("{id:int}/preview.pdf")]
        public async Task<IActionResult> Preview(int id)
        {
            var result = await BaseService.PreviewAsync(id);

            if (!result.Successful || result.Value is null)
                return NotFound(result);

            Response.Headers.CacheControl = "no-store";
            return File(result.Value, "application/pdf");
        }

        /// <summary>
        /// The number the next invoice would draw, without consuming it, so the editor
        /// can show it before the invoice is saved.
        /// </summary>
        [HttpGet("next-number")]
        public async Task<IActionResult> NextNumber()
        {
            var result = await BaseService.PeekInvoiceNumberAsync();
            return result.Successful ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Whether a number is already used. Advisory: the save is the guarantee.
        /// <paramref name="id"/> is the invoice being edited, so its own number is not
        /// reported as a clash.
        /// </summary>
        [HttpGet("number-exists")]
        public async Task<IActionResult> NumberExists(
            [FromQuery] string? number, [FromQuery] int id = 0)
        {
            var result = await BaseService.InvoiceNumberExistsAsync(number, id);
            return Ok(result);
        }
    }
}
