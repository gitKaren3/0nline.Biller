using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class CompanySqlQueryProvider : SqlQueryProvider<Company>
    {
        public CompanySqlQueryProvider()
            : base(new DbTableDescriptor("Company")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "TenantID", "Name", "BillingContactID", "Registration", "Address", "AddressCity", "AddressCode", "AddressCountry", "Phone", "LogoImageID", "Active", "CountryCode", "TaxNumber", "TaxSchemeID" },
                ActiveColumn = "Active",
                LookupLabel = new LookupLabel(new[] { "Name" }, " "),
                LookupFilter = "\"ID\" IS DISTINCT FROM (SELECT \"CompanyID\" FROM \"Tenant\" WHERE \"ID\" = @TenantId)"
            })
        { }
    }
}