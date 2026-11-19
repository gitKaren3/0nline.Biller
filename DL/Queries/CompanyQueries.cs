using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;
using Dapper;

namespace _0nline.Biller.DL.Queries
{
    /// <summary>
    /// The company reads that are not a plain table read.
    /// </summary>
    public interface ICompanyQueries
    {
        /// <summary>
        /// The tenant's clients: every company it owns except its own profile, with the
        /// billing contact joined in so a list never shows a bare id.
        /// </summary>
        Task<Company[]> ListClientsAsync();
    }

    public sealed class CompanyQueries : DbSession, ICompanyQueries
    {
        public CompanyQueries(
            IDbContextProvider<BillerDbConfig> ctx,
            ITenantContext tenancyContext)
            : base(ctx, tenancyContext)
        {
        }

        public async Task<Company[]> ListClientsAsync()
        {
            RequireTenantId();

            var company = Quoted("Company");
            var contact = Quoted("Contact");
            var tenant = Quoted("Tenant");

            // The tenant's own profile is the company its Tenant row points at. A LEFT
            // JOIN plus "IS NULL OR <>" is the portable null-safe form of
            // IS DISTINCT FROM: a tenant with no company attached must still see every
            // client, and "<> NULL" would hide them all.
            var sql = $@"
                SELECT c.{Quoted("ID")}, c.{Quoted("TenantID")}, c.{Quoted("Name")},
                       c.{Quoted("BillingContactID")}, c.{Quoted("Registration")},
                       c.{Quoted("Address")}, c.{Quoted("AddressCity")}, c.{Quoted("AddressCode")},
                       c.{Quoted("AddressCountry")}, c.{Quoted("Phone")}, c.{Quoted("LogoImageID")},
                       c.{Quoted("Active")}, c.{Quoted("CountryCode")}, c.{Quoted("TaxNumber")},
                       c.{Quoted("TaxSchemeID")},
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
                LEFT JOIN {tenant} t
                       ON t.{Quoted("ID")} = @TenantId
                WHERE c.{Quoted("TenantID")} = @TenantId
                  AND (t.{Quoted("CompanyID")} IS NULL
                       OR c.{Quoted("ID")} <> t.{Quoted("CompanyID")})
                ORDER BY c.{Quoted("Name")}";

            var rows = await QueryAsync<ClientRow>(sql);

            return rows.Select(row => new Company
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
                CountryCode = row.CountryCode,
                TaxNumber = row.TaxNumber,
                TaxSchemeID = row.TaxSchemeID,
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
            public string? CountryCode { get; set; }
            public string? TaxNumber { get; set; }
            public short? TaxSchemeID { get; set; }

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
