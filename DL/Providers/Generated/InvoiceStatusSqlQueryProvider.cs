using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class InvoiceStatusSqlQueryProvider : SqlQueryProvider<InvoiceStatus>
    {
        public InvoiceStatusSqlQueryProvider()
            : base(new DbTableDescriptor("InvoiceStatus")
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