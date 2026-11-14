using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Models.db.Lookup;
using _0nline.Shared.Db.Contract.Interfaces;
using Xunit;

namespace _0nline.Biller.Test;

/// <summary>
/// Guards the generated tenancy markers. The store scopes by the marker and the
/// foreign key, so a regeneration that drops one of these would return another
/// tenant's rows without any SQL changing shape.
/// </summary>
public class TenancyModelContractTests
{
    [Theory]
    [InlineData(typeof(BankAccount))]
    [InlineData(typeof(Company))]
    [InlineData(typeof(Contact))]
    [InlineData(typeof(_0nline.Biller.DL.Contract.Models.db.File))]
    [InlineData(typeof(Image))]
    [InlineData(typeof(Invoice))]
    [InlineData(typeof(TenantRate))]
    [InlineData(typeof(TenantSetting))]
    public void Tenant_owned_models_declare_the_scope_marker(Type model)
    {
        Assert.True(
            typeof(IDbTenantScoped).IsAssignableFrom(model),
            $"{model.Name} must implement IDbTenantScoped.");
    }

    [Fact]
    public void Tenant_is_the_root()
    {
        Assert.True(typeof(IDbTenant).IsAssignableFrom(typeof(Tenant)));
        Assert.False(typeof(IDbTenantScoped).IsAssignableFrom(typeof(Tenant)));
    }

    [Theory]
    [InlineData(typeof(InvoiceLine))]
    [InlineData(typeof(InvoiceSnapshot))]
    [InlineData(typeof(CompanyContactLink))]
    [InlineData(typeof(Tier))]
    [InlineData(typeof(RateType))]
    [InlineData(typeof(InvoiceStatus))]
    [InlineData(typeof(TaxScheme))]
    public void Child_and_global_models_are_not_tenant_scoped(Type model)
    {
        // Children are isolated through their parent; globals are deliberately shared.
        Assert.False(
            typeof(IDbTenantScoped).IsAssignableFrom(model),
            $"{model.Name} must not declare IDbTenantScoped.");
        Assert.False(
            typeof(IDbTenant).IsAssignableFrom(model),
            $"{model.Name} must not declare IDbTenant.");
    }
}
