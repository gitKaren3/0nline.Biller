using System.Reflection;
using System.Security.Claims;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm.Components;
using _0nline.Shared.Client.Services;
using _0nline.Shared.Contract.Models;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace _0nline.Biller.Test;

public class TenantFormTests : Base.MudTestContext, IAsyncLifetime
{
    private readonly RecordingTenantService _tenants = new();
    private readonly RecordingContactService _contacts = new();
    private readonly RecordingCompanyService _companies = new();

    public TenantFormTests()
    {
        Services.AddSingleton<ITenantService>(_tenants);
        Services.AddSingleton<IContactService>(_contacts);
        Services.AddSingleton<ICompanyService>(_companies);
        Services.AddSingleton<IImageService>(new NoopImageService());
        Services.AddSingleton<AuthenticationStateProvider>(new SignedInAuthStateProvider());

        var session = new UserSession();
        session.Initialize(7, "biller", 19);
        Services.AddSingleton(session);

        // Cleanup calls MudBlazor makes when the logo picker is disposed.
        JSInterop.SetupVoid("mudElementRef.removeOnBlurEvent", _ => true);
        JSInterop.SetupVoid("mudElementRef.removeOnFocusEvent", _ => true);
        JSInterop.SetupVoid("mudPopover.connect", _ => true);
        JSInterop.SetupVoid("mudPopover.disconnect", _ => true);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task ExistingProfileSavesChildrenThenAttachesThemToTheTenant()
    {
        var contact = new Contact
        {
            ID = 26,
            TenantID = 19,
            UserId = 7,
            FirstName = "Harriet",
            Surname = "Jones",
            Email = "harriet@flamingo.co.za"
        };

        var company = new Company
        {
            ID = 16,
            TenantID = 19,
            Name = "Flamingo Hair Design",
            Address = "1 Main Road",
            AddressCity = "Cape Town"
        };

        var tenant = new Tenant
        {
            ID = 19,
            UserId = 7,
            TierID = 1,
            ContactID = contact.ID,
            CompanyID = company.ID,
            Contact = contact,
            Company = company
        };

        Tenant? saved = null;
        var cut = Render<TenantForm>(p => p
            .Add(x => x.Tenant, tenant)
            .Add(x => x.Onboarding, false)
            .Add(x => x.OnSaved, t => saved = t));

        await InvokeOnValidSubmitAsync(cut);

        Assert.Equal(new[] { 26 }, _contacts.Updated);
        Assert.Empty(_contacts.Created);
        Assert.Equal(new[] { 16 }, _companies.Updated);
        Assert.Empty(_companies.Created);

        Assert.Equal((26, 16), _tenants.Attached);
        Assert.False(_tenants.RegisterCalled);
        Assert.NotNull(saved);
        Assert.Equal(19, saved!.ID);
        Assert.Equal(26, saved.ContactID);
        Assert.Equal(16, saved.CompanyID);
    }

    [Fact]
    public async Task EmptyProfileCreatesBothChildrenAndAttachesThem()
    {
        var tenant = new Tenant
        {
            ID = 19,
            UserId = 7,
            TierID = 1,
            ContactID = null,
            CompanyID = null
        };

        Tenant? saved = null;
        var cut = Render<TenantForm>(p => p
            .Add(x => x.Tenant, tenant)
            .Add(x => x.Onboarding, true)
            .Add(x => x.OnSaved, t => saved = t));

        await InvokeOnValidSubmitAsync(cut);

        Assert.Empty(_contacts.Updated);
        Assert.Single(_contacts.Created);
        Assert.Empty(_companies.Updated);
        Assert.Single(_companies.Created);

        Assert.Equal((101, 201), _tenants.Attached);
        Assert.False(_tenants.RegisterCalled);
        Assert.NotNull(saved);
    }

    private static async Task InvokeOnValidSubmitAsync(IRenderedComponent<TenantForm> cut)
    {
        var method = typeof(TenantForm)
            .GetMethod("OnValidSubmit", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        await (Task)method!.Invoke(cut.Instance, null)!;
    }

    private sealed class SignedInAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, "7"),
                    new Claim(ClaimTypes.Name, "harriet@flamingo.co.za")
                },
                "test");

            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }

    private sealed class RecordingTenantService : ITenantService
    {
        public bool RegisterCalled { get; private set; }
        public (int? Contact, int? Company) Attached { get; private set; }

        public Task<Result<Tenant>> AttachProfileAsync(int? contactId, int? companyId)
        {
            Attached = (contactId, companyId);
            return Task.FromResult(Result<Tenant>.Success(new Tenant
            {
                ID = 19,
                UserId = 7,
                TierID = 1,
                ContactID = contactId,
                CompanyID = companyId
            }));
        }

        public Task<Result<TenantRegistrationResult>> RegisterAsync(Tenant entity)
        {
            RegisterCalled = true;
            return Task.FromResult(Result<TenantRegistrationResult>.None());
        }

        public Task<Result<TenantRegistrationResult>> EnsureAsync(string? currentToken = null)
            => throw new NotSupportedException();

        public Task<Result<Tenant>> CreateOrUpdateAsync(Tenant entity)
            => throw new NotSupportedException();

        public Task<Result<Tenant?>> GetTenantByUserIdAsync(long userId)
            => throw new NotSupportedException();

        public Task<Result> CloseAsync(int tenantId) => throw new NotSupportedException();
        public Task<Result> PurgeAsync(int tenantId) => throw new NotSupportedException();

        public Task<Result<Tenant?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Tenant>> CreateAsync(Tenant entity) => throw new NotSupportedException();
        public Task<Result<Tenant>> UpdateAsync(Tenant entity) => throw new NotSupportedException();
        public Task<Result<Tenant?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
    }

    private sealed class RecordingContactService : IContactService
    {
        public List<int> Updated { get; } = new();
        public List<int> Created { get; } = new();
        private int _nextId = 101;

        public Task<Result<Contact>> UpdateAsync(Contact entity)
        {
            Updated.Add(entity.ID);
            return Task.FromResult(Result<Contact>.Success(entity));
        }

        public Task<Result<Contact>> CreateAsync(Contact entity)
        {
            entity.ID = _nextId++;
            Created.Add(entity.ID);
            return Task.FromResult(Result<Contact>.Success(entity));
        }

        public Task<Result<Contact?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Contact[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => throw new NotSupportedException();
        public Task<Result<Contact?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
    }

    private sealed class RecordingCompanyService : ICompanyService
    {
        public List<int> Updated { get; } = new();
        public List<int> Created { get; } = new();
        private int _nextId = 201;

        public Task<Result<Company>> UpdateAsync(Company entity)
        {
            Updated.Add(entity.ID);
            return Task.FromResult(Result<Company>.Success(entity));
        }

        public Task<Result<Company>> CreateAsync(Company entity)
        {
            entity.ID = _nextId++;
            Created.Add(entity.ID);
            return Task.FromResult(Result<Company>.Success(entity));
        }

        public Task<Result<Company?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Company[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => throw new NotSupportedException();
        public Task<Result<Company?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
    }

    private sealed class NoopImageService : IImageService
    {
        public Task<Result<Image?>> DeleteOrDeactivateAsync(int id) => Task.FromResult(Result<Image?>.Success());
        public Task<Result<Image>> UploadAndRegisterImageAsync(IFormFile file, string category, string caption, bool ispublic)
            => throw new NotSupportedException();
        public Task<Result> RemoveAndDeRegisterImageAsync(int imageId) => throw new NotSupportedException();
        public Task<Result<Image?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Image[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => throw new NotSupportedException();
        public Task<Result<Image>> CreateAsync(Image entity) => throw new NotSupportedException();
        public Task<Result<Image>> UpdateAsync(Image entity) => throw new NotSupportedException();
    }
}
