namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// The filters the invoice list accepts.
    /// </summary>
    /// <remarks>
    /// Every member is optional. <see cref="Text"/> is one search term matched
    /// against the invoice number, the contact's name and the company's name; the
    /// remaining members are ANDed with it.
    /// </remarks>
    public sealed record InvoiceSearch
    {
        /// <summary>
        /// A substring matched case-insensitively against the invoice number, the
        /// contact's first or last name, or the company's name.
        /// </summary>
        public string? Text { get; init; }

        /// <summary>The earliest invoice date to include, inclusive.</summary>
        public DateTime? From { get; init; }

        /// <summary>The latest invoice date to include, inclusive.</summary>
        public DateTime? To { get; init; }

        /// <summary>The invoice status ids to include. Empty means every status.</summary>
        public short[]? Statuses { get; init; }
    }
}
