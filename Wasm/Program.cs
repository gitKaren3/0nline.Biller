using _0nline.Auth.Contract;
using _0nline.Shared.Client.Services;
using _0nline.Shared.Client.UI;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Client.Interfaces;
using _0nline.Shared.Contract.Client.Models;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm;
using _0nline.Biller.Wasm.Client.Auth;
using _0nline.Biller.Wasm.Client.DL;
using _0nline.Biller.Wasm.Client.Utils;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;



var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Named HttpClient for APIs
//builder.Services.AddHttpClient("BillerApi", client =>
//{
//    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
//});

// Default HttpClient for Blazor itself
//builder.Services.AddScoped(sp => new HttpClient
//{
//    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
//});

builder.Services.AddOidcAuthentication(options =>
{
    // Configure your authentication provider options here.
    // For more information, see https://aka.ms/blazor-standalone-auth
    builder.Configuration.Bind("Local", options.ProviderOptions);
});

//MudBlazor
builder.Services.AddMudServices();

builder.Services.AddBlazoredLocalStorage();

#region * Error Logging *

builder.Logging.SetMinimumLevel(LogLevel.Debug);
//builder.Services.AddSingleton<ILoggerProvider, ApiLoggerProvider>();

#endregion // .......................................................................................

#region * Localization *
builder.Services.AddLocalization(options => options.ResourcesPath = "");   // took out the folder name becuase it was being appended to the assembly name instead of the root namespace, which was causing it to not find the resources.
// Optional: set default culture (commented out because it broke something)
//CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en");
//CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en");
#endregion // .......................................................................................

#region * Theme *
// use ThemeSettings from appsettings.json to configure the theme service (will default to the values in the ThemeSettings constructor if not provided)
builder.Services.Configure<ThemeSettings>(builder.Configuration.GetSection("ThemeSettings"));
builder.Services.AddScoped<IThemeService, ThemeService>();
#endregion // .......................................................................................

#region * Auth *

builder.Services.AddTransient<AuthMessageHandler>();
builder.Services.AddTransient<TenantMessageHandler>();

builder.Services.Configure<AuthClientOptions>(builder.Configuration.GetSection("AuthClient"));

//builder.Services.AddHttpClient<IAuthHttpClient, AuthHttpClient>()
//    .AddHttpMessageHandler<AuthMessageHandler>();
////builder.Services.AddTransient<IAuthHttpClient, AuthHttpClient>();
//builder.Services.AddScoped<IAuthenticationService, AuthClient>();
//
builder.Services.AddHttpClient<IAuthenticationService, BillerAuthClient>();

builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton<UserSession>();  // for wasm this is per user, forcing persistence
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthStateProvider>();

builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
})
.AddHttpMessageHandler<AuthMessageHandler>();

#endregion // .......................................................................................

#region * DL **  

builder.Services.Configure<DLClientOptions>(builder.Configuration.GetSection("DLClient"));

builder.Services.AddHttpClient<ITenantService,TenantServiceClient>()
    .AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<ITierService, TierServiceClient>()
    .AddHttpMessageHandler<AuthMessageHandler>();

builder.Services.AddHttpClient<ICompanyService, CompanyServiceClient>()
    .AddHttpMessageHandler<AuthMessageHandler>()
    .AddHttpMessageHandler<TenantMessageHandler>()
    ;
builder.Services.AddHttpClient<IContactService, ContactServiceClient>()
    .AddHttpMessageHandler<AuthMessageHandler>()
    .AddHttpMessageHandler<TenantMessageHandler>()
    ;
builder.Services.AddHttpClient<IImageService,ImageServiceClient>()
    .AddHttpMessageHandler<AuthMessageHandler>()
    .AddHttpMessageHandler<TenantMessageHandler>();
builder.Services.AddHttpClient<IImageUploadService<Image>, ImageServiceClient>()
    .AddHttpMessageHandler<AuthMessageHandler>()
    .AddHttpMessageHandler<TenantMessageHandler>();

#endregion // .......................................................................................

#region * Utility Api Clients **
builder.Services.AddHttpClient<IMailService,MailServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
})
.AddHttpMessageHandler<AuthMessageHandler>()
.AddHttpMessageHandler<TenantMessageHandler>();


#endregion // .......................................................................................

#region " Client Services "
builder.Services.AddSingleton<IPhoneNumberService, PhoneNumberService>();
#endregion

try
{
    await builder.Build().RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Startup error: {ex}");
}

