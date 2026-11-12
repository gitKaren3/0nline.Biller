using _0nline.Biller.DL.Contract.Models.db;
using Xunit;

namespace _0nline.Biller.Test;

public class TenantRateResolverTests
{
    private const int ContactId = 26;
    private const int CompanyId = 16;

    [Fact]
    public void ContactOnlyInvoicePrefersTheContactRateAndStillFallsBackToStandard()
    {
        var standards = Rate(1, rateType: 1, companyId: null, contactId: null, amount: 600m);
        var contact = Rate(2, rateType: 1, companyId: null, contactId: ContactId, amount: 550m);
        var company = Rate(3, rateType: 1, companyId: CompanyId, contactId: null, amount: 500m);

        var ranked = TenantRateResolver.Ranked(
            new[] { standards, company, contact }, ContactId, companyId: null);

        Assert.Equal(new[] { 2, 1 }, ranked.Select(rate => rate.ID));
    }

    [Fact]
    public void CompanyInvoicePrefersCompanyContactThenCompanyThenStandard()
    {
        var standards = Rate(1, rateType: 1, companyId: null, contactId: null, amount: 600m);
        var contactOnly = Rate(2, rateType: 1, companyId: null, contactId: ContactId, amount: 550m);
        var company = Rate(3, rateType: 1, companyId: CompanyId, contactId: null, amount: 500m);
        var companyContact = Rate(4, rateType: 1, companyId: CompanyId, contactId: ContactId, amount: 450m);

        var ranked = TenantRateResolver.Ranked(
            new[] { standards, contactOnly, company, companyContact }, ContactId, CompanyId);

        Assert.Equal(new[] { 4, 3, 1 }, ranked.Select(rate => rate.ID));
        Assert.DoesNotContain(ranked, rate => rate.ID == 2);
    }

    [Fact]
    public void MostRecentlyCapturedRateWinsWithinTheSameScope()
    {
        var older = Rate(1, rateType: 1, companyId: null, contactId: null, amount: 600m,
            capturedOn: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newer = Rate(2, rateType: 1, companyId: null, contactId: null, amount: 650m,
            capturedOn: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var best = TenantRateResolver.Best(
            new[] { older, newer }, ContactId, companyId: null, rateTypeId: 1);

        Assert.Equal(2, best?.ID);
    }

    [Fact]
    public void ArchivedRatesAreNeverOffered()
    {
        var archived = Rate(1, rateType: 1, companyId: null, contactId: null, amount: 600m);
        archived.Active = false;

        var best = TenantRateResolver.Best(
            new[] { archived }, ContactId, companyId: null, rateTypeId: 1);

        Assert.Null(best);
    }

    [Fact]
    public void ATypeFilterKeepsOnlyThatRateType()
    {
        var hourly = Rate(1, rateType: 1, companyId: null, contactId: null, amount: 600m,
            capturedOn: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var daily = Rate(2, rateType: 2, companyId: null, contactId: null, amount: 4000m,
            capturedOn: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var best = TenantRateResolver.Best(
            new[] { hourly, daily }, ContactId, companyId: null, rateTypeId: 1);

        Assert.Equal(1, best?.ID);
    }

    [Fact]
    public void CompanyScopedRateIsNotOfferedOnAPrivateContactInvoice()
    {
        var company = Rate(1, rateType: 1, companyId: CompanyId, contactId: null, amount: 500m);

        var best = TenantRateResolver.Best(
            new[] { company }, ContactId, companyId: null, rateTypeId: 1);

        Assert.Null(best);
    }

    private static TenantRate Rate(
        int id,
        short rateType,
        int? companyId,
        int? contactId,
        decimal amount,
        DateTime? capturedOn = null)
        => new()
        {
            ID = id,
            TenantID = 19,
            RateTypeID = rateType,
            CompanyID = companyId,
            ContactID = contactId,
            RateIncl = amount,
            Active = true,
            CapturedOn = capturedOn ?? new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
}
