using System.Security.Claims;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm.Pages;
using _0nline.Shared.Contract.Models;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace _0nline.Biller.Test;

/// <summary>
/// The invoice editor's number field: a new invoice shows the number it is about to be
/// given, a clash is annotated on the field, and the regenerate action replaces it.
/// </summary>
public class InvoiceNumberPageTests : Base.MudTestContext, IAsyncLifetime
{
    private readonly FakeInvoiceService _invoices = new();

    public InvoiceNumberPageTests()
    {
        Services.AddAuthorizationCore();

        // bUnit registers a placeholder authorization service that throws. The page's
        // AuthorizeView only needs "is the user signed in", so allow everything.
        Services.AddSingleton<IAuthorizationService, AllowAllAuthorizationService>();
        Services.AddSingleton<AuthenticationStateProvider>(new SignedInAuthStateProvider());
        Services.AddSingleton<IInvoiceService>(_invoices);
        Services.AddSingleton<ITenantSettingService>(new FakeTenantSettingService());
        Services.AddSingleton<IContactService>(new FakeContactService());
        Services.AddSingleton<ICompanyService>(new FakeCompanyService());
        Services.AddSingleton<ITenantRateService>(new FakeTenantRateService());
        Services.AddSingleton<IRateTypeService>(new FakeRateTypeService());
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void ANewInvoiceShowsTheNumberItWillBeGiven()
    {
        _invoices.NextNumbers.Enqueue("#00086");

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
            Assert.Equal("#00086", cut.Find("#invoice-number").GetAttribute("value")));
    }

    [Fact]
    public void ATakenNumberIsAnnotatedWhenTheFieldIsLeft()
    {
        _invoices.NextNumbers.Enqueue("#00086");
        _invoices.Taken.Add("INV-9");

        var cut = RenderPage();
        cut.WaitForAssertion(() =>
            Assert.Equal("#00086", cut.Find("#invoice-number").GetAttribute("value")));

        cut.Find("#invoice-number").Change("INV-9");
        cut.Find("#invoice-number").Blur(new FocusEventArgs());

        cut.WaitForAssertion(() => Assert.Contains("INV-9 is already used.", cut.Markup));
    }

    [Fact]
    public void ANewInvoiceFillsThePaymentReferenceWithItsNumber()
    {
        _invoices.NextNumbers.Enqueue("#00086");

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
            Assert.Equal("#00086", cut.Find("#payment-ref").GetAttribute("value")));
    }

    [Fact]
    public void RegenerateReplacesTheNumber()
    {
        _invoices.NextNumbers.Enqueue("#00086");
        _invoices.NextNumbers.Enqueue("#00087");

        var cut = RenderPage();
        cut.WaitForAssertion(() =>
            Assert.Equal("#00086", cut.Find("#invoice-number").GetAttribute("value")));

        cut.Find("button[title='Use the next number']").Click();

        cut.WaitForAssertion(() =>
            Assert.Equal("#00087", cut.Find("#invoice-number").GetAttribute("value")));
        Assert.Equal("#00087", cut.Find("#payment-ref").GetAttribute("value"));
    }

    [Fact]
    public void EditingTheNumberAlsoUpdatesThePaymentReference()
    {
        _invoices.NextNumbers.Enqueue("#00086");

        var cut = RenderPage();
        cut.WaitForAssertion(() =>
            Assert.Equal("#00086", cut.Find("#invoice-number").GetAttribute("value")));

        cut.Find("#invoice-number").Change("#00099");

        cut.WaitForAssertion(() =>
            Assert.Equal("#00099", cut.Find("#payment-ref").GetAttribute("value")));
    }

    /// <summary>The page renders inside AuthorizeView, which needs a cascading auth state.</summary>
    private IRenderedComponent<CascadingAuthenticationState> RenderPage()
        => Render<CascadingAuthenticationState>(parameters => parameters
            .AddChildContent<Invoice_Page>(page => page.Add(component => component.Id, 0)));

    private sealed class FakeInvoiceService : IInvoiceService
    {
        public Queue<string> NextNumbers { get; } = new();
        public HashSet<string> Taken { get; } = new(StringComparer.Ordinal);

        public Task<Result<InvoiceNumberPreview>> PeekInvoiceNumberAsync()
            => Task.FromResult(Result<InvoiceNumberPreview>.Success(new InvoiceNumberPreview
            {
                Number = NextNumbers.Count > 0 ? NextNumbers.Dequeue() : "#00001",
                Counter = 1,
                Format = "#{n:00000}"
            }));

        public Task<Result<bool>> InvoiceNumberExistsAsync(string? number, int excludeInvoiceId = 0)
            => Task.FromResult(Result<bool>.Success(Taken.Contains(number ?? string.Empty)));

        public Task<Result<Invoice?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Invoice[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => throw new NotSupportedException();
        public Task<Result<Invoice>> CreateAsync(Invoice entity) => throw new NotSupportedException();
        public Task<Result<Invoice>> UpdateAsync(Invoice entity) => throw new NotSupportedException();
        public Task<Result<Invoice?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
        public Task<Result<Invoice>> FinalizeAsync(int id) => throw new NotSupportedException();
        public Task<Result<byte[]>> PreviewAsync(int id) => throw new NotSupportedException();
    }

    private sealed class FakeTenantSettingService : ITenantSettingService
    {
        private static readonly TenantSetting Settings = new()
        {
            ID = 1,
            TenantID = 19,
            CurrencyCode = "ZAR",
            CurrencyMinorUnits = 2,
            TimeZoneId = "UTC",
            DefaultTaxRate = 15m,
            InvoiceNumberFormat = "#{n:00000}",
            NextInvoiceNumber = 86
        };

        public Task<Result<TenantSetting>> GetForTenantAsync()
            => Task.FromResult(Result<TenantSetting>.Success(Settings));

        public Task<Result<TenantSetting?>> GetOneAsync(int id)
            => Task.FromResult(Result<TenantSetting?>.Success(Settings));

        public Task<Result<TenantSetting>> EnsureForTenantAsync(int tenantId, long? userId)
            => Task.FromResult(Result<TenantSetting>.Success(Settings));

        public Task<Result<TenantSetting>> SaveAsync(TenantSetting setting)
            => Task.FromResult(Result<TenantSetting>.Success(Settings));
    }

    private sealed class FakeContactService : IContactService
    {
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(
                new Dictionary<int, string>()));

        public Task<Result<Contact?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Contact[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<Contact>> CreateAsync(Contact entity) => throw new NotSupportedException();
        public Task<Result<Contact>> UpdateAsync(Contact entity) => throw new NotSupportedException();
        public Task<Result<Contact?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
    }

    private sealed class FakeCompanyService : ICompanyService
    {
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(
                new Dictionary<int, string>()));

        public Task<Result<Company?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Company[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<Company>> CreateAsync(Company entity) => throw new NotSupportedException();
        public Task<Result<Company>> UpdateAsync(Company entity) => throw new NotSupportedException();
        public Task<Result<Company?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
    }

    private sealed class FakeRateTypeService : IRateTypeService
    {
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(
                new Dictionary<int, string>()));

        public Task<Result<RateType?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<RateType[]>> GetAllAsync() => throw new NotSupportedException();
    }

    private sealed class FakeTenantRateService : ITenantRateService
    {
        public Task<Result<TenantRate[]>> GetAllAsync()
            => Task.FromResult(Result<TenantRate[]>.Success(Array.Empty<TenantRate>()));

        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(
                new Dictionary<int, string>()));

        public Task<Result<TenantRate?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<TenantRate>> CreateAsync(TenantRate entity) => throw new NotSupportedException();
        public Task<Result<TenantRate>> UpdateAsync(TenantRate entity) => throw new NotSupportedException();
        public Task<Result<TenantRate?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
    }

    private sealed class AllowAllAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    private sealed class SignedInAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "7"),
                    new Claim(ClaimTypes.Name, "harriet@flamingo.co.za"),
                    new Claim("tenantId", "19")
                },
                "test");

            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }
}
