using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Contract.Models.db
{
    /// <summary>
    /// An invoice line belongs to its invoice. It has no TenantID of its own, so it is
    /// only reachable through an invoice that was already tenant-filtered.
    /// </summary>
    /// <remarks>
    /// The generated half of this class is in <c>Generated/InvoiceLine.cs</c>; this part
    /// carries the hand-written declaration that the row is a child.
    /// </remarks>
    public partial class InvoiceLine : IDbChildEntity<Invoice>
    {
        /// <summary>
        /// The rate the line picker sees: <c>null</c> means "not chosen yet". The column
        /// is a non-nullable <see cref="TenantRateID"/>, so an unsaved line carries 0 and
        /// a <c>MudSelect</c> would otherwise render the literal "0". Not a column, so it
        /// is kept out of the payload and never written by the data layer.
        /// </summary>
        [Required(ErrorMessage = "Choose a rate.")]
        [NotMapped]
        [Editable(false)]
        [JsonIgnore]
        public int? RateSelection
        {
            get => TenantRateID <= 0 ? null : TenantRateID;
            set => TenantRateID = value ?? 0;
        }
    }
}
