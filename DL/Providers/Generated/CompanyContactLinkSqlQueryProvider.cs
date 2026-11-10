using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class CompanyContactLinkSqlQueryProvider : SqlQueryProvider<CompanyContactLink>
    {
        public CompanyContactLinkSqlQueryProvider()
            : base(new DbTableDescriptor("CompanyContactLink")
            {
                KeyColumns = new[] { "CompanyID", "ContactID" },
                EditableColumns = new[] { "CompanyID", "ContactID" },
                ActiveColumn = null,
                LookupLabel = LookupLabel.Key,
                LookupFilter = null
            })
        { }
    }
}