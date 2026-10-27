using _0nline.Biller.Wasm.Client.Themes;
using _0nline.Biller.Test.Base;
using _0nline.Shared.Client.Extensions;
using _0nline.Shared.Contract.Client.Interfaces;
using _0nline.Shared.Contract.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MudBlazor.Utilities;
using Xunit;

namespace _0nline.Biller.Test;

/// <summary>
/// Locks in Biller's product accent and proves it overrides the shared 0nline theme without
/// mutating the shared <see cref="ThemeSettings"/> singleton.
/// </summary>
public class BillerThemeServiceTests : MudTestContext
{
    private const string BillerCoral = "#ff6f61";

    private static string Hex(MudColor c) => c.ToString(MudColorOutputFormats.Hex);

    private static IThemeService ResolveTheme()
    {
        var services = new ServiceCollection();
        services.AddSharedClientServices(new ConfigurationBuilder().Build().GetSection("ThemeSettings"));
        services.AddScoped<IThemeService, BillerThemeService>();

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IThemeService>();
    }

    [Fact]
    public void BillerThemeService_ReplacesTheSharedRegistration()
    {
        var services = new ServiceCollection();
        services.AddSharedClientServices(new ConfigurationBuilder().Build().GetSection("ThemeSettings"));
        services.AddScoped<IThemeService, BillerThemeService>();

        using var provider = services.BuildServiceProvider();
        var theme = provider.GetRequiredService<IThemeService>();

        Assert.IsType<BillerThemeService>(theme);

        // The shared defaults must not have been mutated — IOptions<ThemeSettings> is a singleton.
        var shared = provider.GetRequiredService<IOptions<ThemeSettings>>().Value;
        Assert.Equal("#22bbbb", Hex(shared.PaletteLight.Primary));
        Assert.Equal("#22bbbb", Hex(shared.PaletteDark.Primary));
    }

    [Fact]
    public void Light_Mode_Uses_Billers_Accent()
    {
        var palette = ResolveTheme().CurrentTheme.PaletteLight;

        Assert.Equal("#5b4bb5", Hex(palette.Primary));
        Assert.Equal("#ffffff", Hex(palette.PrimaryContrastText));
        Assert.Equal("#5b4bb5", Hex(palette.AppbarBackground));
        Assert.Equal("#ffffff", Hex(palette.AppbarText));
    }

    [Fact]
    public void Dark_Mode_Uses_Billers_Accent()
    {
        var palette = ResolveTheme().CurrentTheme.PaletteDark;

        Assert.Equal("#a5b4fc", Hex(palette.Primary));
        Assert.Equal("#0f0f14", Hex(palette.PrimaryContrastText));
        Assert.Equal("#0f0f14", Hex(palette.Background));
    }

    [Fact]
    public void Default_Controls_Use_The_Accent_Not_The_Shared_Coral()
    {
        var theme = ResolveTheme();

        // ActionDefault drives checkboxes, radios, switches, menu icons and input hover borders.
        Assert.Equal("#5b4bb5", Hex(theme.CurrentTheme.PaletteLight.ActionDefault));
        Assert.Equal("#a5b4fc", Hex(theme.CurrentTheme.PaletteDark.ActionDefault));
    }

    [Fact]
    public void Secondary_And_Error_Are_Brand_Specific()
    {
        var theme = ResolveTheme();

        Assert.Equal("#9a6412", Hex(theme.CurrentTheme.PaletteLight.Secondary));
        Assert.Equal("#ffffff", Hex(theme.CurrentTheme.PaletteLight.SecondaryContrastText));
        Assert.Equal("#c6412e", Hex(theme.CurrentTheme.PaletteLight.Error));
    }

    [Fact]
    public void No_Palette_Slot_Keeps_The_0nline_Coral()
    {
        var theme = ResolveTheme();

        foreach (var palette in new[] { theme.CurrentTheme.PaletteLight, theme.CurrentTheme.PaletteDark })
        {
            foreach (var slot in new[] { palette.Primary, palette.Secondary, palette.Tertiary, palette.Error, palette.ActionDefault })
            {
                Assert.NotEqual(BillerCoral, Hex(slot));
            }
        }
    }
}
