namespace _0nline.Biller.Wasm.Client.Themes
{
    /// <summary>
    /// Biller's product design tokens.
    /// <para>
    /// Biller owns its own accent so it is recognisable as a distinct app. The 0nline wordmark
    /// takes the palette's accent like everything else, so the whole app reads as one colour
    /// scheme. Every colour here is chosen so that white text on a fill clears WCAG AA (4.5:1)
    /// and text on a page surface clears it too.
    /// </para>
    /// </summary>
    public static class BillerTokens
    {
        /// <summary>
        /// Biller's accent: app bar, primary actions, active navigation, focus rings
        /// (light mode).
        /// </summary>
        public const string Accent = "#5B4BB5";

        /// <summary>
        /// Light tint of <see cref="Accent"/>, used for accent-coloured text and icons on dark
        /// surfaces (dark mode).
        /// </summary>
        public const string AccentOnDark = "#A5B4FC";

        /// <summary>
        /// Contrast colour for content sitting on top of <see cref="Accent"/> fills.
        /// </summary>
        public const string AccentContrast = "#FFFFFF";

        /// <summary>
        /// Contrast colour for content sitting on top of <see cref="AccentOnDark"/> fills.
        /// </summary>
        public const string AccentOnDarkContrast = "#0F0F14";

        /// <summary>
        /// Biller's warm companion to the indigo accent, used for secondary actions and
        /// accents: the image uploader button, secondary buttons, the tagline.
        /// <para>
        /// Replaces the 0nline coral, which failed contrast against white text (2.73:1).
        /// </para>
        /// </summary>
        public const string Bronze = "#9A6412";

        /// <summary>
        /// Contrast colour for content sitting on top of <see cref="Bronze"/> fills.
        /// </summary>
        public const string BronzeContrast = "#FFFFFF";

        /// <summary>
        /// Biller's error colour, deliberately distinct from <see cref="Bronze"/> so that an
        /// error is never confused with a brand accent.
        /// </summary>
        public const string Error = "#C6412E";

        /// <summary>
        /// Contrast colour for content sitting on the shared palette's light success, warning
        /// and tertiary fills. White text on those colours fails WCAG AA.
        /// </summary>
        public const string OnLightFillContrast = "#212121";

        /// <summary>
        /// Biller's info colour, darkened so that white text clears WCAG AA.
        /// </summary>
        public const string Info = "#0277BD";

        /// <summary>Dark mode page background.</summary>
        public const string DarkBackground = "#0F0F14";

        /// <summary>Dark mode card and container surface.</summary>
        public const string DarkSurface = "#1C1C24";

        /// <summary>Dark mode navigation drawer background.</summary>
        public const string DarkDrawer = "#16161C";
    }
}
