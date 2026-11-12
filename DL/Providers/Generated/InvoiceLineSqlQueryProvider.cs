using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class InvoiceLineSqlQueryProvider : SqlQueryProvider<InvoiceLine>
    {
        public InvoiceLineSqlQueryProvider()
            : base(new DbTableDescriptor("InvoiceLine")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "InvoiceID", "ContractID", "Description", "Qty", "TenantRateID", "TotalTax", "BilledRateIncl", "TaxSchemeID", "TaxRate", "TaxNote", "RateTypeID" },
                ActiveColumn = null,
                LookupLabel = new LookupLabel(new[] { "Description" }, " "),
                LookupFilter = null
            })
        { }
    }
}