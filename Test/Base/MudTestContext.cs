using _0nline.Biller.Wasm.Client.DL;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Client.Services;
using _0nline.Shared.Contract.Client.Interfaces;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;


namespace _0nline.Biller.Test.Base
{
    public abstract class MudTestContext : BunitContext, IAsyncDisposable
    {
        protected MudTestContext()
        {
            Services.AddMudServices();
            Services.AddSingleton(TimeProvider.System);
            Services.AddBlazoredLocalStorage();
            Services.AddSingleton<IPhoneNumberService, PhoneNumberService>();
            Services.AddSingleton<IImageUploadService<Image>, ImageServiceClient>();

            // Stub out common MudBlazor JSInterop calls
            JSInterop.SetupVoid("mudElementRef.addOnBlurEvent", _ => true);
            JSInterop.SetupVoid("mudElementRef.addOnFocusEvent", _ => true);
            JSInterop.SetupVoid("mudElementRef.addOnClickEvent", _ => true);

            JSInterop.SetupVoid("mudPopover.initialize", _ => true);
            JSInterop.SetupVoid("mudPopover.open", _ => true);
            JSInterop.SetupVoid("mudPopover.close", _ => true);

            JSInterop.SetupVoid("mudResizeObserver.observe", _ => true);
            JSInterop.SetupVoid("mudResizeObserver.unobserve", _ => true);

            JSInterop.SetupVoid("mudScrollManager.scrollTo", _ => true);

            JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
            JSInterop.SetupVoid("mudKeyInterceptor.disconnect", _ => true);

        }

        public new async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

}
