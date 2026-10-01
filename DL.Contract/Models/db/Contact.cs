using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _0nline.Shared.Contract.Interfaces;

namespace _0nline.Biller.DL.Contract.Models.db
{
    public partial class Contact : IContact
    {
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
