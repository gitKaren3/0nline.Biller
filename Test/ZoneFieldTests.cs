using _0nline.Shared.Client.Components;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace _0nline.Biller.Test;

/// <summary>
/// The country → time-zone cascade in the shared ZoneField. The UI interaction is
/// exercised through parameters; what matters here is the rule that the pair can
/// never be left inconsistent.
/// </summary>
public class ZoneFieldTests : Base.MudTestContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void SingleZoneCountrySelectsItsOnlyZone()
    {
        string? zone = "sentinel";

        var cut = Render<ZoneField>(parameters => parameters
            .Add(field => field.CountryIso, "ZA")
            .Add(field => field.TimeZoneId, (string?)null)
            .Add(field => field.TimeZoneIdChanged,
                 EventCallback.Factory.Create<string?>(this, value => zone = value)));

        cut.WaitForAssertion(() => Assert.Equal("Africa/Johannesburg", zone));
        Assert.Contains("Johannesburg (GMT+2)", cut.Markup);
    }

    [Fact]
    public void MultiZoneCountryUsesTheSuggestedZone()
    {
        string? zone = null;

        Render<ZoneField>(parameters => parameters
            .Add(field => field.CountryIso, "US")
            .Add(field => field.SuggestedTimeZoneId, "America/New_York")
            .Add(field => field.TimeZoneIdChanged,
                 EventCallback.Factory.Create<string?>(this, value => zone = value)));

        Assert.Equal("America/New_York", zone);
    }

    [Fact]
    public void MultiZoneCountryWithoutASuggestionLeavesTheZoneBlank()
    {
        string? zone = "sentinel";

        Render<ZoneField>(parameters => parameters
            .Add(field => field.CountryIso, "US")
            .Add(field => field.TimeZoneId, "Europe/London")
            .Add(field => field.TimeZoneIdChanged,
                 EventCallback.Factory.Create<string?>(this, value => zone = value)));

        Assert.Null(zone);
    }

    [Fact]
    public void AZoneFromAnotherCountryIsReplaced()
    {
        string? zone = null;

        var cut = Render<ZoneField>(parameters => parameters
            .Add(field => field.CountryIso, "ZA")
            .Add(field => field.TimeZoneId, "Europe/London")
            .Add(field => field.TimeZoneIdChanged,
                 EventCallback.Factory.Create<string?>(this, value => zone = value)));

        cut.WaitForAssertion(() => Assert.Equal("Africa/Johannesburg", zone));
    }
}
