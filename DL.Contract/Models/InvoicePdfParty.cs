namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>The issuer or the customer side of an invoice.</summary>
    public sealed class InvoicePdfParty
    {
        public string Name { get; init; } = string.Empty;

        public string? Registration { get; init; }

        /// <summary>The party's tax registration number, when it has one.</summary>
        public string? TaxNumber { get; init; }

        /// <summary>The label for <see cref="TaxNumber"/> (VAT, GST, …).</summary>
        public string? TaxSchemeName { get; init; }

        /// <summary>The person to address, when it differs from <see cref="Name"/>.</summary>
        public string? ContactName { get; init; }

        public string? Email { get; init; }

        public string? Phone { get; init; }

        public string? Address { get; init; }

        public string? AddressCity { get; init; }

        public string? AddressCode { get; init; }

        public string? AddressCountry { get; init; }
    }
}
