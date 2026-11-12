using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class TenantRateSqlQueryProvider : SqlQueryProvider<TenantRate>
    {
        public TenantRateSqlQueryProvider()
            : base(new DbTableDescriptor("TenantRate")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "TenantID", "RateTypeID", "CompanyID", "ContactID", "RateIncl", "Active", "CapturedOn", "Note" },
                ActiveColumn = "Active",
                LookupLabel = new LookupLabel(new[] { "Note" }, " "),
                LookupFilter = null
            })
        { }
    }
}