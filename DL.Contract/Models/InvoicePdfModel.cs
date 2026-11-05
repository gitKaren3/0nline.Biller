using _0nline.Shared.Contract.Interfaces;

namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// An invoice as it should be rendered, frozen at issue time.
    /// </summary>
    /// <remarks>
    /// Self-contained on purpose: rendering it touches no database, so a link
    /// mailed years ago still produces the same document after the tenant changes
    /// banks, the customer is renamed or the invoice row is edited.
    /// </remarks>
    public sealed class InvoicePdfModel : IPdfDocumentModel
    {
        /// <summary>
        /// The shape of this model. Stored beside the snapshot so a future renderer
        /// can tell which contract the JSON was written under.
        /// </summary>
        public const short CurrentLayoutVersion = 1;

        public string InvoiceNumber { get; init; } = string.Empty;

        public DateTime InvoiceDate { get; init; }

        public string? PaymentRef { get; init; }

        public string? StatusName { get; init; }

        public InvoicePdfParty Issuer { get; init; } = new();

        public InvoicePdfParty Customer { get; init; } = new();

        public InvoicePdfBank? Bank { get; init; }

        public List<InvoicePdfLine> Lines { get; init; } = new();

        public decimal TotalTax { get; init; }

        public decimal Total { get; init; }
    }
}
