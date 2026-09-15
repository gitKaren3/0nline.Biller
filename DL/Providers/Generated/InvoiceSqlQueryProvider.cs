using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class InvoiceSqlQueryProvider : SqlQueryProvider<Invoice>
    {
        public InvoiceSqlQueryProvider()
            : base("Invoice", new string[] { "TenantID", "InvoiceNumber", "IssuedToContactID", "CompanyID", "InvoiceDate", "PaymentRef", "InvoiceStatusID" }, "ID")
        { }
        public override string AllQuery => AddFilter(base.AllQuery, "\"TenantID\" = @TenantId");
        public override string ByIdQuery => AddFilter(base.ByIdQuery, "\"TenantID\" = @TenantId");
        public override string LookupQuery => AddFilter($"SELECT \"ID\", \"InvoiceNumber\" FROM \"Invoice\"", "\"TenantID\" = @TenantId");
        public override string UpdateQuery => AddFilter(base.UpdateQuery, "\"TenantID\" = @TenantId");
        public override string DeleteQuery => AddFilter(base.DeleteQuery, "\"TenantID\" = @TenantId");
        public override string DeactivateQuery => string.Empty;
    }
}