using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class TierServiceClient : DLHttpClient<Tier>, ITierService
    {
        public TierServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "tier")
        {
        }

        public new Task<Result<IEnumerable<Tier>>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

    }
}
