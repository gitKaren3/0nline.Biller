using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class TierSqlQueryProvider : SqlQueryProvider<Tier>
    {
        public TierSqlQueryProvider()
            : base(new DbTableDescriptor("Tier")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "Name", "Rank", "MaxInvoiceCount", "MaxUploadCount", "Active" },
                ActiveColumn = "Active",
                LookupLabel = new LookupLabel(new[] { "Name" }, " "),
                LookupFilter = null
            })
        { }
    }
}