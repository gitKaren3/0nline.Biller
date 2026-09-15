using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _0nline.Biller.DL.Contract.Models.db
{
    public partial class Company
    {
        public string? PhoneStr { 
            get { return Phone?.ToString(); } 
            set { Phone = string.IsNullOrEmpty(value) ? null : long.Parse(value); } 
        }
    }
}
