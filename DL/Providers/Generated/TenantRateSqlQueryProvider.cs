using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class TenantRateSqlQueryProvider : SqlQueryProvider<TenantRate>
    {
        public TenantRateSqlQueryProvider()
            : base("TenantRate", new string[] { "TenantID", "RateTypeID", "CompanyID", "ContactID", "DefaultRate", "Active", "StartDate", "EndDate" }, "ID")
        { }
        public override string AllQuery => AddFilter(base.AllQuery, "\"TenantID\" = @TenantId");
        public override string ByIdQuery => AddFilter(base.ByIdQuery, "\"TenantID\" = @TenantId");
        public override string LookupQuery => AddFilter($"SELECT \"ID\", \"''\" FROM \"TenantRate\"", "\"TenantID\" = @TenantId");
        public override string UpdateQuery => AddFilter(base.UpdateQuery, "\"TenantID\" = @TenantId");
        public override string DeleteQuery => AddFilter(base.DeleteQuery, "\"TenantID\" = @TenantId");
        public override string DeactivateQuery => $"UPDATE \"TenantRate\" SET \"Active\" = FALSE WHERE \"ID\" = @Id RETURNING *";
    }
}