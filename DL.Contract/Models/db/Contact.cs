using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using _0nline.Shared.Contract.Interfaces;

namespace _0nline.Biller.DL.Contract.Models.db
{
    public partial class Contact : IContact
    {
        /// <summary>
        /// Cell number is part of the minimum contact set, mirroring IContact.
        /// There is no NOT NULL constraint on the column, so this is a capture-time rule only.
        /// </summary>
        [Required(ErrorMessage = "Required")]
        public string? CellNo 
        {
            get { return CellPhone?.ToString(); }
            set { CellPhone = string.IsNullOrEmpty(value) ? null : long.Parse(value); }
        }

        public string? BusinessNo
        {
            get { return  BusinessPhone?.ToString(); }
            set { BusinessPhone = string.IsNullOrEmpty(value) ? null : long.Parse(value); }
        }
    } 
}
