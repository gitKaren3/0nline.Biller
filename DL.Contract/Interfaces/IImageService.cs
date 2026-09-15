using _0nline.Shared.Contract.Models;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Biller.DL.Contract.Models.db;
using Microsoft.AspNetCore.Http;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    public interface IImageService : IDLReadableService<Image>, IDLWritableService<Image>
    {
        Task<Result<Image>> UploadAndRegisterImageAsync(IFormFile file, string category, string caption);
    }
}