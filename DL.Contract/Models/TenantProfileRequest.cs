namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// The only part of a tenant a client may set: which of its own contact and
    /// company records are the tenant's profile. Identity, tier and status are
    /// server-owned.
    /// </summary>
    public class TenantProfileRequest
    {
        public int? ContactID { get; set; }
        public int? CompanyID { get; set; }
    }
}
