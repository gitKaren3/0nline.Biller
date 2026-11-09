using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace _0nline.Biller.DL.Contract.Models.db
{
    public partial class Invoice
    {
        /// <summary>
        /// Detailed read model: the lines that make up this invoice. Populated by
        /// <c>InvoiceService</c> when a single invoice is loaded; not a database
        /// column, so it is excluded from insert/update field lists.
        /// </summary>
        [NotMapped]
        [Editable(false)]
        public List<InvoiceLine> InvoiceLines { get; set; } = new();

        /// <summary>
        /// Alias for <see cref="InvoiceLines"/> for clients that use the shorter name.
        /// </summary>
        [NotMapped]
        [Editable(false)]
        public List<InvoiceLine> Lines
        {
            get => InvoiceLines;
            set => InvoiceLines = value ?? new();
        }

        /// <summary>
        /// The contact the picker sees: <c>null</c> means "not chosen yet". The column
        /// is a non-nullable <see cref="IssuedToContactID"/>, so an unsaved invoice
        /// carries 0 and a <c>MudSelect</c> would otherwise render the literal "0".
        /// The setter keeps the column in sync; the property is not a column, so it is
        /// kept out of the API payload and never written by the data layer.
        /// </summary>
        [Required(ErrorMessage = "Choose a contact to invoice.")]
        [NotMapped]
        [Editable(false)]
        [JsonIgnore]
        public int? IssuedToContactSelection
        {
            get => IssuedToContactID <= 0 ? null : IssuedToContactID;
            set => IssuedToContactID = value ?? 0;
        }
    }
}