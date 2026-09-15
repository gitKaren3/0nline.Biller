using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class CompanyContactLinkSqlQueryProvider : SqlQueryProvider<CompanyContactLink>
    {
        public CompanyContactLinkSqlQueryProvider()
            : base("CompanyContactLink", new string[] { "ContactID" }, "CompanyID")
        { }
        public override string AllQuery => base.AllQuery;
        public override string ByIdQuery => base.ByIdQuery;
        public override string LookupQuery => $"SELECT \"CompanyID\" AS Key, \"''\" AS Value FROM \"CompanyContactLink\"";
        public override string UpdateQuery => base.UpdateQuery;
        public override string DeleteQuery => base.DeleteQuery;
        public override string DeactivateQuery => string.Empty;
    }
}