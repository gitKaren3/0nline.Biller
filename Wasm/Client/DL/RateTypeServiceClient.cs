using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class RateTypeServiceClient : DLHttpClient<RateType>, IRateTypeService
    {
        public RateTypeServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "RateType")
        {
        }

        public new Task<Result<RateType[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();
    }
}