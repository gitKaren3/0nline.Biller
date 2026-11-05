namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>One billed line. The unit price is tax-inclusive.</summary>
    public sealed class InvoicePdfLine
    {
        public string Description { get; init; } = string.Empty;

        /// <summary>The rate type's name, when it explains the line.</summary>
        public string? RateName { get; init; }

        public short Qty { get; init; }

        public decimal UnitPrice { get; init; }

        /// <summary>Tax already contained in <see cref="UnitPrice"/> for this line.</summary>
        public decimal Tax { get; init; }

        public decimal Amount => Qty * UnitPrice;
    }
}
