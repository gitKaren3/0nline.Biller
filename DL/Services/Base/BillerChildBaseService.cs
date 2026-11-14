using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;

namespace _0nline.Biller.DL.Services.Base
{
    /// <summary>
    /// A Biller child service: reached through its parent, never by its own key.
    /// </summary>
    /// <remarks>
    /// Sits on <see cref="ChildDbService{TChild,TParent}"/> so the public surface is
    /// parent-scoped and there is no by-id read or delete to misuse. Tenancy is
    /// declared by the child model's marker, not by this base.
    /// </remarks>
    public abstract class BillerChildBaseService<TChild, TParent>
        : ChildDbService<TChild, TParent>, IDLChildService<TChild, TParent>
        where TChild : class, IDbEntity, IDbChildEntity<TParent>
        where TParent : class, IDbEntity
    {
        protected BillerChildBaseService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<TChild> queryService,
            ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }

        public virtual async Task<Result<TChild[]>> GetByParentAsync(TParent parent)
        {
            var children = await GetForParentAsync(parent);
            return Result<TChild[]>.Success(children);
        }

        public virtual async Task<Result<TChild>> AddAsync(TParent parent, TChild child)
        {
            var count = await AddForParentAsync(parent, child);
            return count > 0 ? Result<TChild>.Success(child) : Result<TChild>.None();
        }

        public virtual async Task<Result<TChild?>> DeleteAsync(TParent parent, TChild child)
        {
            var count = await DeleteForParentAsync(parent, child);
            return count > 0 ? Result<TChild?>.Success() : Result<TChild?>.None();
        }
    }
}
