using System.Data;
using _0nline.Biller.DL.Contract.Enums;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Service;
using Dapper;

namespace _0nline.Biller.DL.Queries
{
    /// <summary>
    /// The invoice reads and set writes that the entity store does not offer.
    /// </summary>
    /// <remarks>
    /// Every statement here is spelled with <see cref="DbSession.Quoted"/> and takes its
    /// values as parameters; <c>@TenantId</c> is bound from the claim by the session, so
    /// no caller can supply it. A method that takes a connection and transaction runs
    /// inside the invoice save's unit of work and is not retried.
    /// </remarks>
    public sealed class InvoiceQueries : DbSession, IInvoiceQueries
    {
        /// <summary>
        /// The SQL fragment each text search shares. The pattern is lower-cased so the
        /// comparison is case-insensitive on both dialects, and the caller's wildcard
        /// characters are escaped by <see cref="ContainsPattern"/>.
        /// </summary>
        private const string LikeSearch = "LIKE LOWER(@Search) ESCAPE '\\'";

        public InvoiceQueries(
            IDbContextProvider<BillerDbConfig> ctx,
            ITenantContext tenancyContext)
            : base(ctx, tenancyContext)
        {
        }

        /// <summary>
        /// The predicate and parameters for an invoice search. The text term is ORed
        /// across the invoice number, contact and company; the date range and statuses
        /// are ANDed after it.
        /// </summary>
        /// <remarks>
        /// The contact and company subqueries carry the same tenant filter as the
        /// invoice itself. The invoice is already scoped by the store, but scoping the
        /// lookups too keeps a corrupt cross-tenant foreign key from matching another
        /// workspace's name.
        /// </remarks>
        public (string? Predicate, IReadOnlyDictionary<string, object?>? Parameters)
            SearchPredicate(InvoiceSearch search)
        {
            ArgumentNullException.ThrowIfNull(search);

            var clauses = new List<string>();
            var parameters = new Dictionary<string, object?>(StringComparer.Ordinal);

            var text = search.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
            {
                parameters["Search"] = ContainsPattern(text);
                parameters["SearchTenantId"] = _tenantContext.TenantId;

                var matches = string.Join(
                    " OR ",
                    $"LOWER({Quoted("InvoiceNumber")}) {LikeSearch}",
                    $"{Quoted("IssuedToContactID")} IN ("
                        + $"SELECT {Quoted("ID")} FROM {Quoted("Contact")} "
                        + $"WHERE {Quoted("TenantID")} = @SearchTenantId "
                        + $"AND (LOWER({Quoted("FirstName")}) {LikeSearch} "
                        + $"OR LOWER({Quoted("Surname")}) {LikeSearch}))",
                    $"{Quoted("CompanyID")} IN ("
                        + $"SELECT {Quoted("ID")} FROM {Quoted("Company")} "
                        + $"WHERE {Quoted("TenantID")} = @SearchTenantId "
                        + $"AND LOWER({Quoted("Name")}) {LikeSearch})");

                clauses.Add($"({matches})");
            }

            if (search.From is { } from)
            {
                clauses.Add($"{Quoted("InvoiceDate")} >= @From");
                parameters["From"] = from.Date;
            }

            if (search.To is { } to)
            {
                clauses.Add($"{Quoted("InvoiceDate")} <= @To");
                parameters["To"] = to.Date;
            }

            if (search.Statuses is { Length: > 0 } statuses)
            {
                var names = new string[statuses.Length];
                for (var index = 0; index < statuses.Length; index++)
                {
                    var name = $"Status{index}";
                    names[index] = $"@{name}";
                    parameters[name] = statuses[index];
                }

                clauses.Add($"{Quoted("InvoiceStatusID")} IN ({string.Join(", ", names)})");
            }

            return clauses.Count == 0
                ? (null, null)
                : (string.Join(" AND ", clauses), parameters);
        }

        public async Task<string[]> IssuedNumbersAsync(
            IReadOnlyCollection<string> candidates)
        {
            if (candidates.Count == 0)
                return Array.Empty<string>();

            var (names, parameters) = ParameterList("Candidate", candidates);

            var sql = $@"SELECT {Quoted("InvoiceNumber")}
                           FROM {Quoted("Invoice")}
                          WHERE {Quoted("TenantID")} = @TenantId
                            AND {Quoted("InvoiceNumber")} IN ({string.Join(", ", names)})";

            return await QueryAsync<string>(sql, parameters);
        }

        public async Task<bool> NumberTakenAsync(
            string number, IDbConnection conn, IDbTransaction tran)
        {
            var count = await ScalarAsync<int>(
                _ctx.CountQuery(
                    "Invoice",
                    $"{Quoted("InvoiceNumber")} = @Number AND {Quoted("TenantID")} = @TenantId"),
                new { Number = number },
                conn,
                tran);

            return count > 0;
        }

        public async Task<decimal?> LastQuantityAsync(
            int contactId, int? companyId, short rateTypeId, int excludeInvoiceId)
        {
            // A null company must match a null column, which `=` cannot express. The
            // explicit NULL test is the portable form of IS NOT DISTINCT FROM, and the
            // parameter is typed so a null does not leave Postgres guessing.
            //
            // The join is a report, not an entity read, so it is spelled here rather
            // than by the single-table statement builders. OFFSET/FETCH with an ORDER BY
            // is the one-row form both dialects accept.
            var sql = $@"
                SELECT il.{Quoted("Qty")}
                  FROM {Quoted("InvoiceLine")} il
                  JOIN {Quoted("Invoice")} i ON i.{Quoted("ID")} = il.{Quoted("InvoiceID")}
                 WHERE i.{Quoted("TenantID")} = @TenantId
                   AND i.{Quoted("IssuedToContactID")} = @ContactId
                   AND ((i.{Quoted("CompanyID")} = @CompanyId)
                        OR (i.{Quoted("CompanyID")} IS NULL AND @CompanyId IS NULL))
                   AND i.{Quoted("InvoiceStatusID")} NOT IN (@Draft, @Cancelled)
                   AND i.{Quoted("ID")} <> @ExcludeId
                   AND il.{Quoted("RateTypeID")} = @RateTypeId
                   AND il.{Quoted("Qty")} > 0
                 ORDER BY i.{Quoted("InvoiceDate")} DESC,
                          i.{Quoted("ID")} DESC,
                          il.{Quoted("ID")} DESC
                 OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY";

            var parameters = new DynamicParameters();
            parameters.Add("ContactId", contactId);
            parameters.Add("CompanyId", companyId, DbType.Int32);
            parameters.Add("Draft", (short)InvoiceStatus.Draft);
            parameters.Add("Cancelled", (short)InvoiceStatus.Cancelled);
            parameters.Add("ExcludeId", excludeInvoiceId);
            parameters.Add("RateTypeId", rateTypeId);

            return await ScalarAsync<decimal?>(sql, parameters);
        }

        public async Task<short?> IssuerTaxSchemeAsync()
        {
            // The issuer is the tenant's own company. A null scheme means it has not
            // registered for tax, which is a business fact, not an error.
            var sql = $@"
                SELECT c.{Quoted("TaxSchemeID")}
                  FROM {Quoted("Tenant")} t
                  LEFT JOIN {Quoted("Company")} c ON c.{Quoted("ID")} = t.{Quoted("CompanyID")}
                 WHERE t.{Quoted("ID")} = @TenantId";

            return await ScalarAsync<short?>(sql);
        }

        public async Task DeleteLinesAsync(
            int invoiceId,
            IReadOnlyCollection<int> lineIds,
            IDbConnection conn,
            IDbTransaction tran)
        {
            if (lineIds.Count == 0)
                return;

            var (names, parameters) = ParameterList("Line", lineIds);
            parameters.Add("InvoiceId", invoiceId);

            var predicate =
                $"{Quoted("InvoiceID")} = @InvoiceId "
                + $"AND {Quoted("ID")} IN ({string.Join(", ", names)})";

            await ExecuteAsync(
                _ctx.DeleteQuery("InvoiceLine", predicate, returnedcolumns: new[] { "ID" }),
                parameters,
                conn,
                tran);
        }

        /// <summary>
        /// An <c>IN</c> list as named parameters. The dialect has no array literal that
        /// both providers accept, and Dapper expands a list only inside its own syntax;
        /// naming the values keeps the statement the same on both.
        /// </summary>
        private static (string[] Names, DynamicParameters Parameters) ParameterList<T>(
            string prefix, IReadOnlyCollection<T> values)
        {
            var names = new string[values.Count];
            var parameters = new DynamicParameters();
            var index = 0;

            foreach (var value in values)
            {
                var name = $"{prefix}{index}";
                names[index] = $"@{name}";
                parameters.Add(name, value);
                index++;
            }

            return (names, parameters);
        }

        /// <summary>
        /// A contains pattern with the user's wildcard characters escaped, so a search
        /// for <c>50%</c> looks for the literal text rather than a pattern.
        /// </summary>
        private static string ContainsPattern(string value)
            => $"%{EscapeLike(value)}%";

        private static string EscapeLike(string value)
            => value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal)
                .Replace("_", "\\_", StringComparison.Ordinal)
                .Replace("[", "\\[", StringComparison.Ordinal)
                .Replace("]", "\\]", StringComparison.Ordinal);
    }
}
