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
    /// A Biller child service: reached through its parent, never by its own key.
    /// </summary>
    /// <remarks>
    /// Mirrors <see cref="BillerBaseService{T}"/> for tenancy and write sanitisation,
    /// but sits on <see cref="ChildDbService{TChild,TParent}"/> so the public surface is
    /// parent-scoped and there is no by-id read or delete to misuse.
    /// </remarks>
    public abstract class BillerChildBaseService<TChild, TParent>
        : ChildDbService<TChild, TParent>, IDLChildService<TChild, TParent>
        where TChild : class, IDbEntity, IDbChildEntity<TParent>
        where TParent : class, IDbEntity
    {
        /// <summary>
        /// Biller rows are tenant-owned. A child usually has no TenantID column of its
        /// own, so this is mostly inert here; it still applies to any child that does.
        /// </summary>
        protected override bool EnforceTenantScope => true;

        protected BillerChildBaseService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<TChild> queryService,
            ITenantContext tenancyContext)
            : base(ctx, queryService, tenancyContext)
        {
        }

        /// <summary>
        /// <c>Tenant</c> is an ownership edge, not part of a child payload. Without this,
        /// a child body could carry a Tenant object and the generic navigation save
        /// would update (or insert) that tenant unscoped.
        /// </summary>
        protected override void SanitizeForWrite(object entity)
        {
            if (entity is null) return;

            var tenantNav = entity.GetType()
                .GetProperty(nameof(Tenant), BindingFlags.Instance | BindingFlags.Public);

            if (tenantNav?.PropertyType == typeof(Tenant))
                tenantNav.SetValue(entity, null);
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
