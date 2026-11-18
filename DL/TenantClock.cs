namespace _0nline.Biller.DL
{
    /// <summary>
    /// "Today" as a tenant sees it: a calendar date in the tenant's zone, falling back
    /// to UTC when the stored zone is missing or unknown.
    /// </summary>
    /// <remarks>
    /// Shared by the invoice date default and the number preview, so the two cannot
    /// disagree about which day an invoice is dated. A bad zone must not block an
    /// invoice, which is why the fallback is silent.
    /// </remarks>
    internal static class TenantClock
    {
        internal static DateTime TodayIn(string? timeZoneId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(timeZoneId)
                    && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out var zone))
                {
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
                }
            }
            catch
            {
                // An unusable zone falls back to UTC rather than blocking the invoice.
            }

            return DateTime.UtcNow.Date;
        }
    }
}
