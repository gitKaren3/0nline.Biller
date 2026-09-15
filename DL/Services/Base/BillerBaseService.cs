using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;
using Dapper;

namespace _0nline.Biller.DL.Services.Base
{
    public abstract class BillerBaseService<T> : DbService<T, int>, IDLBaseService<T> where T : class
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

        protected async Task<Result<IEnumerable<T>>> GetAllAsync()
        {
            var list = await base.GetAllAsync();
            return Result<IEnumerable<T>>.Success(list);
        }

        protected new async Task<Result<IDictionary<int, string>>> GetLookupAsync()
        {
            var lookup = await base.GetLookupAsync();
            return Result<IDictionary<int, string>>.Success(lookup);
        }

        protected async Task<Result<T>> CreateAsync(T entity)
        {
            T[] entities = {entity};
            int count = await base.AddAsync(entities);
            return Result<T>.Success(entity);
        }

        protected new async Task<Result<T>> UpdateAsync(T entity)
        {
            T[] entities = {entity};
            int count = await base.UpdateAsync(entities);
            return Result<T>.Success(entity);
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
