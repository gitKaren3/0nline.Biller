using _0nline.Biller.DL.Contract.Enums;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Reports;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Location;
using Dapper;
using Npgsql;

namespace _0nline.Biller.DL.Services
{
    /// <summary>
    /// Invoices are saved as an aggregate: the invoice row and its lines are
    /// written in one transaction, so a failed line cannot leave a half-invoice.
    /// A finalized invoice is locked against further edits.
    /// </summary>
    public class InvoiceService : BillerBaseService<Invoice>, IInvoiceService, IDLReadableService<Invoice>, IDLWritableService<Invoice>
    {
        private readonly IInvoiceLineService _invoiceLineService;
        private readonly IInvoicePdfService _pdfService;
        private readonly ITenantSettingService _tenantSettings;

        public InvoiceService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<Invoice> queryService,
            ITenantContext tenancyContext,
            IInvoiceLineService invoiceLineService,
            IInvoicePdfService pdfService,
            ITenantSettingService tenantSettings)
            : base(ctx, queryService, tenancyContext)
        {
            _invoiceLineService = invoiceLineService;
            _pdfService = pdfService;
            _tenantSettings = tenantSettings;
        }

        public new Task<Result<Invoice[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<Invoice>> CreateAsync(Invoice entity) => CreateWithLinesAsync(entity);
        public new Task<Result<Invoice>> UpdateAsync(Invoice entity) => UpdateWithLinesAsync(entity);

        public new async Task<Result<Invoice?>> DeleteOrDeactivateAsync(int id)
        {
            var loaded = await GetOneAsync(id);
            if (!loaded.Successful || loaded.Value is not Invoice invoice)
                return Result<Invoice?>.None();

            if (invoice.theInvoiceStatus != InvoiceStatus.Draft)
                return Result<Invoice?>.Failure(
                    ErrorCode.Conflict, "Only a draft invoice can be deleted.");

            return await base.DeleteOrDeactivateAsync(id);
        }

        public async Task<Result<Invoice>> FinalizeAsync(int id)
        {
            var loaded = await GetOneAsync(id);
            if (!loaded.Successful || loaded.Value is not Invoice invoice)
                return Result<Invoice>.None();

            if (invoice.theInvoiceStatus != InvoiceStatus.Draft)
                return Result<Invoice>.Failure(
                    ErrorCode.Conflict, "Only a draft invoice can be finalized.");

            if (invoice.InvoiceLines.Count == 0)
                return Result<Invoice>.Failure(
                    ErrorCode.ValidationFailed, "Add at least one line before finalizing.");

            if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            {
                invoice.InvoiceNumber =
                    $"INV-{invoice.InvoiceDate?.Year ?? DateTime.Today.Year}-{invoice.ID:D5}";
            }

            invoice.theInvoiceStatus = InvoiceStatus.Finalized;
            ClearNavigation(invoice);

            var updated = await base.UpdateAsync(invoice);
            if (!updated.Successful)
                return Result<Invoice>.Failure(
                    ErrorCode.ServerError, "The invoice could not be finalized.");

            return await ReloadAsync(id);
        }

        public async Task<Result<byte[]>> PreviewAsync(int id)
        {
            var loaded = await GetOneAsync(id);
            if (!loaded.Successful || loaded.Value is not Invoice invoice)
                return Result<byte[]>.None();

            return await _pdfService.RenderAsync(invoice);
        }

        /// <summary>
        /// Creates the invoice and its lines in one transaction. A blank invoice
        /// number gets Biller's default format; a supplied number is kept.
        /// </summary>
        public async Task<Result<Invoice>> CreateWithLinesAsync(Invoice invoice)
        {
            ArgumentNullException.ThrowIfNull(invoice);

            invoice.ID = 0;
            if (invoice.theInvoiceStatus == default)
                invoice.theInvoiceStatus = InvoiceStatus.Draft;
            invoice.CompanyID = invoice.CompanyID is > 0 ? invoice.CompanyID : null;

            var defaults = await ResolveDefaultsAsync(invoice);
            if (defaults.Error is not null)
                return Result<Invoice>.Failure(ErrorCode.ValidationFailed, defaults.Error);

            var autoNumber = string.IsNullOrWhiteSpace(invoice.InvoiceNumber);
            if (!autoNumber)
                invoice.InvoiceNumber = invoice.InvoiceNumber!.Trim();

            var validationError = await ValidateAsync(invoice, defaults.Value);
            if (validationError is not null)
                return Result<Invoice>.Failure(ErrorCode.ValidationFailed, validationError);

            var lines = PrepareLines(invoice, defaults.Value);
            var invoiceProperties = GetCachedProperties<Invoice>();
            var lineProperties = GetCachedProperties<InvoiceLine>();

            ClearNavigation(invoice);

            using var conn = Connection;
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                if (autoNumber)
                    invoice.InvoiceNumber = await AllocateInvoiceNumberAsync(conn, tran);

                await InsertAsync(invoice, invoiceProperties, conn, tran);

                foreach (var line in lines)
                {
                    line.ID = 0;
                    line.InvoiceID = invoice.ID;
                    await InsertAsync(line, lineProperties, conn, tran);
                }

                tran.Commit();
            }
            catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                // The service checks before writing; this is the guarantee under a race.
                tran.Rollback();
                return Result<Invoice>.Failure(
                    ErrorCode.Conflict,
                    $"Invoice number {invoice.InvoiceNumber} is already used.");
            }
            catch
            {
                tran.Rollback();
                throw;
            }

            return await ReloadAsync(invoice.ID);
        }

        /// <summary>
        /// Updates a draft invoice and reconciles its lines: existing ids are
        /// updated, id 0 is inserted, and anything the caller left out is deleted.
        /// </summary>
        public async Task<Result<Invoice>> UpdateWithLinesAsync(Invoice invoice)
        {
            ArgumentNullException.ThrowIfNull(invoice);
            if (invoice.ID <= 0)
                return Result<Invoice>.None();

            var loaded = await GetOneAsync(invoice.ID);
            if (!loaded.Successful || loaded.Value is not Invoice current)
                return Result<Invoice>.None();

            if (current.theInvoiceStatus != InvoiceStatus.Draft)
                return Result<Invoice>.Failure(
                    ErrorCode.Conflict, "This invoice has been finalized and can no longer be edited.");

            invoice.InvoiceDate ??= current.InvoiceDate;
            invoice.theInvoiceStatus = InvoiceStatus.Draft;
            invoice.CompanyID = invoice.CompanyID is > 0 ? invoice.CompanyID : null;
            invoice.InvoiceNumber = string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
                ? current.InvoiceNumber
                : invoice.InvoiceNumber!.Trim();

            var defaults = await ResolveDefaultsAsync(invoice, current);
            if (defaults.Error is not null)
                return Result<Invoice>.Failure(ErrorCode.ValidationFailed, defaults.Error);

            var validationError = await ValidateAsync(invoice, defaults.Value);
            if (validationError is not null)
                return Result<Invoice>.Failure(ErrorCode.ValidationFailed, validationError);

            var lines = PrepareLines(invoice, defaults.Value);
            var existing = current.InvoiceLines.ToDictionary(line => line.ID);

            if (lines.Any(line => line.ID > 0 && !existing.ContainsKey(line.ID)))
                return Result<Invoice>.Failure(
                    ErrorCode.ValidationFailed, "A line does not belong to this invoice.");

            var invoiceProperties = GetCachedProperties<Invoice>();
            var lineProperties = GetCachedProperties<InvoiceLine>();

            ClearNavigation(invoice);

            using var conn = Connection;
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                await UpdateAsync(invoice, invoiceProperties, conn, tran);

                var kept = new HashSet<int>();
                foreach (var line in lines)
                {
                    line.InvoiceID = invoice.ID;

                    if (line.ID == 0)
                        await InsertAsync(line, lineProperties, conn, tran);
                    else
                        await UpdateAsync(line, lineProperties, conn, tran);

                    kept.Add(line.ID);
                }

                var removed = existing.Keys.Where(id => !kept.Contains(id)).ToArray();
                if (removed.Length > 0)
                {
                    await conn.ExecuteAsync(
                        $@"DELETE FROM {Quoted("InvoiceLine")}
                            WHERE {Quoted("InvoiceID")} = @InvoiceId
                              AND {Quoted("ID")} = ANY(@Ids)",
                        new { InvoiceId = invoice.ID, Ids = removed },
                        tran);
                }

                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }

            return await ReloadAsync(invoice.ID);
        }

        /// <summary>
        /// A single invoice is returned with its lines so a caller can render the
        /// full detail without a second round trip. The invoice itself is
        /// tenant-scoped by the base provider; lines are requested through that
        /// scoped invoice, which is the only way a child row is addressed.
        /// </summary>
        protected override async Task<Invoice> SingleAugment(Invoice invoice)
        {
            if (invoice is not null)
            {
                var lines = await _invoiceLineService.GetByParentAsync(invoice);
                invoice.InvoiceLines = lines.Value?.ToList() ?? new List<InvoiceLine>();
            }
            return invoice!;
        }

        private async Task<(InvoiceDefaults Value, string? Error)> ResolveDefaultsAsync(
            Invoice invoice, Invoice? current = null)
        {
            var settings = await _tenantSettings.GetForTenantAsync();
            if (!settings.Successful || settings.Value is not TenantSetting tenantSetting)
            {
                return (default, settings.Errors.FirstOrDefault()
                    ?? "Complete your billing settings before creating an invoice.");
            }

            var currency = CurrencyCatalog.Default.Get(
                invoice.CurrencyCode ?? current?.CurrencyCode ?? tenantSetting.CurrencyCode);

            invoice.CurrencyCode = currency.Code;
            invoice.CurrencyMinorUnits = (short)currency.MinorUnits;

            if (invoice.InvoiceDate is null)
                invoice.InvoiceDate = TodayIn(tenantSetting.TimeZoneId);

            using var conn = Connection;
            var tenantId = RequireTenantId();

            // The issuer is the tenant's own company. A null scheme means it has not
            // registered for tax, which is a business fact, not an error.
            short? issuerScheme = await conn.ExecuteScalarAsync<short?>(
                @"SELECT c.""TaxSchemeID""
                    FROM ""Tenant"" t
                    LEFT JOIN ""Company"" c ON c.""ID"" = t.""CompanyID""
                   WHERE t.""ID"" = @TenantId",
                new { TenantId = tenantId });

            return (new InvoiceDefaults(
                issuerScheme is null ? null : (TaxScheme)issuerScheme.Value,
                tenantSetting.DefaultTaxRate ?? 0m), null);
        }

        private static DateTime TodayIn(string? timeZoneId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(timeZoneId)
                    && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out var zone))
                {
                    return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
                }
            }
            catch
            {
                // An unusable zone falls back to UTC rather than blocking the invoice.
            }

            return DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Draws the next number from the tenant's counter and increments it in the same
        /// statement, so two concurrent creates cannot receive the same number. Runs in
        /// the caller's transaction: a failed create rolls the number back too.
        /// </summary>
        private async Task<string> AllocateInvoiceNumberAsync(
            System.Data.IDbConnection conn, System.Data.IDbTransaction tran)
        {
            var tenantId = RequireTenantId();

            var number = await conn.ExecuteScalarAsync<string?>(
                @"UPDATE ""TenantSetting""
                     SET ""NextInvoiceNumber"" = COALESCE(""NextInvoiceNumber"", 1) + 1
                   WHERE ""TenantID"" = @TenantId
               RETURNING COALESCE(""InvoiceNumberPrefix"", '')
                         || (COALESCE(""NextInvoiceNumber"", 2) - 1)::text",
                new { TenantId = tenantId },
                tran);

            if (string.IsNullOrWhiteSpace(number))
                throw new InvalidOperationException(
                    "Billing settings are required to number an invoice.");

            return number;
        }

        private async Task<string?> ValidateAsync(Invoice invoice, InvoiceDefaults defaults)
        {
            if (invoice.IssuedToContactID <= 0)
                return "Choose a contact to invoice.";

            if (invoice.InvoiceDate is null)
                return "Choose an invoice date.";

            if (!Enum.IsDefined(invoice.theInvoiceStatus))
                return "Choose a valid invoice status.";

            if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber) && invoice.InvoiceNumber.Length > 30)
                return "The invoice number may not exceed 30 characters.";

            if (invoice.PaymentRef?.Length > 250)
                return "The payment reference may not exceed 250 characters.";

            if (invoice.CurrencyCode is null || invoice.CurrencyCode.Length != 3)
                return "Choose a currency for the invoice.";

            var lines = invoice.InvoiceLines ?? new List<InvoiceLine>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.Description))
                    return "Every line needs a description.";

                if (line.Description.Length > 200)
                    return "A line description may not exceed 200 characters.";

                if (line.Qty is null or <= 0)
                    return "Every line needs a quantity greater than zero.";

                if (line.TenantRateID <= 0)
                    return "Every line needs a rate.";

                if (line.BilledRateIncl is null or < 0)
                    return "A billed rate may not be negative.";

                if (line.TotalTax is < 0)
                    return "Tax may not be negative.";

                if (line.theTaxScheme is { } scheme && !Enum.IsDefined(scheme))
                    return "A line names an unknown tax scheme.";

                if (line.TaxRate is < 0 or > 100)
                    return "A tax rate must be between 0 and 100.";

                if (line.TaxNote?.Length > 250)
                    return "A tax note may not exceed 250 characters.";
            }

            // A business that is not registered may not charge tax. Reject the write
            // rather than silently zeroing a rate the caller thought it had set.
            if (defaults.IssuerTaxSchemeID is null
                && lines.Any(line =>
                    line.theTaxScheme is not null
                    || (line.TaxRate ?? 0m) != 0m
                    || (line.TotalTax ?? 0m) != 0m))
            {
                return "The business is not registered for tax, so no tax can be charged.";
            }

            using var conn = Connection;
            var tenantId = RequireTenantId();

            // A number supplied by the caller must be unique within the tenant. A blank
            // number is auto-assigned and cannot clash, so only check a supplied one.
            if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            {
                var number = invoice.InvoiceNumber.Trim();
                var duplicate = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT 1 FROM ""Invoice""
                      WHERE ""TenantID"" = @TenantId
                        AND ""InvoiceNumber"" = @Number
                        AND ""ID"" <> @Id",
                    new { TenantId = tenantId, Number = number, Id = invoice.ID });

                if (duplicate is not null)
                    return $"Invoice number {number} is already used.";
            }

            var contactExists = await conn.ExecuteScalarAsync<int?>(
                @"SELECT 1 FROM ""Contact"" WHERE ""ID"" = @Id AND ""TenantID"" = @TenantId",
                new { Id = invoice.IssuedToContactID, TenantId = tenantId });

            if (contactExists is null)
                return "The selected contact does not belong to this workspace.";

            if (invoice.CompanyID is int companyId)
            {
                var companyExists = await conn.ExecuteScalarAsync<int?>(
                    @"SELECT 1 FROM ""Company"" WHERE ""ID"" = @Id AND ""TenantID"" = @TenantId",
                    new { Id = companyId, TenantId = tenantId });

                if (companyExists is null)
                    return "The selected company does not belong to this workspace.";
            }

            var rateIds = lines.Select(line => line.TenantRateID).Distinct().ToArray();
            if (rateIds.Length > 0)
            {
                var validRates = (await conn.QueryAsync<int>(
                    @"SELECT ""ID"" FROM ""TenantRate""
                      WHERE ""TenantID"" = @TenantId AND ""ID"" = ANY(@Ids)",
                    new { TenantId = tenantId, Ids = rateIds })).ToHashSet();

                if (rateIds.Any(rateId => !validRates.Contains(rateId)))
                    return "A selected rate does not belong to this workspace.";
            }

            return null;
        }

        /// <summary>
        /// Copies only the columns a line owns and fills in the tax defaults. The
        /// rate actually used is stored on the line, never re-derived at render time.
        /// </summary>
        private static List<InvoiceLine> PrepareLines(Invoice invoice, InvoiceDefaults defaults)
        {
            var lines = invoice.InvoiceLines ?? new List<InvoiceLine>();
            int minorUnits = invoice.CurrencyMinorUnits ?? 2;

            return lines.Select(line =>
            {
                TaxScheme? scheme = defaults.IssuerTaxSchemeID;
                decimal rate = defaults.DefaultTaxRate;

                if (scheme is null)
                {
                    rate = 0m;
                }
                else
                {
                    scheme = line.theTaxScheme ?? scheme;
                    rate = line.TaxRate ?? rate;
                }

                decimal gross = (line.BilledRateIncl ?? 0m) * (line.Qty ?? 0);
                decimal tax = TaxMath.TaxFromInclusive(gross, rate, minorUnits);

                return new InvoiceLine
                {
                    ID = line.ID,
                    InvoiceID = invoice.ID,
                    ContractID = line.ContractID,
                    Description = line.Description?.Trim(),
                    Qty = line.Qty,
                    TenantRateID = line.TenantRateID,
                    BilledRateIncl = line.BilledRateIncl,
                    TaxSchemeID = (short?)scheme,
                    TaxRate = rate,
                    TaxNote = string.IsNullOrWhiteSpace(line.TaxNote) ? null : line.TaxNote.Trim(),
                    TotalTax = tax
                };
            }).ToList();
        }

        private readonly record struct InvoiceDefaults(TaxScheme? IssuerTaxSchemeID, decimal DefaultTaxRate);

        private static void ClearNavigation(Invoice invoice)
        {
            invoice.Tenant = null;
            invoice.IssuedToContact = null;
        }

        private async Task<Result<Invoice>> ReloadAsync(int id)
        {
            var reloaded = await GetOneAsync(id);
            return reloaded.Successful && reloaded.Value is Invoice invoice
                ? Result<Invoice>.Success(invoice)
                : Result<Invoice>.None();
        }
    }
}
