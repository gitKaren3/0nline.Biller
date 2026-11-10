using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db.Lookup;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class InvoiceStatusServiceClient : DLHttpClient<InvoiceStatus>, IInvoiceStatusService
    {
        public InvoiceStatusServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "InvoiceStatus")
        {
        }

        public new Task<Result<InvoiceStatus[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();
    }
}