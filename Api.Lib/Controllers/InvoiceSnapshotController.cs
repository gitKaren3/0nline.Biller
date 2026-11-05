using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    /// <summary>
    /// Issues invoice snapshots and serves their PDFs. This controller is hand
    /// written: the snapshot table is child-scoped, so the generator emits no CRUD
    /// surface for it, and the public read is by token rather than by id.
    /// </summary>
    [ApiController]
    [Route("api/biller/invoicesnapshot")]
    public class InvoiceSnapshotController : ControllerBase
    {
        private const string PdfContentType = "application/pdf";

        private readonly IInvoiceSnapshotService _snapshots;

        public InvoiceSnapshotController(IInvoiceSnapshotService snapshots)
        {
            _snapshots = snapshots;
        }

        /// <summary>
        /// Freezes an invoice and returns the link to mail. Re-issuing without
        /// <paramref name="regenerate"/> returns the live snapshot, so a resend does
        /// not invalidate a link that is already in someone's inbox.
        /// </summary>
        [HttpPost("invoice/{invoiceId:int}")]
        public async Task<IActionResult> Create(int invoiceId, [FromQuery] bool regenerate = false)
        {
            var result = await _snapshots.CreateForInvoiceAsync(invoiceId, regenerate);

            if (!result.Successful
                || result.Value is not InvoiceSnapshot snapshot
                || snapshot.Token is not Guid token)
            {
                return NotFound(Result.NotFound(
                    HttpContext.TraceIdentifier, "Invoice not found."));
            }

            return Ok(Result<InvoiceSnapshotLink>.Success(new InvoiceSnapshotLink
            {
                Token = token,
                Url = $"{Request.Scheme}://{Request.Host}/api/biller/invoicesnapshot/{token:D}/pdf",
                CreatedAt = snapshot.CreatedAt ?? DateTime.UtcNow
            }));
        }

        /// <summary>
        /// The emailed link. Anonymous by design: the GUID token is the capability,
        /// and no tenant claim is available to scope it with.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{token:guid}/pdf")]
        public async Task<IActionResult> GetPdf(Guid token, CancellationToken cancellationToken)
        {
            var result = await _snapshots.RenderPdfAsync(token, cancellationToken);

            // Unknown, revoked and unreadable snapshots are indistinguishable to the
            // caller, so a wrong guess learns nothing.
            if (!result.Successful || result.Value is null)
                return NotFound();

            // Delivery evidence belongs to the anonymous path only: a tenant
            // previewing its own invoice must not look like the client opened it.
            if (User.Identity?.IsAuthenticated != true)
                await _snapshots.RecordAccessAsync(token, cancellationToken);

            Response.Headers.CacheControl = "private, no-store";
            Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            Response.Headers.ContentDisposition = $"inline; filename=\"invoice-{token:N}.pdf\"";

            return new FileContentResult(result.Value, PdfContentType)
            {
                EnableRangeProcessing = true
            };
        }

        /// <summary>Kills a link without touching the invoice.</summary>
        [HttpPost("{id:int}/revoke")]
        public async Task<IActionResult> Revoke(int id)
        {
            var result = await _snapshots.RevokeAsync(id);
            return result.Successful ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// What the tenant sees about the link: when it was first opened, when it
        /// was last opened, and how many opens were counted.
        /// </summary>
        [HttpGet("invoice/{invoiceId:int}/activity")]
        public async Task<IActionResult> GetActivity(int invoiceId)
        {
            var result = await _snapshots.GetActivityAsync(invoiceId);
            return result.Successful ? Ok(result) : NotFound(result);
        }
    }
}
