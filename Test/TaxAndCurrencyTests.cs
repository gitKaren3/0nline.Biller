using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Location;
using Xunit;

namespace _0nline.Biller.Test;

public class TaxAndCurrencyTests
{
    [Theory]
    [InlineData(115, 15, 2, 15.00)]
    [InlineData(100, 15, 2, 13.04)]
    [InlineData(100, 0, 2, 0)]
    [InlineData(0, 15, 2, 0)]
    [InlineData(110, 10, 0, 10)]
    public void TaxIsExtractedFromTheInclusiveGross(
        decimal gross, decimal rate, int minorUnits, decimal expected)
    {
        Assert.Equal(expected, TaxMath.TaxFromInclusive(gross, rate, minorUnits));
    }

    [Fact]
    public void NetAndTaxStillAddUpToThePrintedGross()
    {
        decimal gross = 123.45m;
        decimal tax = TaxMath.TaxFromInclusive(gross, 15m, 2);

        Assert.Equal(gross, TaxMath.NetFromInclusive(gross, tax) + tax);
    }

    [Theory]
    [InlineData("ZAR", "R", 2)]
    [InlineData("USD", "$", 2)]
    [InlineData("JPY", "¥", 0)]
    [InlineData("KWD", "د.ك", 3)]
    public void CurrencyCatalogueCarriesSymbolAndMinorUnits(
        string code, string symbol, int minorUnits)
    {
        var currency = CurrencyCatalog.Default.Find(code);

        Assert.NotNull(currency);
        Assert.Equal(symbol, currency!.Symbol);
        Assert.Equal(minorUnits, currency.MinorUnits);
    }

    [Fact]
    public void UnknownCurrencyFallsBackToItsCode()
    {
        var currency = CurrencyCatalog.Default.Get("XYZ");

        Assert.Equal("XYZ", currency.Code);
        Assert.Equal("XYZ", currency.Symbol);
        Assert.Equal(2, currency.MinorUnits);
    }

    [Fact]
    public void RegisteredCompanyWithoutTaxDetailsIsInvalid()
    {
        var company = new Company
        {
            Name = "Acme",
            Address = "1 Main Road",
            AddressCity = "Cape Town",
            TaxRegistered = true
        };

        var results = company.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(company))
            .ToList();

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(Company.TaxSchemeSelection)));
        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(Company.CountryCode)));
        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(Company.TaxNumber)));
    }

    [Fact]
    public void CompanyWithNoTaxSchemeIsValidAndNotRegistered()
    {
        var company = new Company
        {
            Name = "Acme",
            Address = "1 Main Road",
            AddressCity = "Cape Town"
        };

        Assert.False(company.TaxRegistered);
        Assert.Empty(company.Validate(
            new System.ComponentModel.DataAnnotations.ValidationContext(company)));
    }

    [Fact]
    public void TurningTaxOffClearsTheSchemeAndNumber()
    {
        var company = new Company
        {
            TaxSchemeID = 1,
            TaxNumber = "4123456789"
        };

        Assert.True(company.TaxRegistered);

        company.TaxRegistered = false;

        Assert.Null(company.TaxSchemeID);
        Assert.Null(company.TaxNumber);
    }
}
