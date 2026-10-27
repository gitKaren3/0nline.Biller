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
    public class RateTypeService : BillerBaseService<RateType>, IRateTypeService, IDLReadableService<RateType>
    {
        public RateTypeService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<RateType> queryService, ITenantContext tenancyContext) 
            : base(ctx, queryService, tenancyContext)
        {
        }

        public new Task<Result<RateType[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();
     
    }
}

