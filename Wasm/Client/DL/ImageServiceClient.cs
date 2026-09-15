using _0nline.Shared.Contract.Client.Models;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Client.Interfaces;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;


namespace _0nline.Biller.Wasm.Client.DL
{
    public class ImageServiceClient : DLHttpClient<Image>, IImageService, IImageUploadService<Image>
    {
        public ImageServiceClient(HttpClient http, IOptions<DLClientOptions> options) 
            : base(http, options, "image")
        {
          
        }

        public new Task<Result<IEnumerable<Image>>> GetAllAsync() => base.GetAllAsync();    
        public new Task<Result<IDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Image>> CreateAsync(Image entity) => base.CreateAsync(entity);
        public new Task<Result<Image?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);
        public new Task<Result<Image>> UpdateAsync(Image entity) => base.UpdateAsync(entity);


        public Task<Result<Image>> UploadAndRegisterImageAsync(IFormFile file, string category, string caption)
        {
            throw new NotImplementedException();   // Server side implementation is not available in the client project. Use UploadFileAsync instead.
        }

        public async Task<Result<Image>> UploadFileAsync(FileUploadRequest request)
        {
            using var content = new MultipartFormDataContent();

            // 10MB limit for images
            var fileStream = request.File.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.File.ContentType);

            content.Add(fileContent, "file", request.File.Name);

            // the controller signature must have both category and caption as nullable:
            if (!string.IsNullOrEmpty(request.Category))
                content.Add(new StringContent(request.Category), "category");

            if (!string.IsNullOrEmpty(request.Caption))
                content.Add(new StringContent(request.Caption), "caption");


            // Call the Biller-specific endpoint
            var response = await Http.PostAsync("upload", content);

            try
            {
                var result = await response.Content.ReadFromJsonAsync<Result<Image>>();
                if (result != null)
                    return result;
                else
                    return Result<Image>.Failure(ErrorCode.ServerError, "Failed to deserialize response.");
            }
            catch (Exception ex)
            {
                return Result<Image>.Failure(ErrorCode.ValidationFailed, ex.Message);
            }
        }

    }
}