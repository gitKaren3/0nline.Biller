namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// The payee details printed on an invoice. Copied into the snapshot when the
    /// invoice is issued so a later bank change cannot rewrite an old invoice.
    /// </summary>
    public sealed class InvoicePdfBank
    {
        public string AccountName { get; init; } = string.Empty;

        public string BankName { get; init; } = string.Empty;

        public string AccountNumber { get; init; } = string.Empty;

        public string? BranchCode { get; init; }

        public string? AccountType { get; init; }

        public string? SwiftCode { get; init; }
    }
}
