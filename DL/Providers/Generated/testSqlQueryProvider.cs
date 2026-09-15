using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class testSqlQueryProvider : SqlQueryProvider<test>
    {
        public testSqlQueryProvider()
            : base("test", new string[] { "label", "payload", "status", "created_at" }, "id")
        { }
        public override string AllQuery => base.AllQuery;
        public override string ByIdQuery => base.ByIdQuery;
        public override string LookupQuery => $"SELECT \"id\" AS Key, \"label\" AS Value FROM \"test\"";
        public override string UpdateQuery => base.UpdateQuery;
        public override string DeleteQuery => base.DeleteQuery;
        public override string DeactivateQuery => string.Empty;
    }
}