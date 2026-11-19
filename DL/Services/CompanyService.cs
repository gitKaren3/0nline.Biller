using _0nline.Biller.DL.Contract.Enums;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Queries;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Location.Interfaces;
using _0nline.Shared.Services;
using System.Text.RegularExpressions;

namespace _0nline.Biller.DL.Services
{
    public class CompanyService : BillerBaseService<Company>, ICompanyService, IDLReadableService<Company>, IDLWritableService<Company>
    {
        private readonly IFileStoreService _fileStoreService;
        private readonly ICompanyContactLinkService _companyContactLinkService;
        private readonly ICountryCatalog _countries;

        private readonly ICompanyQueries _queries;

        public CompanyService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<Company> queryService, ITenantContext tenancyContext
            , IFileStoreService fileStoreService, ICompanyContactLinkService companyContactLinkService, ICountryCatalog countries
            , ICompanyQueries queries)
            : base(ctx, queryService, tenancyContext)
        {
            _fileStoreService = fileStoreService;
            _companyContactLinkService = companyContactLinkService;
            _countries = countries;
            _queries = queries;
        }

        public new Task<Result<Company[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<PageOf<Company>>> GetPageAsync(int page, int pageSize)
            => base.GetPageAsync(page, pageSize);

        public new async Task<Result<Company>> CreateAsync(Company entity)
        {
            PrepareTaxIdentity(entity);

            return ValidateTaxIdentity(entity) is { } error
                ? Result<Company>.Failure(ErrorCode.ValidationFailed, error)
                : await base.CreateAsync(entity);
        }

        public new async Task<Result<Company>> UpdateAsync(Company entity)
        {
            PrepareTaxIdentity(entity);

            return ValidateTaxIdentity(entity) is { } error
                ? Result<Company>.Failure(ErrorCode.ValidationFailed, error)
                : await base.UpdateAsync(entity);
        }

        public new Task<Result<Company?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        private static void PrepareTaxIdentity(Company entity)
        {
            if (string.IsNullOrWhiteSpace(entity.CountryCode))
                return;

            entity.CountryCode = entity.CountryCode.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// A company is tax-registered when it names a scheme. That makes the number
        /// mandatory, and where the country catalogue knows the number's shape, it must
        /// match. The scheme is cleared together with the number when the switch is off,
        /// so an unregistered company cannot keep a stale registration number.
        /// </summary>
        private string? ValidateTaxIdentity(Company entity)
        {
            if (entity.theTaxScheme is not { } scheme)
            {
                entity.TaxNumber = null;
                return null;
            }

            if (!Enum.IsDefined(scheme))
                return "Choose a valid tax scheme.";

            // Legacy rows only carry the country name. Resolve the ISO code so the
            // number can be checked and so the invoice can print it consistently.
            var country = _countries.FindByIso(entity.CountryCode)
                          ?? _countries.FindByName(entity.AddressCountry);

            if (country is null)
                return "Choose the country where the business is registered for tax.";

            entity.CountryCode = country.Iso2;

            if (string.IsNullOrWhiteSpace(entity.TaxNumber))
                return "Enter the tax registration number, or switch tax registration off.";

            if (!string.IsNullOrWhiteSpace(country.VatNumberPattern)
                && !Regex.IsMatch(
                    entity.TaxNumber.Trim(),
                    country.VatNumberPattern,
                    RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(250)))
            {
                return $"That does not look like a valid {country.Name} tax number.";
            }

            entity.TaxNumber = entity.TaxNumber.Trim();
            return null;
        }

        /// <summary>
        /// The tenant's clients: every company it owns except its own profile. The
        /// billing contact is joined in the same statement, so the list never shows a
        /// bare <c>BillingContactID</c> and editing does not need an N+1 read.
        /// </summary>
        /// <remarks>
        /// This is a report-shaped read, not a table read: the tenant's own company is
        /// excluded by business rule and the contact is part of the row. The statement
        /// lives with the other company queries.
        /// </remarks>
        public async Task<Result<Company[]>> ListClientsAsync()
            => Result<Company[]>.Success(await _queries.ListClientsAsync());

        /// <summary>
        /// A single company is returned with its linked contacts, so a detail view does
        /// not need a second round trip. The company was already tenant-filtered, and the
        /// links are requested through it - the only way a link row is addressed.
        /// </summary>
        protected override async Task<Company> SingleAugment(Company company)
        {
            if (company?.LogoImage?.Url != null)
                company.LogoImage.ResolvedUrl = _fileStoreService.ResolvePublicUrl(_tenantContext.AppId, company.LogoImage.Url);

            if (company is not null)
            {
                var links = await _companyContactLinkService.GetByParentAsync(company);

                company.Contacts = links.Value?
                    .Select(link => link.Contact)
                    .Where(contact => contact is not null)
                    .Select(contact => contact!)
                    .ToList() ?? new List<Contact>();
            }

            return company!;
        }
    }
}

