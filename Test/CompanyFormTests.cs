using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm.Client.DL;
using _0nline.Biller.Wasm.Components;
using _0nline.Shared.Contract.Client.Interfaces;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace _0nline.Biller.Test;

public class CompanyFormTests : Base.MudTestContext, IAsyncLifetime
{
    public CompanyFormTests()
    {
        var http = new HttpClient { BaseAddress = new Uri("http://localhost/api/biller/image/") };
        var options = Options.Create(new DLClientOptions { ApiBaseUrl = "http://localhost/api/biller/" });

        Services.AddSingleton(new ImageServiceClient(http, options));
        Services.AddSingleton<IImageService>(sp => sp.GetRequiredService<ImageServiceClient>());
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
}
