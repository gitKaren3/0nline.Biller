using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class TaxSchemeSqlQueryProvider : SqlQueryProvider<TaxScheme>
    {
        public TaxSchemeSqlQueryProvider()
            : base(new DbTableDescriptor("TaxScheme")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "ID", "Name" },
                ActiveColumn = null,
                LookupLabel = new LookupLabel(new[] { "Name" }, " "),
                LookupFilter = null
            })
        { }
    }
}