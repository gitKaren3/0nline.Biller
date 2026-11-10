using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class InvoiceSqlQueryProvider : SqlQueryProvider<Invoice>
    {
        public InvoiceSqlQueryProvider()
            : base(new DbTableDescriptor("Invoice")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "TenantID", "InvoiceNumber", "IssuedToContactID", "CompanyID", "InvoiceDate", "PaymentRef", "InvoiceStatusID", "CurrencyCode", "CurrencyMinorUnits" },
                ActiveColumn = null,
                LookupLabel = new LookupLabel(new[] { "InvoiceNumber" }, " "),
                LookupFilter = null
            })
        { }
    }
}