using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services;
using Xunit;

namespace _0nline.Biller.Test;

/// <summary>
/// <c>PrepareLines</c> rebuilds every line from the columns the line owns, and the
/// rebuild has to keep the version the caller read.
/// </summary>
/// <remarks>
/// Dropping it is not a cosmetic loss: the store matches the line's UPDATE on
/// <c>Version</c>, so a line saved as version 0 is refused as a conflict even when
/// nobody else has touched the invoice - which is exactly what "changed by another
/// writer" reported to a single user with one tab open.
/// </remarks>
public class InvoiceLinePreparationTests
{
    private static readonly InvoiceService.InvoiceDefaults NoTax = new(null, 0m);

    [Fact]
    public void A_prepared_line_keeps_the_version_it_was_loaded_with()
    {
        var loaded = new InvoiceLine
        {
            ID = 12,
            InvoiceID = 9,
            Version = 4,
            Description = "Consulting",
            Qty = 2m,
            BilledRateIncl = 115m
        };

        var invoice = new Invoice
        {
            ID = 9,
            CurrencyMinorUnits = 2,
            InvoiceLines = new List<InvoiceLine> { loaded }
        };

        var prepared = InvoiceService.PrepareLines(invoice, NoTax);

        var line = Assert.Single(prepared);
        Assert.Equal(12, line.ID);
        Assert.Equal(4, line.Version);
    }

    [Fact]
    public void A_new_line_is_prepared_as_version_zero()
    {
        var invoice = new Invoice
        {
            ID = 9,
            CurrencyMinorUnits = 2,
            InvoiceLines = new List<InvoiceLine>
            {
                new() { Description = "New line", Qty = 1m, BilledRateIncl = 50m }
            }
        };

        var prepared = InvoiceService.PrepareLines(invoice, NoTax);

        Assert.Equal(0, Assert.Single(prepared).Version);
    }
}
