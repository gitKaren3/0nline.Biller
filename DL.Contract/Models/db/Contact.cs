using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _0nline.Biller.DL.Contract.Models.db
{
    public partial class Contact
    {
        public string? CellPhoneStr 
        {
            get { return CellPhone.ToString(); }
            set { CellPhone = string.IsNullOrEmpty(value) ? null : long.Parse(value); }
        }

        public string? BusinessPhoneStr
        {
            get { return BusinessPhone.ToString(); }
            set { BusinessPhone = string.IsNullOrEmpty(value) ? null : long.Parse(value); }
        }
    } 
}
