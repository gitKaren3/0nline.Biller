using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class FileSqlQueryProvider : SqlQueryProvider<File>
    {
        public FileSqlQueryProvider()
            : base(new DbTableDescriptor("File")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "TenantID", "Url", "Caption", "Public" },
                ActiveColumn = null,
                LookupLabel = new LookupLabel(new[] { "Url" }, " "),
                LookupFilter = null
            })
        { }
    }
}