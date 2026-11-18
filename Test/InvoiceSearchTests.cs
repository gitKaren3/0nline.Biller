using Microsoft.Extensions.Options;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Queries;
using _0nline.Biller.DL.Providers;
using _0nline.Biller.DL.Services;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Db.Service;
using Xunit;

namespace _0nline.Biller.Test;

/// <summary>
/// The invoice search predicate: what it matches, what it parameterises and which
/// date ranges it refuses.
/// </summary>
public class InvoiceSearchTests
{
    private readonly InvoiceQueries _queries = new(
        new PostgreSqlContextProvider<BillerDbConfig>(
            Options.Create(new BillerDbConfig())),
        new FakeTenant());

    [Fact]
    public void Text_matches_invoice_number_contact_and_company()
    {
        var (predicate, parameters) = _queries.SearchPredicate(
            new InvoiceSearch { Text = "acme" });

        Assert.NotNull(predicate);
        Assert.Contains("LOWER(\"InvoiceNumber\") LIKE LOWER(@Search) ESCAPE '\\'", predicate);
        Assert.Contains("\"IssuedToContactID\" IN (SELECT \"ID\" FROM \"Contact\"", predicate);
        Assert.Contains("\"CompanyID\" IN (SELECT \"ID\" FROM \"Company\"", predicate);
        Assert.Contains("\"TenantID\" = @SearchTenantId", predicate);
        Assert.Equal("%acme%", parameters!["Search"]);
        Assert.Equal(7, parameters!["SearchTenantId"]);
    }

    [Fact]
    public void Text_wildcards_are_escaped()
    {
        var (_, parameters) = _queries.SearchPredicate(
            new InvoiceSearch { Text = "50%_off" });

        Assert.Equal("%50\\%\\_off%", parameters!["Search"]);
    }

    [Fact]
    public void Date_range_and_statuses_are_anded()
    {
        var from = new DateTime(2026, 1, 1, 13, 0, 0);
        var to = new DateTime(2026, 1, 31, 13, 0, 0);

        var (predicate, parameters) = _queries.SearchPredicate(
            new InvoiceSearch
            {
                From = from,
                To = to,
                Statuses = new short[] { 1, 5 }
            });

        Assert.NotNull(predicate);
        Assert.Contains("\"InvoiceDate\" >= @From", predicate);
        Assert.Contains("\"InvoiceDate\" <= @To", predicate);
        Assert.Contains("\"InvoiceStatusID\" IN (@Status0, @Status1)", predicate);
        Assert.Equal(from.Date, parameters!["From"]);
        Assert.Equal(to.Date, parameters!["To"]);
        Assert.Equal((short)1, parameters!["Status0"]);
        Assert.Equal((short)5, parameters!["Status1"]);
    }

    [Fact]
    public void An_empty_search_has_no_predicate()
    {
        var (predicate, parameters) = _queries.SearchPredicate(new InvoiceSearch());

        Assert.Null(predicate);
        Assert.Null(parameters);
    }

    [Fact]
    public async Task End_date_before_start_is_rejected()
    {
        var result = await new TestInvoiceService().SearchPageAsync(
            new InvoiceSearch
            {
                From = new DateTime(2026, 2, 1),
                To = new DateTime(2026, 1, 1)
            },
            page: 1,
            pageSize: 10);

        Assert.False(result.Successful);
        Assert.Equal(ErrorCode.ValidationFailed, result.ErrorCode);
    }

    /// <summary>
    /// The date check runs before any dependency is touched, so the rest can be null:
    /// the predicate itself is covered against <see cref="InvoiceQueries"/> above.
    /// </summary>
    private sealed class TestInvoiceService : InvoiceService
    {
        public TestInvoiceService()
            : base(
                new PostgreSqlContextProvider<BillerDbConfig>(
                    Options.Create(new BillerDbConfig())),
                new InvoiceSqlQueryProvider(),
                new FakeTenant(),
                invoiceLineService: null!,
                pdfService: null!,
                tenantSettings: null!,
                contacts: null!,
                companies: null!,
                tenantRates: null!,
                rateTypes: null!,
                queries: null!,
                numbers: null!)
        {
        }
    }

    private sealed class FakeTenant : ITenantContext
    {
        public int? TenantId => 7;
        public long? UserId => 1;
        public string AppId => "test";
        public bool IsTenantAssigned => true;
    }
}
