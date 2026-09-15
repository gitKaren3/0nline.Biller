using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class InvoiceLineSqlQueryProvider : SqlQueryProvider<InvoiceLine>
    {
        public InvoiceLineSqlQueryProvider()
            : base("InvoiceLine", new string[] { "InvoiceID", "ContractID", "Description", "Qty", "TenantRateID", "TotalTax", "BilledRateIncl" }, "ID")
        { }
        public override string AllQuery => base.AllQuery;
        public override string ByIdQuery => base.ByIdQuery;
        public override string LookupQuery => $"SELECT \"ID\" AS Key, \"Description\" AS Value FROM \"InvoiceLine\"";
        public override string UpdateQuery => base.UpdateQuery;
        public override string DeleteQuery => base.DeleteQuery;
        public override string DeactivateQuery => string.Empty;
    }
}