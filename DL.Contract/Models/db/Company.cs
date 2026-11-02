using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _0nline.Biller.DL.Contract.Models.db
{
    public partial class Company
    {
        public string? PhoneNo { 
            get { return Phone?.ToString("+0"); } 
            set { Phone = string.IsNullOrEmpty(value) ? null : long.Parse(value); } 
        }

        /// <summary>
        /// Detailed read model: the contacts linked to this company. Populated by
        /// <c>CompanyService</c> when a single company is loaded; not a database column,
        /// so it is excluded from insert and update field lists.
        /// </summary>
        [NotMapped]
        [Editable(false)]
        public List<Contact> Contacts { get; set; } = new();
    }
}
