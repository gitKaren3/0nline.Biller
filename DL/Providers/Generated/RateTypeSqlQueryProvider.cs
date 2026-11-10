using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class RateTypeSqlQueryProvider : SqlQueryProvider<RateType>
    {
        public RateTypeSqlQueryProvider()
            : base(new DbTableDescriptor("RateType")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "Name", "MaxQty", "Active" },
                ActiveColumn = "Active",
                LookupLabel = new LookupLabel(new[] { "Name" }, " "),
                LookupFilter = null
            })
        { }
    }
}