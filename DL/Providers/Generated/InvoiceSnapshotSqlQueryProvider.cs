using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class InvoiceSnapshotSqlQueryProvider : SqlQueryProvider<InvoiceSnapshot>
    {
        public InvoiceSnapshotSqlQueryProvider()
            : base(new DbTableDescriptor("InvoiceSnapshot")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "InvoiceID", "Token", "LayoutVersion", "Snapshot", "CreatedAt", "RevokedAt", "FirstAccessedAt", "LastAccessedAt", "AccessCount" },
                ActiveColumn = null,
                LookupLabel = new LookupLabel(new[] { "Token" }, " "),
                LookupFilter = null
            })
        { }
    }
}