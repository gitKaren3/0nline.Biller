using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    public interface ITenantRateService : IDLReadableService<TenantRate>, IDLWritableService<TenantRate>
    {
        /// <summary>
        /// The captured rates among <paramref name="ids"/> that belong to the caller's
        /// tenant. An id from another tenant, or one that does not exist, is absent from
        /// the result rather than an error.
        /// </summary>
        /// <remarks>
        /// This is a rate question, not a generic row question: an invoice line keeps the
        /// rate it was priced from, and a save compares that copy against the captured
        /// row. It lives here because no other service has the same need, and the
        /// generic readable surface does not grow a shape for one caller. A default
        /// reads them one at a time so a client compiles; the server service answers it
        /// with the store's single set read.
        /// </remarks>
        async Task<Result<TenantRate[]>> GetCapturedAsync(IEnumerable<int> ids)
        {
            var rows = new List<TenantRate>();

            foreach (var id in ids)
            {
                var one = await GetOneAsync(id);
                if (one.Value is not null)
                    rows.Add(one.Value);
            }

            return Result<TenantRate[]>.Success(rows.ToArray());
        }
    }
}
