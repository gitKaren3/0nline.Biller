using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Services
{
    /// <summary>
    /// Invoice lines are read and changed through their invoice. The parent-scoped
    /// operations live in <see cref="BillerChildBaseService{TChild,TParent}"/>; there is
    /// nothing line-specific to add yet.
    /// </summary>
    public class InvoiceLineService
        : BillerChildBaseService<InvoiceLine, Invoice>, IInvoiceLineService
    {
        public InvoiceLineService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<InvoiceLine> queryService,
            ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }
    }
}
