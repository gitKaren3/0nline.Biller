using MudBlazor;
namespace _0nline.Biller.Wasm.Client.Themes
{
    public static class _0nlineTheme
    {
        public static MudTheme Theme = new MudTheme()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#33cccc",      // turquoise brand color
                Secondary = "#FF6F61",    // coral accent
                Background = "#F5F5F5",   // light gray background
                Surface = "#ffffff",      // white cards/containers
                AppbarBackground = "#33cccc",
                DrawerBackground = "#ffffff",
                DrawerText = "#333333",
                DrawerIcon = "#333333",
                TextPrimary = "#333333",  // dark gray text
                TextSecondary = "#555555"
            },
            Typography = new Typography()
            {
                Default = new DefaultTypography()
                { 
                    FontFamily = new[] { "Inconsolata", "Share Tech Mono", "Cascadia Mono", "monospace" }
                },
                H1 = new H1Typography()
                {
                    FontFamily = new[] { "Inconsolata", "Cascadia Mono", "monospace" },
                    FontWeight = "700"
                },
                H2 = new H2Typography()
                {
                    FontFamily = new[] { "Inconsolata", "Cascadia Mono", "monospace" }
                },
                H3 = new H3Typography()
                {
                    FontFamily = new[] { "Inconsolata", "Cascadia Mono", "monospace" }
                },
                H4 = new H4Typography()
                {
                    FontFamily = new[] { "Share Tech Mono", "monospace" },
                    TextTransform = "uppercase"
                }
            }
        };
    }
}
