using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Exceptions;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;
using Dapper;

namespace _0nline.Biller.DL.Services.Base
{
    public abstract class BillerBaseService<T> : DbService<T, int>, IDLBaseService<T> where T : class, IDbEntity
    {
        //constructor
        protected BillerBaseService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<T> queryStringService, ITenantContext tenancyContext)
            : base(ctx, queryStringService, tenancyContext)
        {

        }

        public async Task<Result<T?>> GetOneAsync(int id)
        {
            var entity = await base.GetByIdAsync(id);
            return entity != null ? Result<T?>.Success(entity) : Result<T?>.None();
        }

        protected async Task<Result<T[]>> GetAllAsync()
        {
            var list = await base.GetAllAsync();
            return Result<T[]>.Success(list);
        }

        public Task<Result<PageOf<T>>> GetPageAsync(int page, int pageSize)
            => PageAsync(new PageRequest(page, pageSize));

        /// <summary>
        /// One page with an app-specific order. The order is chosen here, in code, so
        /// an app service can expose a sort without accepting a column name from the
        /// caller. With no sort the store uses its primary-key order.
        /// </summary>
        protected async Task<Result<PageOf<T>>> PageAsync(
            PageRequest page,
            params DbSort<T>[] orderBy)
        {
            try
            {
                var result = await base.GetPageAsync(page, orderBy: orderBy);
                return Result<PageOf<T>>.Success(
                    new PageOf<T>(result.Items, result.TotalCount, result.Page, result.PageSize));
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or OverflowException)
            {
                return Result<PageOf<T>>.Failure(ErrorCode.ValidationFailed, ex.Message);
            }
        }

        protected new async Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
        {
            var lookup = await base.GetLookupAsync();
            return Result<IReadOnlyDictionary<int, string>>.Success(lookup);
        }

        protected async Task<Result<T>> CreateAsync(T entity)
        {
            T[] entities = {entity};
            int count = await base.AddAsync(entities);
            return count > 0 ? Result<T>.Success(entity) : Result<T>.None();
        }

        protected new async Task<Result<T>> UpdateAsync(T entity)
        {
            T[] entities = {entity};
            try
            {
                int count = await base.UpdateAsync(entities);
                return count > 0 ? Result<T>.Success(entity) : Result<T>.None();
            }
            catch (DbConcurrencyException ex)
            {
                // The row exists but is no longer at the version the caller read, so the
                // UPDATE matched nothing. A conflict, not a server error: the caller
                // reloads and re-applies.
                return Result<T>.Failure(ErrorCode.Conflict, ex.Message);
            }
        }

        protected async Task<Result<T?>> DeleteOrDeactivateAsync(int id)
        {
            try
            {
                int count = await base.DeleteAsync(id);
                if (count > 0)
                    return Result<T?>.Success();
                else
                    return Result<T?>.None();
            }
            catch 
            {
                int count = await base.DeActivateAsync(id);
                var entity = await base.GetByIdAsync(id);
                if (count > 0)
                    return Result<T?>.Success(entity);
                else
                    return Result<T?>.None();
            }
        }

        /// <summary>
        /// Entity-based variant of <see cref="DeleteOrDeactivateAsync(int)"/> that
        /// carries the version the caller read, so a stale delete is reported as a
        /// conflict instead of a silent no-op or a wrong-row delete.
        /// </summary>
        protected async Task<Result<T?>> DeleteOrDeactivateAsync(T entity)
        {
            if (entity is null)
                throw new ArgumentNullException(nameof(entity));

            int id = GetEntityKey(entity);

            try
            {
                int count = await base.DeleteAsync(entity);
                if (count > 0)
                    return Result<T?>.Success();
                else
                    return Result<T?>.None();
            }
            catch (DbConcurrencyException ex)
            {
                return Result<T?>.Failure(ErrorCode.Conflict, ex.Message);
            }
            catch
            {
                int count = await base.DeActivateAsync(entity);
                var reloaded = await base.GetByIdAsync(id);
                if (count > 0)
                    return Result<T?>.Success(reloaded);
                else
                    return Result<T?>.None();
            }
        }

        private static int GetEntityKey(T entity)
        {
            var properties = GetCachedProperties<T>();
            var pks = GetPKs(properties);
            if (pks.Length == 0)
                throw new InvalidOperationException("Entity has no primary key.");

            var value = properties.First(p => p.Name == pks[0]).GetValue(entity);
            return Convert.ToInt32(value);
        }

        protected async Task<Result<T?>> FirstOrDefault(string sql, object parameters)
        {
            using var conn = Connection;
            var entity = await conn.QueryFirstOrDefaultAsync<T>(sql, parameters);
            if (entity != null)
            {
                await LoadNavigationPropertiesAsync(entity, conn);
                return Result<T?>.Success(entity);
            }
            else
                return Result<T?>.None();
        }
    }
}
