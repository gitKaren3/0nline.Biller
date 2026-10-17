using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Services;

namespace _0nline.Biller.DL.Services
{
    public class CompanyService : BillerBaseService<Company>, ICompanyService, IDLReadableService<Company>, IDLWritableService<Company>
    {
        private readonly IFileStoreService _fileStoreService;

        public CompanyService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<Company> queryService, ITenantContext tenancyContext
            , IFileStoreService fileStoreService) 
            : base(ctx, queryService, tenancyContext)
        {
            _fileStoreService = fileStoreService;
        }

        public new Task<Result<Company[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Company>> CreateAsync(Company entity) => base.CreateAsync(entity);
        public new Task<Result<Company>> UpdateAsync(Company entity) => base.UpdateAsync(entity);
        public new Task<Result<Company?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        protected override Task<Company> SingleAugment(Company company)
        {
            if (company?.LogoImage?.Url != null)
                company.LogoImage.ResolvedUrl = _fileStoreService.ResolvePublicUrl(_tenantContext.AppId, company.LogoImage.Url);
            return Task.FromResult(company!);
        }
    }
}

