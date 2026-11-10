using _0nline.Biller.DL.Contract.Numbering;
using Xunit;

namespace _0nline.Biller.Test;

public class InvoiceNumberFormatTests
{
    private static readonly DateTime November2026 = new(2026, 11, 10);

    private static InvoiceNumberFormat Parse(string text)
    {
        Assert.True(InvoiceNumberFormat.TryParse(text, out var format, out var error), error);
        return format!;
    }

    [Theory]
    [InlineData("#{n:00000}", 1, "#00001")]
    [InlineData("#{n:00000}", 84, "#00084")]
    [InlineData("INV-{n:00000}", 84, "INV-00084")]
    [InlineData("INV{n}", 84, "INV84")]
    [InlineData("{n}", 84, "84")]
    [InlineData("{n:0}", 7, "7")]
    [InlineData("2026/{n:0000}", 84, "2026/0084")]
    [InlineData("{yyyy}/{n:0000}", 84, "2026/0084")]
    [InlineData("{y:4}/{n}", 84, "2026/84")]
    [InlineData("{y:2}-{n:0000}", 84, "26-0084")]
    [InlineData("{n:0000}/A", 84, "0084/A")]
    [InlineData("{n:00000}", 0, "00000")]
    public void FormatsRenderLiteralsAndAPaddedNumber(string text, int number, string expected)
    {
        Assert.Equal(expected, Parse(text).Render(number, November2026));
    }

    [Fact]
    public void TheDefaultFormatIsAHashedFiveDigitNumber()
    {
        var format = Parse(InvoiceNumberFormat.DefaultFormat);

        Assert.Equal("#00001", format.Render(1, November2026));
        Assert.Equal("#00084", format.Render(84, November2026));
        Assert.Equal(5, format.Digits);
        Assert.False(format.HasYear);
    }

    [Fact]
    public void PaddingIsAMinimumSoTheNumberWidensInsteadOfTruncating()
    {
        var format = Parse("INV-{n:0000}");

        Assert.Equal("INV-9999", format.Render(9999, November2026));
        Assert.Equal("INV-10000", format.Render(10000, November2026));
    }

    [Fact]
    public void ALiteralDigitRunIsNotAPlaceholder()
    {
        // The 0 in the year is printed as typed, not treated as a padding run.
        Assert.Equal("2026/0084", Parse("2026/{n:0000}").Render(84, November2026));
    }

    [Fact]
    public void TheYearComesFromTheInvoiceDateNotTheClock()
    {
        var format = Parse("{yyyy}/{n:0000}");

        Assert.Equal("2027/0085", format.Render(85, new DateTime(2027, 1, 2)));
        Assert.Equal("1999/0007", format.Render(7, new DateTime(1999, 12, 31)));
    }

    [Fact]
    public void TheSameCounterRendersANewYearSoTheSequenceDoesNotReset()
    {
        var format = Parse("{yyyy}/{n:0000}");

        Assert.Equal("2026/0085", format.Render(85, new DateTime(2026, 12, 31)));
        Assert.Equal("2027/0085", format.Render(85, new DateTime(2027, 1, 2)));
    }

    [Fact]
    public void SurroundingWhitespaceIsTrimmedFromTheStoredFormat()
    {
        Assert.Equal("INV-{n}", Parse("  INV-{n}  ").Text);
    }

    [Fact]
    public void ATwentyCharacterLiteralStillFits()
    {
        var format = Parse(new string('x', 20) + "{n}");

        Assert.Equal(InvoiceNumberFormat.MaxLength, format.MaxRenderedLength);
        Assert.Equal(30, format.Render(int.MaxValue, November2026).Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INV-")]
    [InlineData("{n}-{n}")]
    [InlineData("{n:0000000000}")]
    [InlineData("{n:abc}")]
    [InlineData("{n:}")]
    [InlineData("{n")]
    [InlineData("INV-{n}}")]
    [InlineData("{x}")]
    [InlineData("{N:000}")]
    [InlineData("{yyyy}-{yyyy}")]
    public void UnusableFormatsAreRejectedWithAReason(string? text)
    {
        Assert.False(InvoiceNumberFormat.TryParse(text, out var format, out var error));
        Assert.Null(format);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public void AFormatThatCouldOverflowTheInvoiceNumberColumnIsRejected()
    {
        // 21 literal characters plus ten digits is 31, one over Invoice.InvoiceNumber.
        var text = new string('x', 21) + "{n}";

        Assert.False(InvoiceNumberFormat.TryParse(text, out _, out var error));
        Assert.Contains("30", error);

        // The format text itself has to fit the column too.
        Assert.NotNull(InvoiceNumberFormat.Validate(new string('x', 28) + "{n}"));
    }

    [Fact]
    public void ValidateReturnsNullForAGoodFormatAndAMessageOtherwise()
    {
        Assert.Null(InvoiceNumberFormat.Validate("#{n:00000}"));
        Assert.Null(InvoiceNumberFormat.Validate("{n}"));
        Assert.NotNull(InvoiceNumberFormat.Validate("INV-"));
    }

    [Theory]
    [InlineData("83", true, 83)]
    [InlineData("#00084", true, 84)]
    [InlineData("INV-0084", true, 84)]
    [InlineData("2026/0084", true, 84)]
    [InlineData("0084", true, 84)]
    [InlineData("0", true, 0)]
    [InlineData("INV-", false, 0)]
    [InlineData("INV-84A", false, 0)]
    [InlineData(null, false, 0)]
    [InlineData("", false, 0)]
    [InlineData("99999999999", false, 0)]
    public void TheNumericPortionIsTheTrailingDigitRun(string? invoiceNumber, bool found, int expected)
    {
        Assert.Equal(found, InvoiceNumberFormat.TryReadNumber(invoiceNumber, out var number));
        Assert.Equal(expected, number);
    }

    [Fact]
    public void ANegativeNumberIsRejectedRatherThanFormatted()
    {
        var format = Parse("{n:0000}");

        Assert.Throws<ArgumentOutOfRangeException>(() => format.Render(-1, November2026));
    }
}
