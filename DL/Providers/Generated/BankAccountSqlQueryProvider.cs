using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class BankAccountSqlQueryProvider : SqlQueryProvider<BankAccount>
    {
        public BankAccountSqlQueryProvider()
            : base(new DbTableDescriptor("BankAccount")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "TenantID", "AccountName", "BankName", "AccountNumber", "BranchCode", "AccountType", "SwiftCode", "IsDefault", "Active" },
                ActiveColumn = "Active",
                LookupLabel = new LookupLabel(new[] { "AccountName" }, " "),
                LookupFilter = null
            })
        { }
    }
}