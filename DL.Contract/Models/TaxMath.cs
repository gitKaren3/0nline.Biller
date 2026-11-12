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

        /// <summary>
        /// The tax added to a net amount, rounded to the currency's minor units. The
        /// gross is always <c>net + tax</c>, so editing either side of a tax-inclusive
        /// field keeps the two numbers adding up exactly.
        /// </summary>
        public static decimal TaxFromNet(decimal net, decimal ratePercent, int minorUnits)
        {
            if (net == 0m || ratePercent <= 0m)
                return 0m;

            decimal raw = net * ratePercent / 100m;
            return decimal.Round(raw, Math.Clamp(minorUnits, 0, 4), MidpointRounding.AwayFromZero);
        }

        /// <summary>The tax-inclusive amount for a net amount at the given rate.</summary>
        public static decimal GrossFromNet(decimal net, decimal ratePercent, int minorUnits)
            => net + TaxFromNet(net, ratePercent, minorUnits);

        /// <summary>The amount excluding tax, given the gross and the extracted tax.</summary>
        public static decimal NetFromInclusive(decimal gross, decimal tax) => gross - tax;
    }
}
