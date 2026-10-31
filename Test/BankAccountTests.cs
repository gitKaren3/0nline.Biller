using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.Wasm.Components;
using _0nline.Shared.Contract.Models;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace _0nline.Biller.Test;

public class BankAccountTests : Base.MudTestContext, IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void FormRejectsAnAccountWithNoRequiredFields()
    {
        var model = new BankAccount { IsDefault = false };

        var cut = Render<BankAccountForm>(p => p.Add(x => x.Model, model));

        Assert.NotNull(cut.Instance.EditContext);
        Assert.False(cut.Instance.Validate());

        model.AccountName = "Flamingo Hair Design";
        model.BankName = "Standard Bank";
        model.AccountNumber = "1234567890";

        Assert.True(cut.Instance.Validate());
    }

    [Fact]
    public void SectionListsAccountsAndMasksTheNumber()
    {
        var service = new FakeBankAccountService
        {
            Accounts =
            {
                new BankAccount
                {
                    ID = 1,
                    TenantID = 19,
                    AccountName = "Flamingo Hair Design",
                    BankName = "Standard Bank",
                    AccountNumber = "1234567890",
                    IsDefault = true,
                    Active = true
                },
                new BankAccount
                {
                    ID = 2,
                    TenantID = 19,
                    AccountName = "Flamingo Savings",
                    BankName = "Capitec",
                    AccountNumber = "9988776655",
                    IsDefault = false,
                    Active = true
                }
            }
        };

        Services.AddSingleton<IBankAccountService>(service);

        var cut = Render<BankAccountSection>();

        cut.WaitForAssertion(() => Assert.Contains("Flamingo Hair Design", cut.Markup));

        // Only the last four digits are shown, and the default account is marked.
        Assert.Contains("•••• 7890", cut.Markup);
        Assert.DoesNotContain("1234567890", cut.Markup);
        Assert.Contains("Default", cut.Markup);
        Assert.Contains("Make default", cut.Markup);
    }

    [Fact]
    public void EmptySectionOffersToAddAnAccount()
    {
        Services.AddSingleton<IBankAccountService>(new FakeBankAccountService());

        var cut = Render<BankAccountSection>();

        cut.WaitForAssertion(() => Assert.Contains("No bank details yet", cut.Markup));

        cut.FindAll("button")
           .First(button => button.TextContent.Contains("Add bank account"))
           .Click();

        cut.WaitForAssertion(() => Assert.Contains("Account holder", cut.Markup));
    }

    [Fact]
    public void SectionCanHideItsHeadingForAHostPage()
    {
        Services.AddSingleton<IBankAccountService>(new FakeBankAccountService());

        var cut = Render<BankAccountSection>(p => p.Add(x => x.ShowHeading, false));

        cut.WaitForAssertion(() => Assert.Contains("No bank details yet", cut.Markup));

        Assert.DoesNotContain("Banking details", cut.Markup);
    }

    private sealed class FakeBankAccountService : IBankAccountService
    {
        public List<BankAccount> Accounts { get; } = new();

        public Task<Result<BankAccount[]>> GetAllAsync()
            => Task.FromResult(Result<BankAccount[]>.Success(Accounts.ToArray()));

        public Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync()
            => Task.FromResult(Result<IReadOnlyDictionary<int, string>>.Success(
                new Dictionary<int, string>()));

        public Task<Result<BankAccount?>> GetOneAsync(int id)
            => Task.FromResult(Result<BankAccount?>.Success(
                Accounts.FirstOrDefault(account => account.ID == id)));

        public Task<Result<BankAccount>> CreateAsync(BankAccount entity)
        {
            entity.ID = Accounts.Count == 0 ? 1 : Accounts.Max(account => account.ID) + 1;
            Accounts.Add(entity);
            return Task.FromResult(Result<BankAccount>.Success(entity));
        }

        public Task<Result<BankAccount>> UpdateAsync(BankAccount entity)
        {
            Accounts.RemoveAll(account => account.ID == entity.ID);
            Accounts.Add(entity);
            return Task.FromResult(Result<BankAccount>.Success(entity));
        }

        public Task<Result<BankAccount?>> DeleteOrDeactivateAsync(int id)
        {
            Accounts.RemoveAll(account => account.ID == id);
            return Task.FromResult(Result<BankAccount?>.Success());
        }
    }
}
