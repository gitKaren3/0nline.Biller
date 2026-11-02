using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class TenantSqlQueryProvider : SqlQueryProvider<Tenant>
    {
        public TenantSqlQueryProvider()
            : base(new DbTableDescriptor("Tenant")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "UserId", "TierID", "ContactID", "CompanyID", "Active" },
                ActiveColumn = "Active",
                LookupLabel = LookupLabel.Key,
                LookupFilter = null
            })
        { }
    }
}