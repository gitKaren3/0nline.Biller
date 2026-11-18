using System.Data;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;

namespace _0nline.Biller.DL.Numbering
{
    /// <summary>
    /// Claims and advances the tenant's invoice-number counter.
    /// </summary>
    /// <remarks>
    /// Both operations run on the caller's transaction and derive from
    /// <see cref="DbSession"/> so they inherit cancellation, identifier quoting and the
    /// <c>@TenantId</c> binding rather than opening a connection of their own.
    /// </remarks>
    public sealed class InvoiceNumberAllocator : DbSession, IInvoiceNumberAllocator
    {
        /// <summary>
        /// A CAS loser is guaranteed to make progress (the winner moved the counter), so
        /// the bound only protects against a counter that is being reset in a loop.
        /// </summary>
        private const int MaxAttempts = 1000;

        public InvoiceNumberAllocator(
            IDbContextProvider<BillerDbConfig> ctx,
            ITenantContext tenancyContext)
            : base(ctx, tenancyContext)
        {
        }

        public async Task<InvoiceNumberClaim?> ClaimAsync(
            IDbConnection conn, IDbTransaction tran)
        {
            RequireTenantId();

            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var current = await ReadAsync(conn, tran);
                if (current is null)
                    return null;

                var claimed = current.Next ?? 1;

                // Compare-and-swap rather than `col = col + 1 ... RETURNING`: the
                // returned row is not portable, and the value to render is the one
                // before the increment. Only the writer that saw `claimed` moves the
                // counter, so two creates cannot draw the same number; the loser reads
                // again and takes the next value.
                var predicate = CombinePredicates(
                    $"{Quoted("TenantID")} = @TenantId",
                    $"COALESCE({Quoted("NextInvoiceNumber")}, 1) = @Claimed");

                var updated = await QueryAsync<int>(
                    _ctx.UpdateQuery(
                        "TenantSetting",
                        new[] { "NextInvoiceNumber" },
                        predicate,
                        returnedcolumns: new[] { "NextInvoiceNumber" }),
                    new { NextInvoiceNumber = claimed + 1, Claimed = claimed },
                    conn,
                    tran);

                if (updated.Length > 0)
                    return new InvoiceNumberClaim(current.Format, claimed);
            }

            throw new InvalidOperationException("A free invoice number could not be found.");
        }

        public async Task AdvanceAsync(
            IDbConnection conn, IDbTransaction tran, int used)
        {
            RequireTenantId();

            if (used >= int.MaxValue)
                return;

            // Monotonic, so a number the tenant typed below the counter cannot pull it
            // backwards. At or above the counter it moves past itself, which is what
            // makes the next automatic number continue after a typed one.
            var predicate = CombinePredicates(
                $"{Quoted("TenantID")} = @TenantId",
                $"COALESCE({Quoted("NextInvoiceNumber")}, 1) < @Target");

            await ExecuteAsync(
                _ctx.UpdateQuery(
                    "TenantSetting",
                    new[] { "NextInvoiceNumber" },
                    predicate,
                    returnedcolumns: new[] { "NextInvoiceNumber" }),
                new { NextInvoiceNumber = used + 1, Target = used + 1 },
                conn,
                tran);
        }

        private async Task<Counter?> ReadAsync(IDbConnection conn, IDbTransaction tran)
        {
            var sql = $@"SELECT {Quoted("InvoiceNumberFormat")} AS {Quoted("Format")},
                                {Quoted("NextInvoiceNumber")} AS {Quoted("Next")}
                           FROM {Quoted("TenantSetting")}
                          WHERE {Quoted("TenantID")} = @TenantId";

            var rows = await QueryAsync<Counter>(sql, null, conn, tran);
            return rows.FirstOrDefault();
        }

        /// <summary>The two columns a claim needs; the rest of the row is not read.</summary>
        private sealed class Counter
        {
            public string? Format { get; set; }
            public int? Next { get; set; }
        }
    }
}
