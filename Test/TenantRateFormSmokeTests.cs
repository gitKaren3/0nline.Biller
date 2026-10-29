using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm.Components;
using Bunit;

namespace _0nline.Biller.Test;

public class TenantRateFormSmokeTests : Base.MudTestContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void RendersWithoutLookupServices()
    {
        var model = new TenantRate { RateTypeID = 1, DefaultRate = 100m };

        var cut = Render<TenantRateForm>(p => p.Add(x => x.Model, model));

        Assert.Contains("Billing rate", cut.Markup);
        Assert.NotNull(cut.Instance.EditContext);
    }
}