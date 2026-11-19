using System.Data;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;
using Dapper;

namespace _0nline.Biller.DL.Queries
{
    /// <summary>
    /// The statements behind the one-default-account invariant.
    /// </summary>
    /// <remarks>
    /// They act on every account of the tenant at once, so there is no single entity to
    /// hand to the store. Both are scoped by the claim, which is why they take no tenant
    /// id: a caller cannot widen them.
    /// </remarks>
    public interface IBankAccountQueries
    {
        /// <summary>Demotes the tenant's default account, optionally sparing one.</summary>
        Task ClearDefaultAsync(int? excludeId);

        /// <summary>
        /// Promotes the lowest-id active account when the tenant has active accounts and
        /// none of them is the default.
        /// </summary>
        Task EnsureDefaultAsync();
    }

    public sealed class BankAccountQueries : DbSession, IBankAccountQueries
    {
        public BankAccountQueries(
            IDbContextProvider<BillerDbConfig> ctx,
            ITenantContext tenancyContext)
            : base(ctx, tenancyContext)
        {
        }

        public async Task ClearDefaultAsync(int? excludeId)
        {
            RequireTenantId();

            // The exclude parameter is typed so a null does not leave Postgres guessing
            // which side of the comparison it belongs to.
            var sql = $@"UPDATE {Quoted("BankAccount")}
                            SET {Quoted("IsDefault")} = @IsDefault
                          WHERE {Quoted("TenantID")} = @TenantId
                            AND {Quoted("IsDefault")} = {_ctx.BooleanLiteral(true)}
                            AND (@ExcludeId IS NULL OR {Quoted("ID")} <> @ExcludeId)";

            var parameters = new DynamicParameters();
            parameters.Add("IsDefault", false);
            parameters.Add("ExcludeId", excludeId, DbType.Int32);

            await ExecuteAsync(sql, parameters);
        }

        public async Task EnsureDefaultAsync()
        {
            RequireTenantId();

            // MIN(ID) is the portable "first by id": ORDER BY ... LIMIT is not, and the
            // scalar subquery is the same sentence in both dialects.
            var sql = $@"UPDATE {Quoted("BankAccount")}
                            SET {Quoted("IsDefault")} = {_ctx.BooleanLiteral(true)}
                          WHERE {Quoted("ID")} = (
                                    SELECT MIN({Quoted("ID")})
                                      FROM {Quoted("BankAccount")}
                                     WHERE {Quoted("TenantID")} = @TenantId
                                       AND {Quoted("Active")} = {_ctx.BooleanLiteral(true)})
                            AND NOT EXISTS (
                                    SELECT 1
                                      FROM {Quoted("BankAccount")}
                                     WHERE {Quoted("TenantID")} = @TenantId
                                       AND {Quoted("Active")} = {_ctx.BooleanLiteral(true)}
                                       AND {Quoted("IsDefault")} = {_ctx.BooleanLiteral(true)})";

            await ExecuteAsync(sql);
        }
    }
}
