using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm.Client.DL;
using _0nline.Biller.Wasm.Components;
using _0nline.Shared.Contract.Client.Interfaces;
using _0nline.Shared.Contract.Models;
using Bunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace _0nline.Biller.Test;

public class CompanyFormTests : Base.MudTestContext, IAsyncLifetime
{
    private readonly RecordingImageService _images = new();

    public CompanyFormTests()
    {
        var http = new HttpClient { BaseAddress = new Uri("http://localhost/api/biller/image/") };
        var options = Options.Create(new DLClientOptions { ApiBaseUrl = "http://localhost/api/biller/" });

        Services.AddSingleton(new ImageServiceClient(http, options));
        Services.AddSingleton<IImageService>(_images);
        Services.AddSingleton<IImageUploadService<Image>>(sp => sp.GetRequiredService<ImageServiceClient>());

        // Cleanup calls MudBlazor makes when the logo picker is disposed.
        JSInterop.SetupVoid("mudElementRef.removeOnBlurEvent", _ => true);
        JSInterop.SetupVoid("mudElementRef.removeOnFocusEvent", _ => true);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void UploaderSurvivesParentRerenderWhenReplacingExistingLogo()
    {
        var company = new Company
        {
            ID = 5,
            Name = "Acme",
            LogoImageID = 7,
            LogoImage = new Image { ID = 7 }
        };

        var cut = Render<CompanyForm>(p => p.Add(c => c.Model, company));

        // Existing logo: preview + Replace, no picker.
        Assert.Empty(cut.FindAll("input[type=file]"));
        cut.FindAll("button").First(b => b.TextContent.Contains("Replace")).Click();
        Assert.Single(cut.FindAll("input[type=file]"));

        // TenantForm re-renders while saving (e.g. _isSubmitting flips).
        cut.Render(p => p.Add(c => c.Model, company));

        Assert.Single(cut.FindAll("input[type=file]"));
    }

    [Fact]
    public async Task RemovingLogoRetiresTheExistingImage()
    {
        var company = new Company
        {
            ID = 5,
            Name = "Acme",
            LogoImageID = 7,
            LogoImage = new Image { ID = 7 }
        };

        var cut = Render<CompanyForm>(p => p.Add(c => c.Model, company));

        cut.FindAll("button").First(b => b.TextContent.Contains("Remove")).Click();

        Assert.Null(company.LogoImageID);
        Assert.Null(company.LogoImage);

        Assert.True(await cut.Instance.CommitLogoAsync());
        // The image is only removed after the company save succeeds, so the
        // stored row cannot end up pointing at a deleted image.
        Assert.Empty(_images.Removed);

        await cut.Instance.FinalizeLogoAsync();
        Assert.Equal(new[] { 7 }, _images.Removed);
    }

    private sealed class RecordingImageService : IImageService
    {
        public List<int> Deactivated { get; } = new();
        public List<int> Removed { get; } = new();

        public Task<Result<Image?>> DeleteOrDeactivateAsync(int id)
        {
            Deactivated.Add(id);
            return Task.FromResult(Result<Image?>.Success());
        }

        public Task<Result<Image>> UploadAndRegisterImageAsync(IFormFile file, string category, string caption, bool ispublic)
            => throw new NotSupportedException();

        public Task<Result> RemoveAndDeRegisterImageAsync(int imageId)
        {
            Removed.Add(imageId);
            return Task.FromResult(Result.Success());
        }

        public Task<Result<Image?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Image[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => throw new NotSupportedException();
        public Task<Result<Image>> CreateAsync(Image entity) => throw new NotSupportedException();
        public Task<Result<Image>> UpdateAsync(Image entity) => throw new NotSupportedException();
    }
}
