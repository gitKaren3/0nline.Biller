namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// What the signed-in caller receives when an invoice is issued: the capability
    /// token and the relative path that serves the PDF anonymously.
    /// </summary>
    public sealed class InvoiceSnapshotLink
    {
        public Guid Token { get; init; }

        /// <summary>
        /// The absolute link to mail, built from the request's scheme and host.
        /// </summary>
        public string Url { get; init; } = string.Empty;

        public DateTime CreatedAt { get; init; }
    }
}
