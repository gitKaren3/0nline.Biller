using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm.Client.State;
using _0nline.Biller.Wasm.Components;
using _0nline.Shared.Contract.Models;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace _0nline.Biller.Test;

/// <summary>
/// The format field: it says "Invalid format" while the format cannot be parsed, shows the
/// number the next invoice will print when it can, and restores a blank rather than saving it.
/// </summary>
public class TenantSettingFormTests : Base.MudTestContext, IAsyncLifetime
{
    public TenantSettingFormTests()
    {
        Services.AddSingleton<ITenantSettingService>(new FakeTenantSettingService());
        Services.AddSingleton<TenantSettingState>();
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void AnUnparsableFormatSaysSoWhileTyping()
    {
        var cut = Render<TenantSettingForm>();
        cut.WaitForAssertion(() => cut.Find("#invoice-number-format"));

        cut.Find("#invoice-number-format").Input("INV-");

        cut.WaitForAssertion(() => Assert.Contains("Invalid format", cut.Markup));
    }

    [Fact]
    public void AValidFormatShowsTheNumberTheNextInvoiceWillPrint()
    {
        var cut = Render<TenantSettingForm>();
        cut.WaitForAssertion(() => cut.Find("#invoice-number-format"));

        cut.Find("#invoice-number-format").Input("#{n:00000}");

        cut.WaitForAssertion(() => Assert.Contains("Next: #00086", cut.Markup));
    }

    [Fact]
    public void ABlankFormatIsRestoredOnBlur()
    {
        var cut = Render<TenantSettingForm>();
        cut.WaitForAssertion(() => cut.Find("#invoice-number-format"));

        cut.Find("#invoice-number-format").Input(string.Empty);
        cut.Find("#invoice-number-format").Blur(new FocusEventArgs());

        cut.WaitForAssertion(() =>
            Assert.Equal("{n}", cut.Find("#invoice-number-format").GetAttribute("value")));
    }

    private sealed class FakeTenantSettingService : ITenantSettingService
    {
        private readonly TenantSetting _settings = new()
        {
            ID = 1,
            TenantID = 19,
            CurrencyCode = "ZAR",
            CurrencyMinorUnits = 2,
            TimeZoneId = "UTC",
            DefaultTaxRate = 15m,
            InvoiceNumberFormat = "{n}",
            NextInvoiceNumber = 86
        };

        public Task<Result<TenantSetting>> GetForTenantAsync()
            => Task.FromResult(Result<TenantSetting>.Success(_settings));

        public Task<Result<TenantSetting?>> GetOneAsync(int id)
            => Task.FromResult(Result<TenantSetting?>.Success(_settings));

        public Task<Result<TenantSetting>> EnsureForTenantAsync(int tenantId, long? userId)
            => Task.FromResult(Result<TenantSetting>.Success(_settings));

        public Task<Result<TenantSetting>> SaveAsync(TenantSetting setting)
            => Task.FromResult(Result<TenantSetting>.Success(setting));
    }
}
