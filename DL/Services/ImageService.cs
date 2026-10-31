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

        public new Task<Result<Image[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Image>> CreateAsync(Image entity) => base.CreateAsync(entity);
        public new Task<Result<Image>> UpdateAsync(Image entity) => base.UpdateAsync(entity);
        public new Task<Result<Image?>> DeleteOrDeactivateAsync(int id) => base.DeleteOrDeactivateAsync(id);

        public async Task<Result<Image>> UploadAndRegisterImageAsync(IFormFile file, string category, string caption, bool ispublic)
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
            var storeResult = await _fileStoreService.SaveAsync(file, appId, tenantId.Value, subdir, isPublic: ispublic);

            // 2. Create DB record
            var image = new Image
            {
                TenantID = tenantId.Value,
                Url = storeResult.RelativePath,
                Caption = caption,
                Public = ispublic
            };

            try
            {
                int count = await AddAsync(image);
                // Augment so the client can render the logo immediately; ResolvedUrl is
                // computed on read, and the upload response is the only read the client gets.
                return Result<Image>.Success(await SingleAugment(image));
            }
            catch (Exception)
            {
                // 3. Cleanup: Delete the file if DB registration fails to avoid orphaned files
                await _fileStoreService.DeleteAsync(StorePath(storeResult.RelativePath), isPublic: ispublic);

                throw;       // Global exception handling logs and returns a failure result to the caller
            }
        }


        public async Task<Result> RemoveAndDeRegisterImageAsync(int imageId)
        {
            var image = await GetByIdAsync(imageId);
            if (image != null)
            {
                string url = image?.Url ?? string.Empty;
                bool isPublic = image?.Public == true;
                var count = await DeleteAsync(imageId);
                if (count > 0)
                {
                    // FileStoreService stores under <base>/<appId>/<relativePath>,
                    // so the app segment is part of the delete path even though it
                    // is not part of the URL-shaped value on the row.
                    bool success = await _fileStoreService.DeleteAsync(StorePath(url), isPublic);

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

        private string StorePath(string relativePath)
            => string.IsNullOrEmpty(_tenantContext.AppId)
                ? relativePath
                : $"{_tenantContext.AppId}/{relativePath.TrimStart('/', '\\')}";

        protected override Task<Image> SingleAugment(Image image)
        {
            if (image?.Url != null )
                if (image?.Public == true && !image.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    image.ResolvedUrl = _fileStoreService.ResolvePublicUrl(_tenantContext.AppId, image.Url);
            return Task.FromResult(image!);
        }

        protected override Task<IEnumerable<Image>> BatchAugment(IEnumerable<Image> images)
        {
            foreach (var image in images)
            {
                if (image?.Public == true 
                    && (!image?.Url?.StartsWith("http", StringComparison.OrdinalIgnoreCase) ?? false))
                    image!.ResolvedUrl = _fileStoreService.ResolvePublicUrl(_tenantContext.AppId, image.Url! );
            }
            
            return Task.FromResult(images);
        }
    }
}