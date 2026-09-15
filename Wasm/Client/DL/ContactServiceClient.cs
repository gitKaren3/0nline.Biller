
using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class ContactServiceClient : DLHttpClient<Contact>, IContactService
    {
        public ContactServiceClient(HttpClient http, IOptions<DLClientOptions> options) 
            : base(http, options, "Contact")
        {
        }

        public new Task<Result<IEnumerable<Contact>>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Contact>> CreateAsync(Contact entity) => base.CreateAsync(entity);
        public new Task<Result<Contact?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
        public new Task<Result<Contact>> UpdateAsync(Contact entity) => base.UpdateAsync(entity);
        
    }
}
