using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class InvoiceServiceClient : DLHttpClient<Invoice>, IInvoiceService
    {
        public InvoiceServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "invoice")
        {
        }

        public new Task<Result<Invoice[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Invoice>> CreateAsync(Invoice entity) => base.CreateAsync(entity);
        public new Task<Result<Invoice>> UpdateAsync(Invoice entity) => base.UpdateAsync(entity);
        public new Task<Result<Invoice?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        public Task<Result<Invoice>> FinalizeAsync(int id)
            => PostAndHandleAsync<Invoice>($"{id}/finalize");

        public Task<Result<byte[]>> PreviewAsync(int id)
            => GetBytesAsync($"{id}/preview.pdf");
    }
}
