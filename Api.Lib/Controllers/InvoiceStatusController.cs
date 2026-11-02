using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace _0nline.Biller.Api.Lib
{
    /// <summary>
    /// Invoice statuses are a global lookup, not tenant data, so the read endpoints are public.
    /// This is only safe while <c>IInvoiceStatusService</c> stays
    /// read-only (<c>IDLReadableService&lt;InvoiceStatus&gt;</c>): the controller generator emits
    /// no POST/PUT/DELETE actions for it.
    /// </summary>
    [AllowAnonymous]
    public partial class InvoiceStatusController
    {

    }
}