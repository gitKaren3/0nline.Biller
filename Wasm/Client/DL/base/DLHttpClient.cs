using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Contract.Interfaces.DL;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace _0nline.Biller.Wasm.Client.DL
{
    public abstract class DLHttpClient<T> : DLHttpClient, IDLBaseService<T> where T : class
    {
        protected DLHttpClient(HttpClient http, IOptions<DLClientOptions> options, string controller)
            : base(http, options, controller)
        {
        }

        public async Task<Result<T?>> GetOneAsync(int id)
            => await GetAndHandleAsync<T?>($"{id}");

        protected async Task<Result<IEnumerable<T>>> GetAllAsync()
            => await GetAndHandleAsync<IEnumerable<T>>("");

        protected async Task<Result<IDictionary<int, string>>> GetLookupAsync()
            => await GetAndHandleAsync<IDictionary<int, string>>("lookup");

        protected async Task<Result<T>> CreateAsync(T entity)
            => await PostAndHandleAsync<T, T>("", entity);

        protected async Task<Result<T>> UpdateAsync(T entity)
            => await PutAndHandleAsync<T, T>("", entity);

        protected async Task<Result<T?>> DeleteOrDeactivateAsync(int id)
            => await DeleteAndHandleAsync<T>($"{id}");
    }

    public abstract class DLHttpClient
    {
        protected readonly HttpClient Http;
        private readonly DLClientOptions _options;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected DLHttpClient(HttpClient http, IOptions<DLClientOptions> options, string controller)
        {
            Http = http;
            _options = options.Value;

            // Add the controller to the base URL:
            Http.BaseAddress = new Uri($"{_options.ApiBaseUrl.TrimEnd('/')}/{controller.TrimStart('/')}/");                
        }

        protected async Task<Result<T>> GetAndHandleAsync<T>(string url)
        {
            try
            {
                var response = await Http.GetAsync(url);
                var result = await response.Content.ReadFromJsonAsync<Result<T>>(JsonOptions);
                return result ?? Result<T>.Failure(ErrorCode.ValidationFailed, "Deserialization failed");
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ErrorCode.ServerError, ex.Message);
            }
        }

        protected async Task<Result<TResponse>> PostAndHandleAsync<TRequest, TResponse>(string url, TRequest data)
        {
            try
            {
                var response = await Http.PostAsJsonAsync(url, data);
                var result = await response.Content.ReadFromJsonAsync<Result<TResponse>>(JsonOptions);
                return result ?? Result<TResponse>.Failure(ErrorCode.ValidationFailed, "Deserialization failed");
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Failure(ErrorCode.ServerError, ex.Message);
            }
        }

        protected async Task<Result<TResponse>> PutAndHandleAsync<TRequest, TResponse>(string url, TRequest data)
        {
            try
            {
                var response = await Http.PutAsJsonAsync(url, data);
                var result = await response.Content.ReadFromJsonAsync<Result<TResponse>>(JsonOptions);
                return result ?? Result<TResponse>.Failure(ErrorCode.ValidationFailed, "Deserialization failed");
            }
            catch (Exception ex)
            {
                return Result<TResponse>.Failure(ErrorCode.ServerError, ex.Message);
            }
        }

        protected async Task<Result<T?>> DeleteAndHandleAsync<T>(string url)
        {
            try
            {
                var response = await Http.DeleteAsync(url);
                var result = await response.Content.ReadFromJsonAsync<Result<T?>>(JsonOptions);
                return result ?? Result<T?>.Failure(ErrorCode.ServerError, "Deserialization failed");
            }
            catch (Exception ex)
            {
                return Result<T?>.Failure(ErrorCode.ServerError, ex.Message);
            }
        }

        protected async Task<T?> GetAsync<T>(string url)
        {
            return await Http.GetFromJsonAsync<T>(url);
        }

        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            var response = await Http.PostAsJsonAsync(url, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        protected async Task PutAsync<TRequest>(string url, TRequest data)
        {
            var response = await Http.PutAsJsonAsync(url, data);
            response.EnsureSuccessStatusCode();
        }

        protected async Task DeleteAsync(string url)
        {
            var response = await Http.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }
    }

}
