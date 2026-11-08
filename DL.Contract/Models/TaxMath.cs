namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// Tax arithmetic for tax-inclusive amounts. A line's price already contains the
    /// tax, so the tax is extracted from the gross rather than added on top.
    /// </summary>
    public static class TaxMath
    {
        /// <summary>
        /// The tax contained in <paramref name="gross"/> at <paramref name="ratePercent"/>.
        /// The result is rounded to the currency's minor units, and the net amount is
        /// always <c>gross - tax</c>, so the two still add up to the printed total.
        /// </summary>
        public static decimal TaxFromInclusive(decimal gross, decimal ratePercent, int minorUnits)
        {
            if (gross == 0m || ratePercent <= 0m)
                return 0m;

            decimal raw = gross * ratePercent / (100m + ratePercent);
            return decimal.Round(raw, Math.Clamp(minorUnits, 0, 4), MidpointRounding.AwayFromZero);
        }

        /// <summary>The amount excluding tax, given the gross and the extracted tax.</summary>
        public static decimal NetFromInclusive(decimal gross, decimal tax) => gross - tax;
    }
}
