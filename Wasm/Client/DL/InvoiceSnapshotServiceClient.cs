using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class InvoiceSnapshotServiceClient : DLHttpClient, IInvoiceSnapshotClient
    {
        public InvoiceSnapshotServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "invoicesnapshot")
        {
        }

        public Task<Result<InvoiceSnapshotLink>> CreateLinkAsync(int invoiceId, bool regenerate = false)
            => PostAndHandleAsync<InvoiceSnapshotLink>(
                $"invoice/{invoiceId}?regenerate={(regenerate ? "true" : "false")}");

        public Task<Result<InvoiceSnapshotActivity>> GetActivityAsync(int invoiceId)
            => GetAndHandleAsync<InvoiceSnapshotActivity>($"invoice/{invoiceId}/activity");

        public Task<Result<InvoiceSnapshot?>> RevokeAsync(int snapshotId)
            => PostAndHandleAsync<InvoiceSnapshot?>($"{snapshotId}/revoke");
    }
}
