using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class CompanySqlQueryProvider : SqlQueryProvider<Company>
    {
        public CompanySqlQueryProvider()
            : base("Company", new string[] { "TenantID", "Name", "BillingContactID", "Registration", "Address", "AddressCity", "AddressCode", "AddressCountry", "Phone", "LogoImageID", "Active" }, "ID")
        { }
        public override string AllQuery => AddFilter(base.AllQuery, "\"TenantID\" = @TenantId");
        public override string ByIdQuery => AddFilter(base.ByIdQuery, "\"TenantID\" = @TenantId");
        public override string LookupQuery => AddFilter($"SELECT \"ID\", \"Name\" FROM \"Company\"", "\"TenantID\" = @TenantId");
        public override string UpdateQuery => AddFilter(base.UpdateQuery, "\"TenantID\" = @TenantId");
        public override string DeleteQuery => AddFilter(base.DeleteQuery, "\"TenantID\" = @TenantId");
        public override string DeactivateQuery => $"UPDATE \"Company\" SET \"Active\" = FALSE WHERE \"ID\" = @Id RETURNING *";
    }
}