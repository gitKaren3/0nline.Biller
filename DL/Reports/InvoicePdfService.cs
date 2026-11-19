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
        private readonly IImageService _images;
        private readonly IFileStoreService _fileStore;
        private readonly ITenantService _tenants;
        private readonly ITenantContext _tenantContext;
        private readonly IPDFRenderer _pdf;

        public InvoicePdfService(
            ICompanyService companies,
            IContactService contacts,
            IBankAccountService bankAccounts,
            IRateTypeService rateTypes,
            IImageService images,
            IFileStoreService fileStore,
            ITenantService tenants,
            ITenantContext tenantContext,
            IPDFRenderer pdf)
        {
            _companies = companies;
            _contacts = contacts;
            _bankAccounts = bankAccounts;
            _rateTypes = rateTypes;
            _images = images;
            _fileStore = fileStore;
            _tenants = tenants;
            _tenantContext = tenantContext;
            _pdf = pdf;
        }

        public async Task<InvoicePdfModel> BuildModelAsync(Invoice invoice)
        {
            ArgumentNullException.ThrowIfNull(invoice);

            var tenant = invoice.Tenant;
            var tenantId = invoice.TenantID > 0 ? invoice.TenantID : _tenantContext.TenantId ?? 0;
            if (tenant is null && tenantId > 0)
            {
                var tenantResult = await _tenants.GetOneAsync(tenantId);
                tenant = tenantResult.Value;
            }

            Company? issuer = null;
            if (tenant?.CompanyID is int issuerCompanyId)
                issuer = (await _companies.GetOneAsync(issuerCompanyId)).Value;

            byte[]? issuerLogo = null;
            Image? logoImage = issuer?.LogoImage;
            if (logoImage is null && issuer?.LogoImageID is int logoId)
            {
                var logoResult = await _images.GetOneAsync(logoId);
                if (logoResult.Successful && logoResult.Value is Image loadedLogo)
                    logoImage = loadedLogo;
            }

            if (logoImage is not null
                && !string.IsNullOrWhiteSpace(logoImage.Url)
                && !logoImage.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Local files live under <base>/<appId>/<relativePath>; the row
                    // stores only the relative part.
                    var relativePath = string.IsNullOrEmpty(_tenantContext.AppId)
                        ? logoImage.Url
                        : $"{_tenantContext.AppId}/{logoImage.Url.TrimStart('/', '\\')}";

                    using var stream = await _fileStore.GetFileStreamAsync(relativePath, logoImage.Public == true);
                    if (stream is not null)
                    {
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        issuerLogo = ms.ToArray();
                    }
                }
                catch
                {
                    issuerLogo = null;
                }
            }

            Contact? issuerContact = null;
            if (tenant?.ContactID is int issuerContactId)
                issuerContact = (await _contacts.GetOneAsync(issuerContactId)).Value;

            Company? customer = null;
            if (invoice.CompanyID is int customerCompanyId)
                customer = (await _companies.GetOneAsync(customerCompanyId)).Value;

            Contact? customerContact = invoice.IssuedToContact;
            if (customerContact is null && invoice.IssuedToContactID > 0)
                customerContact = (await _contacts.GetOneAsync(invoice.IssuedToContactID)).Value;

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
                customerContact,
                bank,
                rateTypes,
                issuerLogo);
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
