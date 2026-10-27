using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    }
}