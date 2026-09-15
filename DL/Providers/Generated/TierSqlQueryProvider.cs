using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class TierSqlQueryProvider : SqlQueryProvider<Tier>
    {
        public TierSqlQueryProvider()
            : base("Tier", new string[] { "Name", "Rank", "MaxInvoiceCount", "MaxUploadCount", "Active" }, "ID")
        { }
        public override string AllQuery => base.AllQuery;
        public override string ByIdQuery => base.ByIdQuery;
        public override string LookupQuery => $"SELECT \"ID\" AS Key, \"Name\" AS Value FROM \"Tier\"";
        public override string UpdateQuery => base.UpdateQuery;
        public override string DeleteQuery => base.DeleteQuery;
        public override string DeactivateQuery => $"UPDATE \"Tier\" SET \"Active\" = FALSE WHERE \"ID\" = @Id RETURNING *";
    }
}