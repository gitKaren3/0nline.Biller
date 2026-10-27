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
    public class InvoiceService : BillerBaseService<Invoice>, IInvoiceService, IDLReadableService<Invoice>, IDLWritableService<Invoice>
    {
        private readonly IInvoiceLineService _invoiceLineService;

        public InvoiceService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<Invoice> queryService,
            ITenantContext tenancyContext,
            IInvoiceLineService invoiceLineService)
            : base(ctx, queryService, tenancyContext)
        {
            _invoiceLineService = invoiceLineService;
        }

        public new Task<Result<Invoice[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Invoice>> CreateAsync(Invoice entity) => base.CreateAsync(entity);
        public new Task<Result<Invoice>> UpdateAsync(Invoice entity) => base.UpdateAsync(entity);
        public new Task<Result<Invoice?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        /// <summary>
        /// A single invoice is returned with its lines so a caller can render the
        /// full detail without a second round trip. The invoice itself is
        /// tenant-scoped by the base provider; lines are loaded only once that
        /// scoped invoice has been found.
        /// </summary>
        protected override async Task<Invoice> SingleAugment(Invoice invoice)
        {
            if (invoice is not null)
            {
                var lines = await _invoiceLineService.GetByInvoiceIdAsync(invoice.ID);
                invoice.InvoiceLines = lines.Value?.ToList() ?? new List<InvoiceLine>();
            }
            return invoice!;
        }
    }
}