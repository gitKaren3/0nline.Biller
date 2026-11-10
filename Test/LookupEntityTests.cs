using _0nline.Biller.DL.Contract.Enums;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Interfaces;
using Xunit;

namespace _0nline.Biller.Test;

public class LookupEntityTests
{
    [Theory]
    [InlineData(typeof(_0nline.Biller.DL.Contract.Models.db.Lookup.Tier))]
    [InlineData(typeof(_0nline.Biller.DL.Contract.Models.db.Lookup.InvoiceStatus))]
    [InlineData(typeof(_0nline.Biller.DL.Contract.Models.db.Lookup.TaxScheme))]
    public void EnumTableEntitiesAreMarkedAsLookups(Type type)
    {
        Assert.True(typeof(IDbLookupEntity).IsAssignableFrom(type));
    }

    [Fact]
    public void HandWrittenLookupIsMarkedWithoutAnEnum()
    {
        Assert.True(typeof(IDbLookupEntity).IsAssignableFrom(typeof(RateType)));
    }

    [Fact]
    public void OrdinaryEntitiesAreNotLookups()
    {
        Assert.False(typeof(IDbLookupEntity).IsAssignableFrom(typeof(Invoice)));
        Assert.False(typeof(IDbLookupEntity).IsAssignableFrom(typeof(Company)));
    }

    [Fact]
    public void RequiredLookupViewTracksTheColumn()
    {
        var invoice = new Invoice { InvoiceStatusID = (short)InvoiceStatus.Draft };

        Assert.Equal(InvoiceStatus.Draft, invoice.theInvoiceStatus);

        invoice.theInvoiceStatus = InvoiceStatus.Paid;

        Assert.Equal((short)InvoiceStatus.Paid, invoice.InvoiceStatusID);
    }

    [Fact]
    public void NullableLookupViewMapsNullAndBack()
    {
        var company = new Company { TaxSchemeID = null };
        Assert.Null(company.theTaxScheme);

        company.theTaxScheme = TaxScheme.Vat;
        Assert.Equal((short)TaxScheme.Vat, company.TaxSchemeID);

        company.theTaxScheme = null;
        Assert.Null(company.TaxSchemeID);
    }
}
