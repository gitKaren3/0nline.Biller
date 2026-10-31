using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class BankAccountServiceClient : DLHttpClient<BankAccount>, IBankAccountService
    {
        public BankAccountServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "BankAccount")
        {
        }

        public new Task<Result<BankAccount[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<BankAccount>> CreateAsync(BankAccount entity) => base.CreateAsync(entity);
        public new Task<Result<BankAccount?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
        public new Task<Result<BankAccount>> UpdateAsync(BankAccount entity) => base.UpdateAsync(entity);
    }
}
