using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Models;
using System.Net.Http.Json;
using System.Text.Json;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace _0nline.Biller.Wasm.Client.Utils
{
    public class MailServiceClient : IMailService
    {
        protected readonly HttpClient Http;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public MailServiceClient(HttpClient http)
        {
            Http = http;
        }

        public Task<Result> SendBrandedMailAsync(string recipientEmail, string subject, string body)
        {
            throw new NotImplementedException();
        }

        public Task<Result> SendBrandedMailAsync(string[] recipients, string[] cc, string[] bcc, string subject, string body, params string[] fileattachments)
        {
            throw new NotImplementedException();
        }

        public async Task<Result> SendContactSupportEmailAsync(ContactUsModel messagedetails)
        {
            try
            {
                //TODO put the actual endpoint in the appsettings.json file and read it from there
                var response = await Http.PostAsJsonAsync(Http.BaseAddress, messagedetails);
                var result = await response.Content.ReadFromJsonAsync<Result>(JsonOptions);
                return result ?? Result.Failure(Guid.NewGuid().ToString(), "Deserialization failed");
            }
            catch (Exception ex)
            {
                return Result.Failure(Guid.NewGuid().ToString(), ex.Message);
            }
        }
    }
}
