using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Models;
using Microsoft.Extensions.Options;

namespace _0nline.Biller.Wasm.Client.DL
{
    public class InvoiceServiceClient : DLHttpClient<Invoice>, IInvoiceService
    {
        public InvoiceServiceClient(HttpClient http, IOptions<DLClientOptions> options)
            : base(http, options, "invoice")
        {
        }

        public new Task<Result<Invoice[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public Task<Result<PageOf<Invoice>>> GetRecentPageAsync(int page, int pageSize)
            => GetAndHandleAsync<PageOf<Invoice>>(
                $"page/recent?page={page}&pageSize={pageSize}");

        public Task<Result<PageOf<Invoice>>> SearchPageAsync(
            InvoiceSearch search, int page, int pageSize)
        {
            ArgumentNullException.ThrowIfNull(search);

            var query = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search.Text))
                query.Add($"text={Uri.EscapeDataString(search.Text.Trim())}");

            if (search.From is { } from)
                query.Add($"from={from:yyyy-MM-dd}");

            if (search.To is { } to)
                query.Add($"to={to:yyyy-MM-dd}");

            if (search.Statuses is { Length: > 0 } statuses)
            {
                foreach (var status in statuses)
                    query.Add($"statuses={status}");
            }

            return GetAndHandleAsync<PageOf<Invoice>>(
                $"page/search?{string.Join("&", query)}");
        }

        public new Task<Result<Invoice>> CreateAsync(Invoice entity) => base.CreateAsync(entity);
        public new Task<Result<Invoice>> UpdateAsync(Invoice entity) => base.UpdateAsync(entity);
        public new Task<Result<Invoice?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        public Task<Result<Invoice>> FinalizeAsync(int id)
            => PostAndHandleAsync<Invoice>($"{id}/finalize");

        public Task<Result<byte[]>> PreviewAsync(int id)
            => GetBytesAsync($"{id}/preview.pdf");

        public Task<Result<InvoiceNumberPreview>> PeekInvoiceNumberAsync()
            => GetAndHandleAsync<InvoiceNumberPreview>("next-number");

        public Task<Result<bool>> InvoiceNumberExistsAsync(string? number, int excludeInvoiceId = 0)
            => GetAndHandleAsync<bool>(
                $"number-exists?number={Uri.EscapeDataString(number?.Trim() ?? string.Empty)}&id={excludeInvoiceId}");

        public Task<Result<decimal?>> LastQuantityAsync(
            int contactId, int? companyId, short rateTypeId, int excludeInvoiceId = 0)
            => GetAndHandleAsync<decimal?>(
                $"last-qty?contactId={contactId}&companyId={companyId?.ToString() ?? string.Empty}"
                + $"&rateTypeId={rateTypeId}&excludeId={excludeInvoiceId}");
    }
}
