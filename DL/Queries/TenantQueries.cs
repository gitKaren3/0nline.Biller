using System.Data;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;

namespace _0nline.Biller.DL.Queries
{
    /// <summary>
    /// The tenant-lifecycle statements: the profile pointers, closing a workspace and
    /// purging one.
    /// </summary>
    /// <remarks>
    /// The purge takes a connection and transaction because it is several statements that
    /// must land together; the owner check and the file cleanup around it are the
    /// service's, since only it knows what to do after the commit.
    ///
    /// Every statement names the tenant as <c>@ForTenantId</c>. The session binds
    /// <c>@TenantId</c> from the claim, and a purge must act on the tenant it was asked
    /// about even while that claim points elsewhere (closing workspace A from a token for
    /// B is refused by the owner check before anything is deleted).
    /// </remarks>
    public interface ITenantQueries
    {
        /// <summary>
        /// Sets the profile pointers on the tenant the user owns. Zero rows means the
        /// tenant is gone or belongs to someone else.
        /// </summary>
        Task<int> AttachProfileAsync(
            int tenantId, long userId, int? contactId, int? companyId);

        /// <summary>
        /// Closes the tenant the user owns and detaches it from them. Zero rows means it
        /// was not found.
        /// </summary>
        Task<int> CloseAsync(int tenantId, long userId);

        /// <summary>The owner of a tenant, inside the caller's transaction.</summary>
        Task<long?> GetOwnerIdAsync(
            int tenantId, IDbConnection conn, IDbTransaction tran);

        /// <summary>
        /// The physical files a purge must remove once the rows are gone. Read before
        /// the deletes, because the rows that name them are about to disappear.
        /// </summary>
        Task<TenantPurgeFiles> GetFilesAsync(
            int tenantId, IDbConnection conn, IDbTransaction tran);

        /// <summary>
        /// Deletes the tenant's data, leaf-first, then the tenant row itself.
        /// </summary>
        Task DeleteGraphAsync(
            int tenantId, IDbConnection conn, IDbTransaction tran);
    }

    /// <summary>The stored file names a purge has to remove after it commits.</summary>
    public sealed record TenantPurgeFiles(string[] ImageUrls, string[] FileUrls);

    public sealed class TenantQueries : DbSession, ITenantQueries
    {
        public TenantQueries(
            IDbContextProvider<BillerDbConfig> ctx,
            ITenantContext tenancyContext)
            : base(ctx, tenancyContext)
        {
        }

        public Task<int> AttachProfileAsync(
            int tenantId, long userId, int? contactId, int? companyId)
        {
            var sql = $@"UPDATE {Quoted("Tenant")}
                            SET {Quoted("ContactID")} = @ContactId,
                                {Quoted("CompanyID")} = @CompanyId
                          WHERE {Quoted("ID")} = @ForTenantId
                            AND {Quoted("UserId")} = @UserId";

            return ExecuteAsync(sql, new
            {
                ContactId = contactId,
                CompanyId = companyId,
                ForTenantId = tenantId,
                UserId = userId
            });
        }

        public Task<int> CloseAsync(int tenantId, long userId)
        {
            // Detaching the owner is what makes the workspace unreachable again; the
            // closed row stays for support and for the subscription repair.
            var sql = $@"UPDATE {Quoted("Tenant")}
                            SET {Quoted("Active")} = {_ctx.BooleanLiteral(false)},
                                {Quoted("UserId")} = NULL
                          WHERE {Quoted("ID")} = @ForTenantId
                            AND {Quoted("UserId")} = @UserId";

            return ExecuteAsync(sql, new { ForTenantId = tenantId, UserId = userId });
        }

        public async Task<long?> GetOwnerIdAsync(
            int tenantId, IDbConnection conn, IDbTransaction tran)
        {
            var sql = $@"SELECT {Quoted("UserId")}
                           FROM {Quoted("Tenant")}
                          WHERE {Quoted("ID")} = @ForTenantId";

            return await ScalarAsync<long?>(sql, new { ForTenantId = tenantId }, conn, tran);
        }

        public async Task<TenantPurgeFiles> GetFilesAsync(
            int tenantId, IDbConnection conn, IDbTransaction tran)
        {
            var images = await QueryAsync<string>(
                $@"SELECT {Quoted("Url")} FROM {Quoted("Image")} WHERE {Quoted("TenantID")} = @ForTenantId",
                new { ForTenantId = tenantId },
                conn,
                tran);

            var files = await QueryAsync<string>(
                $@"SELECT {Quoted("Url")} FROM {Quoted("File")} WHERE {Quoted("TenantID")} = @ForTenantId",
                new { ForTenantId = tenantId },
                conn,
                tran);

            return new TenantPurgeFiles(images, files);
        }

        public async Task DeleteGraphAsync(
            int tenantId, IDbConnection conn, IDbTransaction tran)
        {
            var parameters = new { ForTenantId = tenantId };

            // Break the tenant -> profile edges first; the rest is leaf-first. The FKs
            // would cascade some of this, but an explicit order keeps the purge readable
            // and independent of the cascade declarations.
            await ExecuteAsync(
                $@"UPDATE {Quoted("Tenant")}
                      SET {Quoted("ContactID")} = NULL, {Quoted("CompanyID")} = NULL
                    WHERE {Quoted("ID")} = @ForTenantId",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("CompanyContactLink")}
                    WHERE {Quoted("CompanyID")} IN (
                              SELECT {Quoted("ID")} FROM {Quoted("Company")}
                               WHERE {Quoted("TenantID")} = @ForTenantId)
                       OR {Quoted("ContactID")} IN (
                              SELECT {Quoted("ID")} FROM {Quoted("Contact")}
                               WHERE {Quoted("TenantID")} = @ForTenantId)",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("InvoiceLine")}
                    WHERE {Quoted("InvoiceID")} IN (
                              SELECT {Quoted("ID")} FROM {Quoted("Invoice")}
                               WHERE {Quoted("TenantID")} = @ForTenantId)",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("Invoice")} WHERE {Quoted("TenantID")} = @ForTenantId",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("TenantRate")} WHERE {Quoted("TenantID")} = @ForTenantId",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("BankAccount")} WHERE {Quoted("TenantID")} = @ForTenantId",
                parameters, conn, tran);

            // Company references Contact (BillingContactID) and Image (LogoImageID), so
            // it must go before either.
            await ExecuteAsync(
                $@"DELETE FROM {Quoted("Company")} WHERE {Quoted("TenantID")} = @ForTenantId",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("Image")} WHERE {Quoted("TenantID")} = @ForTenantId",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("Contact")} WHERE {Quoted("TenantID")} = @ForTenantId",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("File")} WHERE {Quoted("TenantID")} = @ForTenantId",
                parameters, conn, tran);

            await ExecuteAsync(
                $@"DELETE FROM {Quoted("Tenant")} WHERE {Quoted("ID")} = @ForTenantId",
                parameters, conn, tran);
        }
    }
}
