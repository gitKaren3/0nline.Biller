using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class testSqlQueryProvider : SqlQueryProvider<test>
    {
        public testSqlQueryProvider()
            : base(new DbTableDescriptor("test")
            {
                KeyColumns = new[] { "id" },
                EditableColumns = new[] { "label", "payload", "status", "created_at" },
                ActiveColumn = null,
                LookupLabel = new LookupLabel(new[] { "label" }, " "),
                LookupFilter = null
            })
        { }
    }
}