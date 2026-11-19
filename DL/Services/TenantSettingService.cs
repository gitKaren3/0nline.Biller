using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Numbering;
using _0nline.Biller.DL.Queries;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Auth.Contract;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Exceptions;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Location;
using _0nline.Shared.Location.Interfaces;
using _0nline.Shared.Location.Models;
using Microsoft.Extensions.Options;

namespace _0nline.Biller.DL.Services
{
    /// <summary>
    /// One settings row per tenant. Writes go through the store so the row carries a
    /// version and an audit stamp; the first-login ensure keeps its own transaction
    /// race handling on top of the store's insert.
    /// </summary>
    public class TenantSettingService
        : BillerBaseService<TenantSetting>, ITenantSettingService
    {
        private readonly MutableTenantContext _mutableContext;
        private readonly IAuthenticationService _authService;
        private readonly ICountryCatalog _countries;
        private readonly ICurrencyCatalog _currencies;
        private readonly TimeZoneCountryMap _timeZones;
        private readonly LocationOptions _location;
        private readonly ITenantSettingQueries _queries;

        public TenantSettingService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<TenantSetting> queryService,
            ITenantContext tenancyContext,
            IAuthenticationService authService,
            ICountryCatalog countries,
            ICurrencyCatalog currencies,
            TimeZoneCountryMap timeZones,
            IOptions<LocationOptions> location,
            ITenantSettingQueries queries)
            : base(ctx, queryService, new MutableTenantContext(tenancyContext))
        {
            _queries = queries;
            _mutableContext = (MutableTenantContext)_tenantContext;
            _authService = authService;
            _countries = countries;
            _currencies = currencies;
            _timeZones = timeZones;
            _location = location.Value;
        }

        public async Task<Result<TenantSetting>> GetForTenantAsync()
        {
            if (_tenantContext.TenantId is not int tenantId || tenantId <= 0)
                return Result<TenantSetting>.Failure(
                    ErrorCode.Unauthorized, "A tenantId claim is required.");

            var existing = await _queries.GetForTenantAsync(tenantId);
            if (existing is not null)
                return Result<TenantSetting>.Success(existing);

            var ensured = await EnsureForTenantAsync(tenantId, _tenantContext.UserId);
            return ensured;
        }

        public async Task<Result<TenantSetting>> EnsureForTenantAsync(int tenantId, long? userId)
        {
            if (tenantId <= 0)
                return Result<TenantSetting>.Failure(
                    ErrorCode.Invalid_Argument, "A tenant is required.");

            var existing = await _queries.GetForTenantAsync(tenantId);
            if (existing is not null)
                return Result<TenantSetting>.Success(existing);

            var seed = await SeedAsync(userId);

            var entity = new TenantSetting
            {
                TenantID = tenantId,
                CurrencyCode = seed.CurrencyCode,
                CurrencyMinorUnits = seed.CurrencyMinorUnits,
                TimeZoneId = seed.TimeZoneId,
                DefaultTaxRate = seed.DefaultTaxRate
            };

            _mutableContext.OverrideTenantId = tenantId;
            _mutableContext.OverrideUserId = userId;
            try
            {
                var created = await base.CreateAsync(entity);
                if (created.Successful && created.Value is TenantSetting createdSetting)
                    return Result<TenantSetting>.Success(createdSetting);
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // Another request won the race; fall through to the read.
            }
            finally
            {
                _mutableContext.OverrideTenantId = null;
                _mutableContext.OverrideUserId = null;
            }

            var raced = await _queries.GetForTenantAsync(tenantId);
            return raced is null
                ? Result<TenantSetting>.Failure(
                    ErrorCode.ServerError, "The tenant settings could not be created.")
                : Result<TenantSetting>.Success(raced);
        }

        public async Task<Result<TenantSetting>> SaveAsync(TenantSetting setting)
        {
            if (setting is null)
                return Result<TenantSetting>.Failure(
                    ErrorCode.Invalid_Argument, "Settings are required.");

            if (_tenantContext.TenantId is not int tenantId || tenantId <= 0)
                return Result<TenantSetting>.Failure(
                    ErrorCode.Unauthorized, "A tenantId claim is required.");

            if (Validate(setting) is { } error)
                return Result<TenantSetting>.Failure(ErrorCode.ValidationFailed, error);

            var currency = _currencies.Get(setting.CurrencyCode);

            // Stored as the parser normalises it, so what Settings shows is what the
            // invoice service renders. Validate already refused an unusable format.
            var numberFormat = InvoiceNumberFormat.TryParse(
                setting.InvoiceNumberFormat, out var parsedFormat, out _)
                ? parsedFormat!.Text
                : InvoiceNumberFormat.DefaultFormat;

            var existing = await _queries.GetForTenantAsync(tenantId);
            if (existing is not null)
            {
                ApplyValues(existing, currency, setting, numberFormat);
                var updated = await base.UpdateAsync(existing);
                if (updated.Successful)
                    return Result<TenantSetting>.Success(existing);
                return Result<TenantSetting>.Failure(updated.ErrorCode, updated.Errors);
            }

            var created = new TenantSetting
            {
                TenantID = tenantId,
                CurrencyCode = currency.Code,
                CurrencyMinorUnits = (short)currency.MinorUnits,
                TimeZoneId = setting.TimeZoneId!.Trim(),
                DefaultTaxRate = setting.DefaultTaxRate ?? 0m,
                InvoiceNumberFormat = numberFormat,
                NextInvoiceNumber = setting.NextInvoiceNumber is int next && next > 0 ? next : 1
            };

            try
            {
                var result = await base.CreateAsync(created);
                if (result.Successful)
                    return Result<TenantSetting>.Success(created);
                return Result<TenantSetting>.Failure(result.ErrorCode, result.Errors);
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // Another request created the row; re-read and update it.
                var raced = await _queries.GetForTenantAsync(tenantId);
                if (raced is null)
                    return Result<TenantSetting>.Failure(
                        ErrorCode.ServerError, "The tenant settings could not be saved.");

                ApplyValues(raced, currency, setting, numberFormat);
                var updated = await base.UpdateAsync(raced);
                if (updated.Successful)
                    return Result<TenantSetting>.Success(raced);
                return Result<TenantSetting>.Failure(updated.ErrorCode, updated.Errors);
            }
        }

        private static void ApplyValues(
            TenantSetting target,
            CurrencyInfo currency,
            TenantSetting setting,
            string numberFormat)
        {
            target.CurrencyCode = currency.Code;
            target.CurrencyMinorUnits = (short)currency.MinorUnits;
            target.TimeZoneId = setting.TimeZoneId!.Trim();
            target.DefaultTaxRate = setting.DefaultTaxRate ?? 0m;
            target.InvoiceNumberFormat = numberFormat;
            target.NextInvoiceNumber = setting.NextInvoiceNumber is int next && next > 0 ? next : 1;
        }

        /// <summary>
        /// The defaults a new workspace starts with. The account's country and zone
        /// are the first choice; the host's configured country is the fallback. The
        /// result is a seed only - the user confirms or changes it in Settings.
        /// </summary>
        private async Task<TenantSetting> SeedAsync(long? userId)
        {
            UserProfile? profile = null;
            if (userId is > 0)
            {
                try
                {
                    var result = await _authService.GetUserProfileAsync(userId.Value.ToString());
                    if (result.Successful) profile = result.Value;
                }
                catch
                {
                    // A seed that cannot read the profile is still a valid seed.
                }
            }

            var country =
                _countries.FindByIso(profile?.PreferredCountryIso)
                ?? _countries.FindByIso(_location.DefaultCountry)
                ?? _countries.FindByIso("ZA");

            var currency = _currencies.Get(country?.CurrencyCode);
            var zone = ResolveZone(profile?.PreferredTimeZoneId, country);
            var rate = country?.StandardVatRate ?? 0m;

            return new TenantSetting
            {
                CurrencyCode = currency.Code,
                CurrencyMinorUnits = (short)currency.MinorUnits,
                TimeZoneId = zone,
                DefaultTaxRate = decimal.Round(rate, 2, MidpointRounding.AwayFromZero)
            };
        }

        private string ResolveZone(string? preferred, CountryInfo? country)
        {
            if (!string.IsNullOrWhiteSpace(preferred)
                && TimeZoneInfo.TryFindSystemTimeZoneById(preferred.Trim(), out _))
            {
                return preferred.Trim();
            }

            if (country is not null && _timeZones.ZonesFor(country.Iso2).FirstOrDefault() is { } fromCountry)
                return fromCountry;

            return "UTC";
        }

        private string? Validate(TenantSetting setting)
        {
            if (_currencies.Find(setting.CurrencyCode) is null)
                return "Choose a supported currency.";

            if (string.IsNullOrWhiteSpace(setting.TimeZoneId)
                || (!string.Equals(setting.TimeZoneId.Trim(), "UTC", StringComparison.Ordinal)
                    && !_timeZones.AllZones.Contains(setting.TimeZoneId.Trim(), StringComparer.Ordinal)))
            {
                return "Choose a supported time zone.";
            }

            if (setting.DefaultTaxRate is < 0 or > 100)
                return "The default tax rate must be between 0 and 100.";

            if (InvoiceNumberFormat.Validate(setting.InvoiceNumberFormat) is { } formatError)
                return formatError;

            if (setting.NextInvoiceNumber is int next && next < 1)
                return "The next invoice number must be at least 1.";

            return null;
        }

        private class MutableTenantContext : ITenantContext, IRequestCancellation
        {
            private readonly ITenantContext _inner;
            public int? OverrideTenantId { get; set; }
            public long? OverrideUserId { get; set; }

            public MutableTenantContext(ITenantContext inner)
            {
                _inner = inner;
            }

            public int? TenantId => OverrideTenantId ?? _inner.TenantId;
            public long? UserId => OverrideUserId ?? _inner.UserId;
            public string AppId => _inner.AppId;
            public bool IsTenantAssigned => TenantId.HasValue;

            // The wrapper changes who the row belongs to, not how long the request
            // lives, so the caller's cancellation still applies.
            public CancellationToken RequestAborted =>
                (_inner as IRequestCancellation)?.RequestAborted ?? CancellationToken.None;
        }
    }
}