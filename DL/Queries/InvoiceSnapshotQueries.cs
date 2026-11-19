using _0nline.Biller.DL.Contract.Enums;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;

namespace _0nline.Biller.DL.Queries
{
    /// <summary>
    /// Snapshot reads and state changes that the entity store cannot express: a
    /// capability read without a tenant, a counter that only moves outside a window, and
    /// two transitions guarded on the row's current state.
    /// </summary>
    public interface IInvoiceSnapshotQueries
    {
        /// <summary>
        /// The live snapshot for a token, or null. Deliberately unscoped: the token is
        /// the capability and an anonymous caller has no tenant.
        /// </summary>
        Task<InvoiceSnapshot?> GetByTokenAsync(Guid token);

        /// <summary>
        /// Records one open. The counter moves only when the previous access is older
        /// than <paramref name="cutoff"/>, so a viewer's range fetches and reloads do
        /// not inflate it.
        /// </summary>
        Task RecordAccessAsync(
            Guid token,
            DateTime accessedAt,
            DateTime cutoff,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a live snapshot if it belongs to the caller's tenant, proved through
        /// its invoice. Null when it is gone, already revoked, or another tenant's.
        /// </summary>
        Task<InvoiceSnapshot?> RevokeAsync(int snapshotId);

        /// <summary>
        /// Moves a finalized invoice to issued. Guarded on the current status, so two
        /// concurrent sends cannot both win.
        /// </summary>
        Task MarkIssuedAsync(int invoiceId, int tenantId);
    }

    public sealed class InvoiceSnapshotQueries : DbSession, IInvoiceSnapshotQueries
    {
        public InvoiceSnapshotQueries(
            IDbContextProvider<BillerDbConfig> ctx,
            ITenantContext tenancyContext)
            : base(ctx, tenancyContext)
        {
        }

        public async Task<InvoiceSnapshot?> GetByTokenAsync(Guid token)
        {
            // Never add a tenant predicate here, and never add a by-id variant of this
            // read: the token is the capability, not the caller's claim.
            var sql = $@"SELECT *
                           FROM {Quoted("InvoiceSnapshot")}
                          WHERE {Quoted("Token")} = @Token
                            AND {Quoted("RevokedAt")} IS NULL";

            var rows = await QueryAsync<InvoiceSnapshot>(sql, new { Token = token });
            return rows.FirstOrDefault();
        }

        public async Task RecordAccessAsync(
            Guid token,
            DateTime accessedAt,
            DateTime cutoff,
            CancellationToken cancellationToken = default)
        {
            // All SET expressions see the old row, so the CASE still tests the previous
            // LastAccessedAt while the same statement refreshes it. The instants come
            // from the caller's clock, which is the clock every other stored instant
            // uses; CURRENT_TIMESTAMP would mix in the database's.
            var sql = $@"UPDATE {Quoted("InvoiceSnapshot")}
                            SET {Quoted("FirstAccessedAt")} = COALESCE({Quoted("FirstAccessedAt")}, @AccessedAt),
                                {Quoted("LastAccessedAt")}  = @AccessedAt,
                                {Quoted("AccessCount")}     = CASE
                                    WHEN {Quoted("LastAccessedAt")} IS NULL
                                      OR {Quoted("LastAccessedAt")} < @Cutoff
                                    THEN COALESCE({Quoted("AccessCount")}, 0) + 1
                                    ELSE COALESCE({Quoted("AccessCount")}, 0)
                                END
                          WHERE {Quoted("Token")} = @Token
                            AND {Quoted("RevokedAt")} IS NULL";

            await ExecuteAsync(
                sql,
                new { Token = token, AccessedAt = accessedAt, Cutoff = cutoff },
                cancellationToken: cancellationToken);
        }

        public async Task<InvoiceSnapshot?> RevokeAsync(int snapshotId)
        {
            RequireTenantId();

            // The child row has no TenantID, so ownership is proved through the invoice.
            // A snapshot id from another tenant updates nothing. The table name qualifies
            // the outer row inside the subquery, which both dialects accept.
            var predicate =
                $"{Quoted("ID")} = @Id AND {Quoted("RevokedAt")} IS NULL "
                + $"AND EXISTS (SELECT 1 FROM {Quoted("Invoice")} "
                + $"WHERE {Quoted("Invoice")}.{Quoted("ID")} = {Quoted("InvoiceSnapshot")}.{Quoted("InvoiceID")} "
                + $"AND {Quoted("Invoice")}.{Quoted("TenantID")} = @TenantId)";

            var sql = _ctx.UpdateQuery(
                "InvoiceSnapshot",
                new[] { "RevokedAt" },
                predicate);

            var rows = await QueryAsync<InvoiceSnapshot>(
                sql,
                new { Id = snapshotId, RevokedAt = DateTime.UtcNow });

            return rows.FirstOrDefault();
        }

        public async Task MarkIssuedAsync(int invoiceId, int tenantId)
        {
            // The tenant is a parameter rather than the claim: the invoice was loaded
            // through a tenant-scoped read, and this helper must not quietly do nothing
            // because a claim is missing.
            var predicate =
                $"{Quoted("ID")} = @Id AND {Quoted("TenantID")} = @ForTenantId "
                + $"AND {Quoted("InvoiceStatusID")} = @Finalized";

            var sql = _ctx.UpdateQuery("Invoice", new[] { "InvoiceStatusID" }, predicate);

            await ExecuteAsync(sql, new
            {
                InvoiceStatusID = (short)InvoiceStatus.Issued,
                Finalized = (short)InvoiceStatus.Finalized,
                Id = invoiceId,
                ForTenantId = tenantId
            });
        }
    }
}
