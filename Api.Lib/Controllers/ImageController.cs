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
        public async Task<IActionResult> Upload([FromForm] IFormFile file, [FromForm] string? category, [FromForm] string? caption, [FromForm] bool ispublic)
        {
            Console.WriteLine($"API upload endpoint reached.Received file: {file?.FileName}, Category: {category}, Caption: {caption}, IsPublic: {ispublic}");
            if (file == null || file.Length == 0)
                throw new ArgumentNullException("No file uploaded.");
                
            var result = await BaseService.UploadAndRegisterImageAsync(file, category ?? string.Empty, caption ?? string.Empty, ispublic);
            return Ok(result);
        }

        [HttpDelete("remove/{imageId}")]
        public async Task<IActionResult> Remove([FromRoute] int imageId)
        {
            var result = await BaseService.RemoveAndDeRegisterImageAsync(imageId);
            return Ok(result);
        }
    }
}