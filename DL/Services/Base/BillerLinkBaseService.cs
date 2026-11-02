using System.Reflection;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;

namespace _0nline.Biller.DL.Services.Base
{
    /// <summary>
    /// A Biller link service: addressed by the two rows it joins, never by its own key.
    /// </summary>
    /// <remarks>
    /// Mirrors <see cref="BillerBaseService{T}"/> for tenancy and write sanitisation, but
    /// sits on <see cref="LinkDbService{TLink,TParent,TTarget}"/> so the surface is
    /// pair-scoped and there is no by-id operation to misuse.
    /// </remarks>
    public abstract class BillerLinkBaseService<TLink, TParent, TTarget>
        : LinkDbService<TLink, TParent, TTarget>, IDLLinkService<TLink, TParent, TTarget>
        where TLink : class, IDbEntity, IDbLinkEntity<TParent, TTarget>, new()
        where TParent : class, IDbEntity
        where TTarget : class, IDbEntity
    {
        /// <summary>
        /// Biller rows are tenant-owned. A link table usually has no TenantID of its own;
        /// the flag still drives the target check in <c>AddLinkAsync</c>.
        /// </summary>
        protected override bool EnforceTenantScope => true;

        protected BillerLinkBaseService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<TLink> queryService,
            ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }

        /// <summary>
        /// <c>Tenant</c> is an ownership edge, not part of a link payload. Without this,
        /// a link body could carry a Tenant object and the generic navigation save would
        /// update (or insert) that tenant unscoped.
        /// </summary>
        protected override void SanitizeForWrite(object entity)
        {
            if (entity is null) return;

            var tenantNav = entity.GetType()
                .GetProperty(nameof(Tenant), BindingFlags.Instance | BindingFlags.Public);

            if (tenantNav?.PropertyType == typeof(Tenant))
                tenantNav.SetValue(entity, null);
        }

        public virtual async Task<Result<TLink[]>> GetByParentAsync(TParent parent)
        {
            var links = await GetForParentAsync(parent);
            return Result<TLink[]>.Success(links);
        }

        public virtual async Task<Result<TLink>> LinkAsync(TParent parent, TTarget target)
        {
            var link = await AddLinkAsync(parent, target);
            return link is null ? Result<TLink>.None() : Result<TLink>.Success(link);
        }

        public virtual async Task<Result<TLink?>> UnlinkAsync(TParent parent, TTarget target)
        {
            var count = await RemoveLinkAsync(parent, target);
            return count > 0 ? Result<TLink?>.Success() : Result<TLink?>.None();
        }
    }
}
