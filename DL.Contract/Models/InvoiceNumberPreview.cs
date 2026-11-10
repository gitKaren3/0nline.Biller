namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// The number the next invoice would draw, formatted with the tenant's format.
    /// Nothing is consumed: abandoning a new invoice must not burn a number.
    /// </summary>
    public class InvoiceNumberPreview
    {
        /// <summary>The formatted number, for example <c>#00086</c>.</summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>The counter value behind <see cref="Number"/>.</summary>
        public int Counter { get; set; }

        /// <summary>The tenant's normalised format, for example <c>#{n:00000}</c>.</summary>
        public string Format { get; set; } = string.Empty;
    }
}
