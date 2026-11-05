namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// What the tenant sees about a link they sent: when it was first opened and
    /// how many times, as far as HTTP can tell. This is "the link was opened",
    /// not "the customer read the invoice" — a mail scanner can fetch it too.
    /// </summary>
    public sealed class InvoiceSnapshotActivity
    {
        public Guid? Token { get; init; }

        public DateTime CreatedAt { get; init; }

        public DateTime? FirstAccessedAt { get; init; }

        public DateTime? LastAccessedAt { get; init; }

        public DateTime? RevokedAt { get; init; }

        /// <summary>
        /// Opens, not requests: the service ignores range requests inside a
        /// debounce window so one PDF view does not count many times.
        /// </summary>
        public int AccessCount { get; init; }

        public bool Opened => FirstAccessedAt is not null;

        public bool IsLive => RevokedAt is null;
    }
}
