using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Services
{
    /// <summary>
    /// Company contacts are read and changed as a set; the pair-scoped operations live in
    /// <see cref="BillerLinkBaseService{TLink,TParent,TTarget}"/>.
    /// </summary>
    public class CompanyContactLinkService
        : BillerLinkBaseService<CompanyContactLink, Company, Contact>, ICompanyContactLinkService
    {
        public CompanyContactLinkService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<CompanyContactLink> queryService,
            ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }
    }
}
