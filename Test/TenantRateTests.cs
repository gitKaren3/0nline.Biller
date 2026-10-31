using System.Reflection;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm.Components;
using _0nline.Shared.Contract.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace _0nline.Biller.Test;

public class TenantRateTests : Base.MudTestContext, IAsyncLifetime
{
    public TenantRateTests()
    {
        // Cleanup calls MudBlazor makes when a field is removed from the tree.
        JSInterop.SetupVoid("mudElementRef.removeOnBlurEvent", _ => true);
        JSInterop.SetupVoid("mudElementRef.removeOnFocusEvent", _ => true);
        JSInterop.SetupVoid("mudPointerEventsNone.listenForPointerEvents", _ => true);
        JSInterop.SetupVoid("mudPointerEventsNone.cancelListener", _ => true);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void FormRejectsARateWithNoRateType()
    {
        var model = new TenantRate { DefaultRate = 100m };

        var cut = Render<TenantRateForm>(p => p.Add(x => x.Model, model));

        Assert.NotNull(cut.Instance.EditContext);
        Assert.False(cut.Instance.Validate());
        cut.WaitForAssertion(() => Assert.Contains("Choose a rate type", cut.Markup));

        model.RateTypeID = 1;

        Assert.True(cut.Instance.Validate());
    }

    [Fact]
    public void RateTypeStartsEmptyRatherThanZero()
    {
        var model = new TenantRate { DefaultRate = 100m };

        var cut = Render<TenantRateForm>(p => p.Add(x => x.Model, model));

        var input = cut.Find(".mud-select input");
        Assert.NotEqual("0", input.GetAttribute("value"));
        Assert.Contains("Select a rate type", cut.Markup);
    }

    [Fact]
    public void RateTypeIsARequiredAnnotationSoBlurValidationSeesIt()
    {
        var property = typeof(TenantRate).GetProperty(nameof(TenantRate.RateTypeSelection));

        Assert.NotNull(property);
        Assert.NotNull(property!.GetCustomAttribute<System.ComponentModel.DataAnnotations.RequiredAttribute>());
    }

    [Fact]
    public void FormRejectsAnEndDateBeforeTheStartDate()
    {
        var model = new TenantRate
        {
            RateTypeID = 1,
            DefaultRate = 100m,
            StartDate = new DateTime(2026, 6, 1),
            EndDate = new DateTime(2026, 5, 1)
        };

        var cut = Render<TenantRateForm>(p => p.Add(x => x.Model, model));

        Assert.False(cut.Instance.Validate());
        cut.WaitForAssertion(
            () => Assert.Contains("end date must be on or after the start date", cut.Markup));
    }

    [Fact]
    public void FormRejectsANegativeRate()
    {
        var model = new TenantRate { RateTypeID = 1, DefaultRate = -1m };

        var cut = Render<TenantRateForm>(p => p.Add(x => x.Model, model));

        Assert.False(cut.Instance.Validate());
        cut.WaitForAssertion(() => Assert.Contains("cannot be negative", cut.Markup));
    }

    [Fact]
    public void NewRateStartsTodayWhenTheHostLeavesTheDateUnset()
    {
        var model = new TenantRate { RateTypeID = 1, DefaultRate = 100m };

        Render<TenantRateForm>(p => p.Add(x => x.Model, model));

        Assert.Equal(DateTime.Today, model.StartDate);
    }

    [Fact]
    public void HostSuppliedLookupsReplaceTheRegisteredServices()
    {
        var rateTypes = new FakeRateTypeService();
        Services.AddSingleton<IRateTypeService>(rateTypes);

        var model = new TenantRate { RateTypeID = 1, DefaultRate = 100m };

        var cut = Render<TenantRateForm>(p => p
            .Add(x => x.Model, model)
            .Add(x => x.RateTypes, new Dictionary<int, string> { [1] = "Hourly" })
            .Add(x => x.Companies, new Dictionary<int, string>())
            .Add(x => x.Contacts, new Dictionary<int, string>()));

        Assert.NotNull(cut.Instance.EditContext);
        Assert.False(rateTypes.LookupCalled);
    }

    [Fact]
    public void SectionListsRatesWithTheirLookupNames()
    {
        var rates = new FakeTenantRateService();
        rates.Rates.Add(new TenantRate
        {
            ID = 1,
            TenantID = 19,
            RateTypeID = 1,
            CompanyID = 4,
            DefaultRate = 450m,
            Active = true,
            StartDate = new DateTime(2026, 1, 1)
        });
        rates.Rates.Add(new TenantRate
        {
            ID = 2,
            TenantID = 19,
            RateTypeID = 2,
            DefaultRate = 900m,
            Active = false,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31)
        });

        Services.AddSingleton<ITenantRateService>(rates);
        Services.AddSingleton<IRateTypeService>(new FakeRateTypeService(new Dictionary<int, string>
        {
            [1] = "Hourly",
            [2] = "Daily"
        }));
        Services.AddSingleton<ICompanyService>(
            new FakeCompanyService(new Dictionary<int, string> { [4] = "Flamingo" }));
        Services.AddSingleton<IContactService>(new FakeContactService());

        var cut = Render<TenantRateSection>();

        cut.WaitForAssertion(() => Assert.Contains("Hourly", cut.Markup));

        Assert.Contains("Flamingo", cut.Markup);
        Assert.Contains("Daily", cut.Markup);
        Assert.Contains("Inactive", cut.Markup);
        Assert.Contains("450.00", cut.Markup);
    }

    [Fact]
    public void EmptySectionOffersToAddARate()
    {
        RegisterSectionServices(new FakeTenantRateService());

        var cut = Render<TenantRateSection>();

        cut.WaitForAssertion(() => Assert.Contains("No rates yet", cut.Markup));

        cut.FindAll("button")
           .First(button => button.TextContent.Contains("Add rate"))
           .Click();

        cut.WaitForAssertion(() => Assert.Contains("Billing rate", cut.Markup));
    }

    [Fact]
    public void SavingAValidRateCreatesIt()
    {
        var rates = new FakeTenantRateService();
        RegisterSectionServices(rates);

        var cut = Render<TenantRateSection>();

        cut.WaitForAssertion(() => Assert.Contains("No rates yet", cut.Markup));

        cut.FindAll("button")
           .First(button => button.TextContent.Contains("Add rate"))
           .Click();

        // The form is rendered by now; fill the same instance the section holds.
        var model = (TenantRate)typeof(TenantRateSection)
            .GetField("_model", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(cut.Instance)!;

        model.RateTypeID = 1;
        model.DefaultRate = 250m;

        cut.FindAll("button")
           .First(button => button.TextContent.Contains("Save rate"))
           .Click();

        cut.WaitForAssertion(() => Assert.Single(rates.Created));

        Assert.Equal(1, rates.Created[0].RateTypeID);
        Assert.Equal(250m, rates.Created[0].DefaultRate);
        Assert.Contains("Saved rates", cut.Markup);
    }

    private void RegisterSectionServices(FakeTenantRateService rates)
    {
        Services.AddSingleton<ITenantRateService>(rates);
        Services.AddSingleton<IRateTypeService>(
            new FakeRateTypeService(new Dictionary<int, string> { [1] = "Hourly" }));
        Services.AddSingleton<ICompanyService>(new FakeCompanyService());
        Services.AddSingleton<IContactService>(new FakeContactService());
    }

    private sealed class FakeTenantRateService : ITenantRateService
    {
        public List<TenantRate> Rates { get; } = new();
        public List<TenantRate> Created { get; } = new();
        public List<int> Removed { get; } = new();

        public Task<Result<TenantRate[]>> GetAllAsync()
            => Task.FromResult(Result<TenantRate[]>.Success(Rates.ToArray()));

        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(
                new Dictionary<int, string>()));

        public Task<Result<TenantRate?>> GetOneAsync(int id)
            => Task.FromResult(Result<TenantRate?>.Success(
                Rates.FirstOrDefault(rate => rate.ID == id)));

        public Task<Result<TenantRate>> CreateAsync(TenantRate entity)
        {
            entity.ID = Rates.Count == 0 ? 1 : Rates.Max(rate => rate.ID) + 1;
            Created.Add(entity);
            Rates.Add(entity);
            return Task.FromResult(Result<TenantRate>.Success(entity));
        }

        public Task<Result<TenantRate>> UpdateAsync(TenantRate entity)
        {
            Rates.RemoveAll(rate => rate.ID == entity.ID);
            Rates.Add(entity);
            return Task.FromResult(Result<TenantRate>.Success(entity));
        }

        public Task<Result<TenantRate?>> DeleteOrDeactivateAsync(int id)
        {
            Removed.Add(id);
            Rates.RemoveAll(rate => rate.ID == id);
            return Task.FromResult(Result<TenantRate?>.Success());
        }
    }

    /// <summary>The rate types are global, so this service is read-only.</summary>
    private sealed class FakeRateTypeService : IRateTypeService
    {
        private readonly IReadOnlyDictionary<int, string> _values;

        public FakeRateTypeService(IReadOnlyDictionary<int, string>? values = null)
            => _values = values ?? new Dictionary<int, string>();

        public bool LookupCalled { get; private set; }

        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
        {
            LookupCalled = true;
            return Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(_values));
        }

        public Task<Result<RateType?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<RateType[]>> GetAllAsync() => throw new NotSupportedException();
    }

    private sealed class FakeCompanyService : ICompanyService
    {
        private readonly IReadOnlyDictionary<int, string> _values;

        public FakeCompanyService(IReadOnlyDictionary<int, string>? values = null)
            => _values = values ?? new Dictionary<int, string>();

        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(_values));

        public Task<Result<Company?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Company[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<Company>> CreateAsync(Company entity) => throw new NotSupportedException();
        public Task<Result<Company>> UpdateAsync(Company entity) => throw new NotSupportedException();
        public Task<Result<Company?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
    }

    private sealed class FakeContactService : IContactService
    {
        private readonly IReadOnlyDictionary<int, string> _values;

        public FakeContactService(IReadOnlyDictionary<int, string>? values = null)
            => _values = values ?? new Dictionary<int, string>();

        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(_values));

        public Task<Result<Contact?>> GetOneAsync(int id) => throw new NotSupportedException();
        public Task<Result<Contact[]>> GetAllAsync() => throw new NotSupportedException();
        public Task<Result<Contact>> CreateAsync(Contact entity) => throw new NotSupportedException();
        public Task<Result<Contact>> UpdateAsync(Contact entity) => throw new NotSupportedException();
        public Task<Result<Contact?>> DeleteOrDeactivateAsync(int id) => throw new NotSupportedException();
    }
}
