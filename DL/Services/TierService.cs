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
    public class TierService : BillerBaseService<Tier>, ITierService, IDLReadableService<Tier>
    {
        public TierService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<Tier> queryService, ITenantContext tenancyContext) 
            : base(ctx, queryService, tenancyContext)
        {
        }

        public new Task<Result<IEnumerable<Tier>>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();
     
    }
}

