using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class TenantSqlQueryProvider : SqlQueryProvider<Tenant>
    {
        public TenantSqlQueryProvider()
            : base("Tenant", new string[] { "UserId", "TierID", "ContactID", "CompanyID", "Active" }, "ID")
        { }
        public override string AllQuery => base.AllQuery;
        public override string ByIdQuery => base.ByIdQuery;
        public override string LookupQuery => $"SELECT \"ID\" AS Key, \"''\" AS Value FROM \"Tenant\"";
        public override string UpdateQuery => base.UpdateQuery;
        public override string DeleteQuery => base.DeleteQuery;
        public override string DeactivateQuery => $"UPDATE \"Tenant\" SET \"Active\" = FALSE WHERE \"ID\" = @Id RETURNING *";
    }
}