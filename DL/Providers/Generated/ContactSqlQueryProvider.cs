using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class ContactSqlQueryProvider : SqlQueryProvider<Contact>
    {
        public ContactSqlQueryProvider()
            : base(new DbTableDescriptor("Contact")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "TenantID", "UserId", "FirstName", "Surname", "Email", "CellPhone" },
                ActiveColumn = null,
                LookupLabel = new LookupLabel(new[] { "Surname", "FirstName" }, ", "),
                LookupFilter = "\"ID\" IS DISTINCT FROM (SELECT \"ContactID\" FROM \"Tenant\" WHERE \"ID\" = @TenantId)"
            })
        { }
    }
}