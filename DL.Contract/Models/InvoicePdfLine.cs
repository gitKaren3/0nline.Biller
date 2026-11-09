namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>One billed line. The unit price is tax-inclusive.</summary>
    public sealed class InvoicePdfLine
    {
        public string Description { get; init; } = string.Empty;

        /// <summary>The rate type's name, when it explains the line.</summary>
        public string? RateName { get; init; }

        public decimal Qty { get; init; }

        public decimal UnitPrice { get; init; }

        /// <summary>Tax already contained in <see cref="UnitPrice"/> for this line.</summary>
        public decimal Tax { get; init; }

        /// <summary>The rate the tax was calculated at, frozen with the line.</summary>
        public decimal TaxRate { get; init; }

        /// <summary>The scheme name for this line (VAT, GST, …).</summary>
        public string? TaxSchemeName { get; init; }

        /// <summary>An optional explanation for a zero-rated or exempt line.</summary>
        public string? TaxNote { get; init; }

        public decimal Amount => Qty * UnitPrice;
    }
}
