using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using Dapper;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Services;

namespace _0nline.Biller.DL.Services
{
    public class CompanyService : BillerBaseService<Company>, ICompanyService, IDLReadableService<Company>, IDLWritableService<Company>
    {
        private readonly IFileStoreService _fileStoreService;
        private readonly ICompanyContactLinkService _companyContactLinkService;

        public CompanyService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<Company> queryService, ITenantContext tenancyContext
            , IFileStoreService fileStoreService, ICompanyContactLinkService companyContactLinkService)
            : base(ctx, queryService, tenancyContext)
        {
            _fileStoreService = fileStoreService;
            _companyContactLinkService = companyContactLinkService;
        }

        public new Task<Result<Company[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Company>> CreateAsync(Company entity) => base.CreateAsync(entity);
        public new Task<Result<Company>> UpdateAsync(Company entity) => base.UpdateAsync(entity);
        public new Task<Result<Company?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        /// <summary>
        /// The tenant's clients: every company it owns except its own profile. The
        /// billing contact is joined in the same statement, so the list never shows a
        /// bare <c>BillingContactID</c> and editing does not need an N+1 read.
        /// </summary>
        /// <remarks>
        /// This is a report-shaped read, not a table read: the tenant's own company is
        /// excluded by business rule and the contact is part of the row. Keeping it on
        /// the service rather than the generic store means the rule stays with the
        /// entity it belongs to; a future report over another entity gets its own
        /// method and query rather than a generic join engine nobody else needs yet.
        /// </remarks>
        public async Task<Result<Company[]>> ListClientsAsync()
        {
            var company = Quoted("Company");
            var contact = Quoted("Contact");
            var tenant = Quoted("Tenant");

            var sql = $@"
                SELECT c.{Quoted("ID")}, c.{Quoted("TenantID")}, c.{Quoted("Name")},
                       c.{Quoted("BillingContactID")}, c.{Quoted("Registration")},
                       c.{Quoted("Address")}, c.{Quoted("AddressCity")}, c.{Quoted("AddressCode")},
                       c.{Quoted("AddressCountry")}, c.{Quoted("Phone")}, c.{Quoted("LogoImageID")},
                       c.{Quoted("Active")},
                       bc.{Quoted("ID")} AS {Quoted("ContactID")},
                       bc.{Quoted("TenantID")} AS {Quoted("ContactTenantID")},
                       bc.{Quoted("UserId")} AS {Quoted("ContactUserId")},
                       bc.{Quoted("FirstName")} AS {Quoted("ContactFirstName")},
                       bc.{Quoted("Surname")} AS {Quoted("ContactSurname")},
                       bc.{Quoted("Email")} AS {Quoted("ContactEmail")},
                       bc.{Quoted("CellPhone")} AS {Quoted("ContactCellPhone")}
                FROM {company} c
                LEFT JOIN {contact} bc
                       ON bc.{Quoted("ID")} = c.{Quoted("BillingContactID")}
                      AND bc.{Quoted("TenantID")} = c.{Quoted("TenantID")}
                WHERE c.{Quoted("TenantID")} = @TenantId
                  AND c.{Quoted("ID")} IS DISTINCT FROM
                      (SELECT {Quoted("CompanyID")} FROM {tenant} WHERE {Quoted("ID")} = @TenantId)
                ORDER BY c.{Quoted("Name")}";

            using var conn = Connection;
            var rows = await conn.QueryAsync<ClientRow>(sql, new { TenantId = _tenantContext.TenantId });

            var clients = rows.Select(row => new Company
            {
                ID = row.ID,
                TenantID = row.TenantID,
                Name = row.Name,
                BillingContactID = row.BillingContactID,
                Registration = row.Registration,
                Address = row.Address,
                AddressCity = row.AddressCity,
                AddressCode = row.AddressCode,
                AddressCountry = row.AddressCountry,
                Phone = row.Phone,
                LogoImageID = row.LogoImageID,
                Active = row.Active,
                BillingContact = row.ContactID is int contactId
                    ? new Contact
                    {
                        ID = contactId,
                        TenantID = row.ContactTenantID ?? row.TenantID,
                        UserId = row.ContactUserId,
                        FirstName = row.ContactFirstName,
                        Surname = row.ContactSurname,
                        Email = row.ContactEmail,
                        CellPhone = row.ContactCellPhone
                    }
                    : null
            }).ToArray();

            return Result<Company[]>.Success(clients);
        }

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
        /// <summary>
        /// Flattened result of the client-list join. The contact columns are aliased
        /// away from the company columns because both tables have ID/TenantID.
        /// </summary>
        private sealed class ClientRow
        {
            public int ID { get; set; }
            public int TenantID { get; set; }
            public string? Name { get; set; }
            public int? BillingContactID { get; set; }
            public string? Registration { get; set; }
            public string? Address { get; set; }
            public string? AddressCity { get; set; }
            public string? AddressCode { get; set; }
            public string? AddressCountry { get; set; }
            public long? Phone { get; set; }
            public int? LogoImageID { get; set; }
            public bool? Active { get; set; }

            public int? ContactID { get; set; }
            public int? ContactTenantID { get; set; }
            public long? ContactUserId { get; set; }
            public string? ContactFirstName { get; set; }
            public string? ContactSurname { get; set; }
            public string? ContactEmail { get; set; }
            public long? ContactCellPhone { get; set; }
        }
    }
}

