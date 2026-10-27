using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Services
{
    public class InvoiceLineService : BillerBaseService<InvoiceLine>, IInvoiceLineService, IDLBaseService<InvoiceLine>
    {
        public InvoiceLineService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<InvoiceLine> queryService, ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }

        public new Task<Result<InvoiceLine?>> GetOneAsync(int id) => base.GetOneAsync(id);

        /// <summary>
        /// Lines are loaded through a parent invoice that has already been
        /// tenant-scoped. The generic helper loads each line's own navigation
        /// properties (for example <c>TenantRate</c>) and skips the back-reference
        /// to the invoice that is already in hand.
        /// </summary>
        public async Task<Result<InvoiceLine[]>> GetByInvoiceIdAsync(int invoiceId)
        {
            var lines = await GetByForeignKeyAsync<InvoiceLine, int>(
                line => line.InvoiceID, invoiceId);

            return Result<InvoiceLine[]>.Success(lines);
        }
    }
}