using System.Security.Claims;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Biller.Wasm.Pages;
using _0nline.Shared.Contract.Models;
using Bunit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace _0nline.Biller.Test;

/// <summary>
/// The invoice list pages through the app-specific "newest first" endpoint. The
/// generic paged read is key-ordered; this page asks for ID descending instead.
/// </summary>
public class InvoicesPageTests : Base.MudTestContext, IAsyncLifetime
{
    private readonly FakeInvoiceService _invoices = new();

    public InvoicesPageTests()
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, AllowAllAuthorizationService>();
        Services.AddSingleton<AuthenticationStateProvider>(new SignedInAuthStateProvider());
        Services.AddSingleton<IInvoiceService>(_invoices);
        Services.AddSingleton<IContactService>(new EmptyContactService());
        Services.AddSingleton<IInvoiceStatusService>(new EmptyInvoiceStatusService());
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void TheListShowsTheNewestPageAndCanAdvance()
    {
        var cut = Render<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<Invoices_Page>());

        cut.WaitForAssertion(() => Assert.Contains("Showing 1-10 of 15", cut.Markup));

        Assert.Contains("INV-15", cut.Markup);
        Assert.DoesNotContain("INV-5", cut.Markup);
        Assert.Equal((1, 10), _invoices.Requests[0]);

        cut.FindAll("button").Single(button => button.TextContent.Trim() == "2").Click();

        cut.WaitForAssertion(() => Assert.Contains("Showing 11-15 of 15", cut.Markup));

        Assert.Contains("INV-5", cut.Markup);
        Assert.DoesNotContain("INV-15", cut.Markup);
        Assert.Equal((2, 10), _invoices.Requests[^1]);
    }

    /// <summary>Fifteen invoices, newest first, divided into page size ten.</summary>
    private sealed class FakeInvoiceService : IInvoiceService
    {
        public List<(int Page, int PageSize)> Requests { get; } = new();

        public Task<Result<PageOf<Invoice>>> GetRecentPageAsync(int page, int pageSize)
        {
            Requests.Add((page, pageSize));

            var all = Enumerable.Range(1, 15)
                .OrderByDescending(id => id)
                .Select(id => new Invoice
                {
                    ID = id,
                    InvoiceNumber = $"INV-{id}",
                    InvoiceDate = DateTime.Today.AddDays(-id),
                    InvoiceStatusID = 1
                })
                .ToArray();

            var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
            return Task.FromResult(Result<PageOf<Invoice>>.Success(
                new PageOf<Invoice>(items, all.Length, page, pageSize)));
        }

        public Task<Result<PageOf<Invoice>>> SearchPageAsync(
            InvoiceSearch search, int page, int pageSize)
            => throw new NotSupportedException();

        public Task<Result<Invoice?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<PageOf<Invoice>>> GetPageAsync(int page, int pageSize) => throw new NotSupportedException();
        public Task<Result<Invoice[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => throw new NotSupportedException();
        public Task<Result<Invoice>> CreateAsync(Invoice entity) => throw new NotSupportedException();
        public Task<Result<Invoice>> UpdateAsync(Invoice entity) => throw new NotSupportedException();
        public Task<Result<Invoice?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
        public Task<Result<Invoice>> FinalizeAsync(int id) => throw new NotSupportedException();
        public Task<Result<byte[]>> PreviewAsync(int id) => throw new NotSupportedException();
        public Task<Result<InvoiceNumberPreview>> PeekInvoiceNumberAsync() => throw new NotSupportedException();
        public Task<Result<bool>> InvoiceNumberExistsAsync(string? number, int excludeInvoiceId = 0)
            => throw new NotSupportedException();
        public Task<Result<decimal?>> LastQuantityAsync(int contactId, int? companyId, short rateTypeId, int excludeInvoiceId = 0)
            => throw new NotSupportedException();
    }

    private sealed class EmptyContactService : IContactService
    {
        public Task<Result<Contact>> CreateAsync(Contact entity) => throw new NotSupportedException();
        public Task<Result<Contact>> UpdateAsync(Contact entity) => throw new NotSupportedException();
        public Task<Result<Contact?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
        public Task<Result<Contact?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<PageOf<Contact>>> GetPageAsync(int page, int pageSize) => throw new NotSupportedException();
        public Task<Result<Contact[]>> GetAllAsync() => throw new NotSupportedException();

        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(
                new Dictionary<int, string>()));
    }

    private sealed class EmptyInvoiceStatusService : IInvoiceStatusService
    {
        public Task<Result<InvoiceStatus?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<PageOf<InvoiceStatus>>> GetPageAsync(int page, int pageSize) => throw new NotSupportedException();
        public Task<Result<InvoiceStatus[]>> GetAllAsync() => throw new NotSupportedException();

        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(
                new Dictionary<int, string>()));
    }

    private sealed class AllowAllAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(
            ClaimsPrincipal user,
            object? resource,
            string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    private sealed class SignedInAuthStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState SignedIn = new(
            new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "tester") }, "test")));

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(SignedIn);
    }
}
