using Microsoft.AspNetCore.Mvc;
using _0nline.Biller.DL.Contract.Models.db;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;


namespace _0nline.Biller.Api.Lib
{
    [Authorize]
    public partial class ImageController : BillerBaseController<Image, int>
    {
      
        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? category, [FromForm] string? caption)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentNullException("No file uploaded.");
                
            var result = await DbService.UploadAndRegisterImageAsync(file, category ?? string.Empty, caption ?? string.Empty);
            return Ok(result);
        }
    }
}