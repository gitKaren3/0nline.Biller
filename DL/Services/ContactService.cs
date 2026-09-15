using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Services
{
    public class ContactService : BillerBaseService<Contact>, IContactService, IDLReadableService<Contact>, IDLWritableService<Contact>
    {
        public ContactService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<Contact> queryService, ITenantContext tenancyContext)
           : base(ctx, queryService, tenancyContext)
        {
        }

        public new Task<Result<IEnumerable<Contact>>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Contact>> CreateAsync(Contact entity) => base.CreateAsync(entity);
        public new Task<Result<Contact>> UpdateAsync(Contact entity) => base.UpdateAsync(entity);
        public new Task<Result<Contact?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
     
    }
}
