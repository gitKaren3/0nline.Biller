using _0nline.Shared.Contract.Client.Interfaces;
using _0nline.Shared.Contract.Client.Models;
using Microsoft.Extensions.Options;
using MudBlazor;

namespace _0nline.Biller.Wasm.Client.Themes
{
    /// <summary>
    /// Biller's theme.
    /// <para>
    /// Starts from the shared 0nline defaults (<see cref="ThemeSettings"/>' constructor) and then
    /// applies Biller's own accent, so the product stays recognisably part of the family while
    /// reading as its own application.
    /// </para>
    /// <para>
    /// Registered in <c>Program.cs</c> <i>after</i> <c>AddSharedClientServices(...)</c> so that it
    /// replaces the shared <see cref="IThemeService"/> registration — MS DI resolves the last
    /// registration for a single service type.
    /// </para>
    /// </summary>
    public sealed class BillerThemeService : IThemeService
    {
        private readonly ThemeSettings _theme;

        public BillerThemeService(IOptions<ThemeSettings> options)
        {
            // A fresh instance rather than options.Value: IOptions<T> is a singleton, so mutating
            // the shared instance would leak Biller's accent into any other app resolving it.
            _theme = new ThemeSettings();
            IsDarkMode = options.Value.DefaultDarkMode;

            ApplyLight(_theme.PaletteLight);
            ApplyDark(_theme.PaletteDark);
        }

        public bool IsDarkMode { get; private set; }

        public MudTheme CurrentTheme => _theme;

        public event Action? OnChange;

        public void SetMode(bool isDark)
        {
            if (IsDarkMode == isDark) return;

            IsDarkMode = isDark;
            OnChange?.Invoke();
        }

        private static void ApplyLight(Palette palette)
        {
            palette.Primary = BillerTokens.Accent;
            palette.PrimaryContrastText = BillerTokens.AccentContrast;

            // The app bar is pinned to the accent in both modes so the product is recognisable at
            // a glance. MudAppBar only honours these slots while its Color is Color.Default.
            palette.AppbarBackground = BillerTokens.Accent;
            palette.AppbarText = BillerTokens.AccentContrast;

            // The colour of every uncoloured interactive control: checkboxes, radios, switches,
            // menu icons, input hover borders. Without this the shared palette's coral leaks
            // into the product.
            palette.ActionDefault = BillerTokens.Accent;

            palette.Secondary = BillerTokens.Bronze;
            palette.SecondaryContrastText = BillerTokens.BronzeContrast;

            palette.Error = BillerTokens.Error;

            // The shared palette's success, warning and tertiary fills are light colours, so
            // they need dark text rather than MudBlazor's default white.
            palette.SuccessContrastText = BillerTokens.OnLightFillContrast;
            palette.WarningContrastText = BillerTokens.OnLightFillContrast;
            palette.TertiaryContrastText = BillerTokens.OnLightFillContrast;

            palette.Info = BillerTokens.Info;
            palette.InfoContrastText = BillerTokens.AccentContrast;

        }

        private static void ApplyDark(Palette palette)
        {
            // Accent-coloured text and icons on a dark surface need the light tint to stay legible.
            palette.Primary = BillerTokens.AccentOnDark;
            palette.PrimaryContrastText = BillerTokens.AccentOnDarkContrast;

            palette.AppbarBackground = BillerTokens.Accent;
            palette.AppbarText = BillerTokens.AccentContrast;

            palette.ActionDefault = BillerTokens.AccentOnDark;

            palette.Secondary = BillerTokens.Bronze;
            palette.SecondaryContrastText = BillerTokens.BronzeContrast;

            palette.Error = BillerTokens.Error;

            palette.SuccessContrastText = BillerTokens.OnLightFillContrast;
            palette.WarningContrastText = BillerTokens.OnLightFillContrast;
            palette.TertiaryContrastText = BillerTokens.OnLightFillContrast;

            palette.Info = BillerTokens.Info;
            palette.InfoContrastText = BillerTokens.AccentContrast;

            palette.Background = BillerTokens.DarkBackground;
            palette.Surface = BillerTokens.DarkSurface;
            palette.DrawerBackground = BillerTokens.DarkDrawer;
        }
    }
}
