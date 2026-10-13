using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Models
{
    public class TenantRegistrationResult
    {
        public Tenant Tenant { get; set; } = new();
        public string? Token { get; set; }
    }
}
