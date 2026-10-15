using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace _0nline.Biller.DL.Services
{
    public class ImageService : BillerBaseService<Image>, IImageService, IDLReadableService<Image>, IDLWritableService<Image>
    {
        private readonly IFileStoreService _fileStoreService;

        public ImageService(IDbContextProvider<BillerDbConfig> ctx, ISqlQueryProvider<Image> queryService, ITenantContext tenantContext, IFileStoreService fileStoreService)
            : base(ctx, queryService, tenantContext)
        {
            _fileStoreService = fileStoreService;
        }

        public new Task<Result<IEnumerable<Image>>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Image>> CreateAsync(Image entity) => base.CreateAsync(entity);
        public new Task<Result<Image>> UpdateAsync(Image entity) => base.UpdateAsync(entity);
        public new Task<Result<Image?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        public async Task<Result<Image>> UploadAndRegisterImageAsync(IFormFile file, string category, string caption)
        {
            var tenantId = _tenantContext.TenantId;
            var appId = _tenantContext.AppId;

            if (!tenantId.HasValue || string.IsNullOrEmpty(appId))
            {
                throw new InvalidOperationException("Tenant context is missing.");
            }

            string subdir = "images";
            if (category != string.Empty) subdir += $"/{category}";

            // 1. Save physical file using the shared service (public for direct browser access)
            var storeResult = await _fileStoreService.SaveAsync(file, appId, tenantId.Value, subdir, isPublic: true);

            // 2. Create DB record
            var image = new Image
            {
                TenantID = tenantId,
                Url = storeResult.RelativePath,
                Caption = caption
            };

            try
            {
                int count = await AddAsync(image);
                return Result<Image>.Success(image);
            }
            catch (Exception)
            {
                // 3. Cleanup: Delete the file if DB registration fails to avoid orphaned files
                await _fileStoreService.DeleteAsync(storeResult.RelativePath);
                
                throw;       // Global exception handling logs and returns a failure result to the caller
            }
        }


        public async Task<Result> RemoveAndDeRegisterImageAsync(int imageId)
        { 
            var image = await GetByIdAsync(imageId);
            if (image != null)
            {
                string url = image?.Url ?? string.Empty;
                var count = await DeleteAsync(imageId);
                if (count > 0)
                {
                    bool success = await _fileStoreService.DeleteAsync(url, isPublic: true);

                    if (success)
                    {
                        return Result.Success();
                    }
                    else
                    {
                        return Result.Failure($"Failed to delete the physical file at {url}.");
                    }
                }

                return Result.Failure($"Failed to delete Image with ID {imageId} from the database.");
            }
            else
            {
                return Result.NotFound($"Image with ID {imageId} not found.");
            }
        }

        protected override Image Augment(Image image)
        {
            if ( image?.Url != null) 
                image.ResolvedUrl = _fileStoreService.ResolvePublicUrl(_tenantContext.AppId, image.Url);
            return image!;
        }

    }
}