using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Models;

namespace _0nline.Biller.DL.Reports
{
    /// <summary>
    /// Turns a loaded invoice into the self-contained render model, then into PDF
    /// bytes. It reads live rows, so it is suitable for a draft preview but the
    /// snapshot service must store the model before it changes.
    /// </summary>
    public sealed class InvoicePdfService : IInvoicePdfService
    {
        private readonly ICompanyService _companies;
        private readonly IContactService _contacts;
        private readonly IBankAccountService _bankAccounts;
        private readonly IRateTypeService _rateTypes;
        private readonly IPDFRenderer _pdf;

        public InvoicePdfService(
            ICompanyService companies,
            IContactService contacts,
            IBankAccountService bankAccounts,
            IRateTypeService rateTypes,
            IPDFRenderer pdf)
        {
            _companies = companies;
            _contacts = contacts;
            _bankAccounts = bankAccounts;
            _rateTypes = rateTypes;
            _pdf = pdf;
        }

        public async Task<InvoicePdfModel> BuildModelAsync(Invoice invoice)
        {
            ArgumentNullException.ThrowIfNull(invoice);

            var tenant = invoice.Tenant;

            Company? issuer = null;
            if (tenant?.CompanyID is int issuerCompanyId)
                issuer = (await _companies.GetOneAsync(issuerCompanyId)).Value;

            Contact? issuerContact = null;
            if (tenant?.ContactID is int issuerContactId)
                issuerContact = (await _contacts.GetOneAsync(issuerContactId)).Value;

            Company? customer = null;
            if (invoice.CompanyID is int customerCompanyId)
                customer = (await _companies.GetOneAsync(customerCompanyId)).Value;

            var accounts = (await _bankAccounts.GetAllAsync()).Value ?? Array.Empty<BankAccount>();
            var bank = accounts.FirstOrDefault(account => account.IsDefault == true && account.Active != false)
                       ?? accounts.FirstOrDefault(account => account.Active != false);

            var rateTypes = (await _rateTypes.GetAllAsync()).Value?
                .Where(rateType => rateType.ID != 0)
                .GroupBy(rateType => rateType.ID)
                .ToDictionary(group => group.Key, group => group.First().Name ?? string.Empty)
                ?? new Dictionary<short, string>();

            return InvoicePdfModelFactory.Create(
                invoice,
                issuer,
                issuerContact,
                customer,
                invoice.IssuedToContact,
                bank,
                rateTypes);
        }

        public async Task<Result<byte[]>> RenderAsync(Invoice invoice, CancellationToken cancellationToken = default)
        {
            var model = await BuildModelAsync(invoice);
            return await RenderAsync(model, cancellationToken);
        }

        public async Task<Result<byte[]>> RenderAsync(InvoicePdfModel model, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(model);
            var pdf = await _pdf.RenderAsync(model, cancellationToken);
            return Result<byte[]>.Success(pdf);
        }
    }
}
