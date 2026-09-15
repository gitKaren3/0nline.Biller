
using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class CompanyServiceClient : DLHttpClient<Company>, ICompanyService
    {
        public CompanyServiceClient(HttpClient http, IOptions<DLClientOptions> options) 
            : base(http, options, "company")
        {
        }

        public new Task<Result<IEnumerable<Company>>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Company>> CreateAsync(Company entity) => base.CreateAsync(entity);
        public new Task<Result<Company?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
        public new Task<Result<Company>> UpdateAsync(Company entity) => base.UpdateAsync(entity);
        
    }
}
