using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Numbering;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Auth.Contract;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Location;
using _0nline.Shared.Location.Interfaces;
using _0nline.Shared.Location.Models;
using Dapper;
using Microsoft.Extensions.Options;

namespace _0nline.Biller.DL.Services
{
    /// <summary>
    /// One settings row per tenant. The write is an upsert on the unique TenantID
    /// so two first-login requests cannot create two rows, and so Save is the same
    /// statement whether the row exists or not.
    /// </summary>
    public class TenantSettingService
        : BillerBaseService<TenantSetting>, ITenantSettingService
    {
        private readonly IAuthenticationService _authService;
        private readonly ICountryCatalog _countries;
        private readonly ICurrencyCatalog _currencies;
        private readonly TimeZoneCountryMap _timeZones;
        private readonly LocationOptions _location;

        public TenantSettingService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<TenantSetting> queryService,
            ITenantContext tenancyContext,
            IAuthenticationService authService,
            ICountryCatalog countries,
            ICurrencyCatalog currencies,
            TimeZoneCountryMap timeZones,
            IOptions<LocationOptions> location)
            : base(ctx, queryService, tenancyContext)
        {
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

            var existing = await ReadAsync(tenantId);
            if (existing is not null)
                return Result<TenantSetting>.Success(existing);

            var ensured = await EnsureForTenantAsync(tenantId, _tenantContext.UserId);
            return ensured;
        }

        /// <summary>
        /// Creates the row without relying on the tenantId claim. First-login ensure
        /// calls this immediately after creating the tenant, before a token carrying
        /// the new id has been issued. The id is explicit and the caller is the
        /// service that just created that tenant.
        /// </summary>
        public async Task<Result<TenantSetting>> EnsureForTenantAsync(int tenantId, long? userId)
        {
            if (tenantId <= 0)
                return Result<TenantSetting>.Failure(
                    ErrorCode.Invalid_Argument, "A tenant is required.");

            var existing = await ReadAsync(tenantId);
            if (existing is not null)
                return Result<TenantSetting>.Success(existing);

            var seed = await SeedAsync(userId);

            using var conn = Connection;
            var created = await conn.QuerySingleOrDefaultAsync<TenantSetting>(
                $@"INSERT INTO {Quoted("TenantSetting")}
                       ({Quoted("TenantID")}, {Quoted("CurrencyCode")},
                        {Quoted("CurrencyMinorUnits")}, {Quoted("TimeZoneId")},
                        {Quoted("DefaultTaxRate")})
                   VALUES (@TenantID, @CurrencyCode, @CurrencyMinorUnits,
                           @TimeZoneId, @DefaultTaxRate)
                   ON CONFLICT ({Quoted("TenantID")}) DO NOTHING
                   RETURNING *",
                new
                {
                    TenantID = tenantId,
                    seed.CurrencyCode,
                    seed.CurrencyMinorUnits,
                    seed.TimeZoneId,
                    seed.DefaultTaxRate
                });

            // DO NOTHING returns no row when another request won the race. Read the
            // row that won instead of returning a failure.
            if (created is null)
                created = await ReadAsync(tenantId);

            return created is null
                ? Result<TenantSetting>.Failure(
                    ErrorCode.ServerError, "The tenant settings could not be created.")
                : Result<TenantSetting>.Success(created);
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

            using var conn = Connection;
            var saved = await conn.QuerySingleAsync<TenantSetting>(
                $@"INSERT INTO {Quoted("TenantSetting")}
                       ({Quoted("TenantID")}, {Quoted("CurrencyCode")},
                        {Quoted("CurrencyMinorUnits")}, {Quoted("TimeZoneId")},
                        {Quoted("DefaultTaxRate")}, {Quoted("InvoiceNumberFormat")},
                        {Quoted("NextInvoiceNumber")})
                   VALUES (@TenantID, @CurrencyCode, @CurrencyMinorUnits,
                           @TimeZoneId, @DefaultTaxRate, @InvoiceNumberFormat,
                           @NextInvoiceNumber)
                   ON CONFLICT ({Quoted("TenantID")}) DO UPDATE SET
                       {Quoted("CurrencyCode")} = EXCLUDED.{Quoted("CurrencyCode")},
                       {Quoted("CurrencyMinorUnits")} = EXCLUDED.{Quoted("CurrencyMinorUnits")},
                       {Quoted("TimeZoneId")} = EXCLUDED.{Quoted("TimeZoneId")},
                       {Quoted("DefaultTaxRate")} = EXCLUDED.{Quoted("DefaultTaxRate")},
                       {Quoted("InvoiceNumberFormat")} = EXCLUDED.{Quoted("InvoiceNumberFormat")},
                       {Quoted("NextInvoiceNumber")} = EXCLUDED.{Quoted("NextInvoiceNumber")}
                   RETURNING *",
                new
                {
                    TenantID = tenantId,
                    CurrencyCode = currency.Code,
                    CurrencyMinorUnits = (short)currency.MinorUnits,
                    TimeZoneId = setting.TimeZoneId!.Trim(),
                    DefaultTaxRate = setting.DefaultTaxRate ?? 0m,
                    InvoiceNumberFormat = numberFormat,
                    NextInvoiceNumber = setting.NextInvoiceNumber is int next && next > 0 ? next : 1
                });

            return Result<TenantSetting>.Success(saved);
        }

        private async Task<TenantSetting?> ReadAsync(int tenantId)
        {
            using var conn = Connection;
            return await conn.QueryFirstOrDefaultAsync<TenantSetting>(
                $"SELECT * FROM {Quoted("TenantSetting")} WHERE {Quoted("TenantID")} = @TenantId",
                new { TenantId = tenantId });
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
    }
}
