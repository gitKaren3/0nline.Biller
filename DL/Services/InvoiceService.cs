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
    public class InvoiceService : BillerBaseService<Invoice>, IInvoiceService, IDLReadableService<Invoice>, IDLWritableService<Invoice>
    {
        public InvoiceService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<Invoice> queryService, ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }

        public new Task<Result<IEnumerable<Invoice>>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Invoice>> CreateAsync(Invoice entity) => base.CreateAsync(entity);
        public new Task<Result<Invoice>> UpdateAsync(Invoice entity) => base.UpdateAsync(entity);
        public new Task<Result<Invoice?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
    }
}
