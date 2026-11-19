using Microsoft.Extensions.Options;
using Npgsql;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Numbering;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Service;
using Xunit;

namespace _0nline.Biller.Test;

/// <summary>
/// The invoice-number claim against a real PostgreSQL instance.
/// </summary>
/// <remarks>
/// The other tests pin the statement the allocator composes. This one proves the
/// behaviour the statement exists for: two claims cannot draw the same number, a
/// rolled-back claim returns the number, and a typed number can only move the counter
/// forward.
///
/// It works in a throwaway schema, so it touches no application data, and creates and
/// drops that schema itself. Like the shared database tests it asserts the connection
/// rather than skipping: a suite that claims to cover the counter should fail loudly
/// when it cannot reach the database.
/// </remarks>
public class InvoiceNumberClaimDatabaseTests
{
    private const string Server =
        "Host=localhost;Port=5432;Username=pilot;Password=hermes;Database=0nline.Biller";

    private const int TenantId = 7;

    [Fact]
    public async Task Two_concurrent_claims_draw_different_numbers()
    {
        await WithSchemaAsync(async schema =>
        {
            await SeedAsync(schema, next: 1);

            var numbers = (await Task.WhenAll(
                    ClaimAsync(schema),
                    ClaimAsync(schema)))
                .Select(claim => claim!.Value)
                .OrderBy(number => number)
                .ToArray();

            Assert.Equal(new[] { 1, 2 }, numbers);
            Assert.Equal(3, await CounterAsync(schema));
        });
    }

    [Fact]
    public async Task A_rolled_back_claim_leaves_the_counter_alone()
    {
        await WithSchemaAsync(async schema =>
        {
            await SeedAsync(schema, next: 1);

            Assert.Equal(1, await ClaimAsync(schema, commit: false));

            // A failed create must not consume the number it drew.
            Assert.Equal(1, await CounterAsync(schema));
            Assert.Equal(1, await ClaimAsync(schema));
        });
    }

    [Fact]
    public async Task Advance_moves_only_forward()
    {
        await WithSchemaAsync(async schema =>
        {
            await SeedAsync(schema, next: 1);

            // A typed number moves the counter past itself.
            await AdvanceAsync(schema, used: 10);
            Assert.Equal(11, await CounterAsync(schema));

            // A number below the counter is kept as typed and leaves it alone.
            await AdvanceAsync(schema, used: 3);
            Assert.Equal(11, await CounterAsync(schema));

            Assert.Equal(11, await ClaimAsync(schema));
        });
    }

    [Fact]
    public async Task A_claim_without_a_settings_row_returns_null()
    {
        await WithSchemaAsync(async schema =>
        {
            await CreateTableAsync(schema);

            Assert.Null(await ClaimAsync(schema));
        });
    }

    private static async Task WithSchemaAsync(Func<string, Task> body)
    {
        Assert.True(
            CanConnect(),
            "PostgreSQL is not reachable; the database-backed numbering test cannot run.");

        var schema = $"biller_test_numbering_{Guid.NewGuid():N}";

        await ExecuteAsync(Server, $@"CREATE SCHEMA ""{schema}""");

        try
        {
            await body(schema);
        }
        finally
        {
            await ExecuteAsync(Server, $@"DROP SCHEMA IF EXISTS ""{schema}"" CASCADE");
        }
    }

    private static async Task SeedAsync(string schema, int next)
    {
        await CreateTableAsync(schema);

        await ExecuteAsync(
            ConnectionString(schema),
            $@"INSERT INTO ""TenantSetting"" VALUES ({TenantId}, {next}, '#{{n:00000}}')");
    }

    private static Task CreateTableAsync(string schema)
        => ExecuteAsync(
            ConnectionString(schema),
            @"
            CREATE TABLE ""TenantSetting"" (
                ""TenantID""            integer NOT NULL,
                ""NextInvoiceNumber""   integer NULL,
                ""InvoiceNumberFormat"" character varying(30) NULL
            )");

    private static async Task<int?> ClaimAsync(string schema, bool commit = true)
    {
        await using var conn = new NpgsqlConnection(ConnectionString(schema));
        await conn.OpenAsync();
        await using var tran = await conn.BeginTransactionAsync();

        var claim = await Allocator(schema).ClaimAsync(conn, tran);

        if (commit)
            await tran.CommitAsync();
        else
            await tran.RollbackAsync();

        return claim?.Number;
    }

    private static async Task AdvanceAsync(string schema, int used)
    {
        await using var conn = new NpgsqlConnection(ConnectionString(schema));
        await conn.OpenAsync();
        await using var tran = await conn.BeginTransactionAsync();

        await Allocator(schema).AdvanceAsync(conn, tran, used);
        await tran.CommitAsync();
    }

    private static Task<int?> CounterAsync(string schema)
        => ScalarAsync(
            ConnectionString(schema),
            $@"SELECT ""NextInvoiceNumber"" FROM ""TenantSetting"" WHERE ""TenantID"" = {TenantId}");

    private static InvoiceNumberAllocator Allocator(string schema)
        => new(
            new PostgreSqlContextProvider<BillerDbConfig>(
                Options.Create(new BillerDbConfig
                {
                    ConnectionString = ConnectionString(schema)
                })),
            new FakeTenant(TenantId));

    /// <summary>
    /// The table names in the allocator's statements are unqualified, so the schema is
    /// selected per connection rather than spelled in the SQL.
    /// </summary>
    private static string ConnectionString(string schema) => $"{Server};Search Path={schema}";

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int?> ScalarAsync(string connectionString, string sql)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        return (int?)await cmd.ExecuteScalarAsync();
    }

    private static bool CanConnect()
    {
        try
        {
            using var conn = new NpgsqlConnection(Server);
            conn.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class FakeTenant : ITenantContext
    {
        public FakeTenant(int tenantId) => TenantId = tenantId;

        public int? TenantId { get; }
        public long? UserId => 1;
        public string AppId => "test";
        public bool IsTenantAssigned => true;
    }
}
