using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    /// <summary>
    /// Tiers are a global lookup, not tenant data, so the read endpoints are public.
    /// This is only safe while <c>ITierService</c> stays
    /// read-only (<c>IDLReadableService&lt;Tier&gt;</c>): the controller generator emits
    /// no POST/PUT/DELETE actions for it. If a write surface is ever added, remove
    /// <see cref="AllowAnonymousAttribute"/> or put <c>[Authorize]</c> on the writes.
    /// </summary>
    [AllowAnonymous]
    public partial class TierController
    {

    }
}