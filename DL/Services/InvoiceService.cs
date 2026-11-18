using _0nline.Biller.DL.Contract.Enums;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Contract.Numbering;
using _0nline.Biller.DL.Numbering;
using _0nline.Biller.DL.Queries;
using _0nline.Biller.DL.Reports;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Exceptions;
using _0nline.Shared.Db.Contract.Interfaces;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Location;

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
        private readonly IContactService _contacts;
        private readonly ICompanyService _companies;
        private readonly ITenantRateService _tenantRates;
        private readonly IRateTypeService _rateTypes;
        private readonly IInvoiceQueries _queries;
        private readonly IInvoiceNumberAllocator _numbers;

        public InvoiceService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<Invoice> queryService,
            ITenantContext tenancyContext,
            IInvoiceLineService invoiceLineService,
            IInvoicePdfService pdfService,
            ITenantSettingService tenantSettings,
            IContactService contacts,
            ICompanyService companies,
            ITenantRateService tenantRates,
            IRateTypeService rateTypes,
            IInvoiceQueries queries,
            IInvoiceNumberAllocator numbers)
            : base(ctx, queryService, tenancyContext)
        {
            _invoiceLineService = invoiceLineService;
            _pdfService = pdfService;
            _tenantSettings = tenantSettings;
            _contacts = contacts;
            _companies = companies;
            _tenantRates = tenantRates;
            _rateTypes = rateTypes;
            _queries = queries;
            _numbers = numbers;
        }

        public new Task<Result<Invoice[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public Task<Result<PageOf<Invoice>>> GetRecentPageAsync(int page, int pageSize)
            => PageAsync(
                new PageRequest(page, pageSize),
                DbSort<Invoice>.Desc(invoice => invoice.ID));

        /// <summary>
        /// One page of invoices matching <paramref name="search"/>, newest first.
        /// </summary>
        /// <remarks>
        /// The text term is ORed across the invoice number, the contact's name and the
        /// company's name. The date range and statuses are ANDed with it, so a caller
        /// can combine a search term with either filter.
        /// </remarks>
        public async Task<Result<PageOf<Invoice>>> SearchPageAsync(
            InvoiceSearch search, int page, int pageSize)
        {
            ArgumentNullException.ThrowIfNull(search);

            if (search.From is { } from && search.To is { } to && to.Date < from.Date)
            {
                return Result<PageOf<Invoice>>.Failure(
                    ErrorCode.ValidationFailed,
                    "The end date cannot be before the start date.");
            }

            var (predicate, parameters) = _queries.SearchPredicate(search);

            return await PageAsync(
                new PageRequest(page, pageSize),
                predicate,
                parameters,
                DbSort<Invoice>.Desc(invoice => invoice.ID));
        }

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

            // Create always numbers an invoice, so a blank one is a legacy row or a
            // deliberate clear. Do not invent a number here: that would bypass the
            // tenant's format and the counter.
            if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
                return Result<Invoice>.Failure(
                    ErrorCode.ValidationFailed,
                    "This invoice has no number. Give it one before finalizing.");

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
        /// The number the next invoice will draw, formatted, without consuming it. The
        /// counter can lag behind what has been issued (a reset, or numbers typed by
        /// hand), so the peek skips numbers already on an invoice instead of handing the
        /// editor a clash it could have avoided.
        /// </summary>
        public async Task<Result<InvoiceNumberPreview>> PeekInvoiceNumberAsync()
        {
            var settings = await _tenantSettings.GetForTenantAsync();
            if (!settings.Successful || settings.Value is not TenantSetting tenantSetting)
            {
                return Result<InvoiceNumberPreview>.Failure(
                    ErrorCode.ValidationFailed,
                    settings.Errors.FirstOrDefault()
                        ?? "Complete your billing settings before numbering an invoice.");
            }

            if (!InvoiceNumberFormat.TryParse(
                    tenantSetting.InvoiceNumberFormat, out var parsed, out var formatError))
            {
                return Result<InvoiceNumberPreview>.Failure(
                    ErrorCode.ValidationFailed,
                    formatError ?? "The invoice number format is not usable.");
            }

            var format = parsed!;
            var counter = tenantSetting.NextInvoiceNumber is int next && next > 0 ? next : 1;
            var invoiceDate = TenantClock.TodayIn(tenantSetting.TimeZoneId);

            const int batchSize = 100;
            for (var batch = 0; batch < 10; batch++)
            {
                var first = counter + batch * batchSize;
                var candidates = Enumerable.Range(first, batchSize)
                    .Select(number => format.Render(number, invoiceDate))
                    .ToArray();

                var issued = (await _queries.IssuedNumbersAsync(candidates))
                    .ToHashSet(StringComparer.Ordinal);

                for (var offset = 0; offset < candidates.Length; offset++)
                {
                    if (issued.Contains(candidates[offset]))
                        continue;

                    return Result<InvoiceNumberPreview>.Success(new InvoiceNumberPreview
                    {
                        Number = candidates[offset],
                        Counter = first + offset,
                        Format = format.Text
                    });
                }
            }

            return Result<InvoiceNumberPreview>.Failure(
                ErrorCode.Conflict, "A free invoice number could not be found.");
        }

        /// <summary>
        /// Whether a number is already used in this tenant. Advisory: the save re-checks,
        /// and the unique index is the guarantee. An invoice's own number is not a clash.
        /// </summary>
        public async Task<Result<bool>> InvoiceNumberExistsAsync(
            string? number, int excludeInvoiceId = 0)
        {
            var trimmed = number?.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return Result<bool>.Success(false);

            var found = await AnyAsync(
                $"{Quoted("InvoiceNumber")} = @Number AND {Quoted("ID")} <> @Id",
                new { Number = trimmed, Id = excludeInvoiceId });

            return Result<bool>.Success(found);
        }

        /// <summary>
        /// The quantity last used for this contact, company and rate type. The editor
        /// uses it when a line's rate type is chosen; a draft or cancelled invoice is
        /// not a precedent. Null means there is no history and the caller should fall
        /// back to the rate type's MaxQty or 1.
        /// </summary>
        public async Task<Result<decimal?>> LastQuantityAsync(
            int contactId, int? companyId, short rateTypeId, int excludeInvoiceId = 0)
        {
            if (contactId <= 0 || rateTypeId <= 0)
                return Result<decimal?>.Success(null);

            var qty = await _queries.LastQuantityAsync(
                contactId, companyId, rateTypeId, excludeInvoiceId);

            return Result<decimal?>.Success(qty);
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

            try
            {
                await InTransactionAsync(async (conn, tran) =>
                {
                    if (autoNumber)
                        invoice.InvoiceNumber = await AllocateInvoiceNumberAsync(
                            conn, tran, invoice.InvoiceDate!.Value);

                    await InsertAsync(invoice, invoiceProperties, conn, tran);

                    if (!autoNumber)
                        await AdvanceInvoiceCounterAsync(conn, tran, invoice.InvoiceNumber);

                    foreach (var line in lines)
                    {
                        line.ID = 0;
                        line.InvoiceID = invoice.ID;
                        await InsertAsync(line, lineProperties, conn, tran);
                    }

                    return invoice.ID;
                });
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // The service checks before writing; this is the guarantee under a race.
                return Result<Invoice>.Failure(
                    ErrorCode.Conflict,
                    $"Invoice number {invoice.InvoiceNumber} is already in use.");
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

            try
            {
                await InTransactionAsync(async (conn, tran) =>
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
                    await _queries.DeleteLinesAsync(invoice.ID, removed, conn, tran);

                    await AdvanceInvoiceCounterAsync(conn, tran, invoice.InvoiceNumber);

                    return invoice.ID;
                });
            }
            catch (DbConcurrencyException ex)
            {
                // Someone saved this invoice (or its tenant counter) after it was read.
                // The version moved, so the UPDATE matched nothing and nothing landed.
                return Result<Invoice>.Failure(ErrorCode.Conflict, ex.Message);
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
                invoice.InvoiceDate = TenantClock.TodayIn(tenantSetting.TimeZoneId);

            var issuerScheme = await _queries.IssuerTaxSchemeAsync();

            return (new InvoiceDefaults(
                issuerScheme is null ? null : (TaxScheme)issuerScheme.Value,
                tenantSetting.DefaultTaxRate ?? 0m), null);
        }

        /// <summary>
        /// Draws the next number from the tenant's counter and formats it. Runs in the
        /// caller's transaction: a failed create rolls the claimed number back too. A
        /// counter that has fallen behind numbers already issued (the tenant may reset
        /// it, or type numbers by hand) advances again instead of clashing.
        /// </summary>
        private async Task<string> AllocateInvoiceNumberAsync(
            System.Data.IDbConnection conn, System.Data.IDbTransaction tran, DateTime invoiceDate)
        {
            for (var attempt = 0; attempt < 1000; attempt++)
            {
                var claimed = await _numbers.ClaimAsync(conn, tran);

                if (claimed is null)
                    throw new InvalidOperationException(
                        "Billing settings are required to number an invoice.");

                if (!InvoiceNumberFormat.TryParse(claimed.Value.Format, out var format, out var error))
                    throw new InvalidOperationException(
                        error ?? "The invoice number format is not usable.");

                var number = format!.Render(claimed.Value.Number, invoiceDate);

                if (!await _queries.NumberTakenAsync(number, conn, tran))
                    return number;
            }

            throw new InvalidOperationException("A free invoice number could not be found.");
        }

        /// <summary>
        /// A number the tenant supplied moves the counter with it, so the next automatic
        /// number continues after it. A number below the counter is kept as typed and
        /// leaves the counter alone: reusing a live number is the unique index's job.
        /// </summary>
        private async Task AdvanceInvoiceCounterAsync(
            System.Data.IDbConnection conn, System.Data.IDbTransaction tran, string? invoiceNumber)
        {
            if (!InvoiceNumberFormat.TryReadNumber(invoiceNumber, out var used) || used >= int.MaxValue)
                return;

            await _numbers.AdvanceAsync(conn, tran, used);
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

            // A number supplied by the caller must be unique within the tenant. A blank
            // number is auto-assigned and cannot clash, so only check a supplied one.
            if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            {
                var number = invoice.InvoiceNumber.Trim();
                var duplicate = await AnyAsync(
                    $"{Quoted("InvoiceNumber")} = @Number AND {Quoted("ID")} <> @Id",
                    new { Number = number, Id = invoice.ID });

                if (duplicate)
                    return $"Invoice number {number} is already used.";
            }

            // Ownership is the owning service's question, and it answers under the same
            // tenant predicate as every other store read.
            if (!await _contacts.ExistsAsync(invoice.IssuedToContactID))
                return "The selected contact does not belong to this workspace.";

            if (invoice.CompanyID is int companyId && !await _companies.ExistsAsync(companyId))
                return "The selected company does not belong to this workspace.";

            // A source rate is optional provenance. When it is present the line must
            // still equal the captured row: rates are immutable, so a mismatch can
            // only mean corrupt data or a client that bypassed the editor.
            var rateTypeIds = lines
                .Where(line => line.RateTypeID is > 0)
                .Select(line => line.RateTypeID!.Value)
                .Distinct()
                .ToArray();

            if (rateTypeIds.Length > 0)
            {
                // Rate types are a fixed lookup table, so the whole set is the cheapest
                // and most honest answer: it is not filtered by Active, and re-saving an
                // old invoice must not fail because a rate type was retired since.
                var known = await _rateTypes.GetAllAsync();
                if (!known.Successful || known.Value is null)
                    return known.Errors.FirstOrDefault() ?? "The rate types could not be read.";

                var knownIds = known.Value.Select(type => type.ID).ToHashSet();
                if (rateTypeIds.Any(id => !knownIds.Contains(id)))
                    return "A line names an unknown rate type.";
            }

            var sourceRateIds = lines
                .Where(line => line.TenantRateID is > 0)
                .Select(line => line.TenantRateID!.Value)
                .Distinct()
                .ToArray();

            if (sourceRateIds.Length > 0)
            {
                var captured = await _tenantRates.GetCapturedAsync(sourceRateIds);
                if (!captured.Successful || captured.Value is null)
                    return captured.Errors.FirstOrDefault() ?? "The captured rates could not be read.";

                var capturedRates = captured.Value.ToDictionary(rate => rate.ID);

                foreach (var line in lines.Where(line => line.TenantRateID is > 0))
                {
                    if (!capturedRates.TryGetValue(line.TenantRateID!.Value, out var rate))
                        return "A selected rate does not belong to this workspace.";

                    if (line.RateTypeID != rate.RateTypeID
                        || line.BilledRateIncl != rate.RateIncl)
                    {
                        return "A line's source rate no longer matches the captured rate.";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Copies only the columns a line owns and fills in the tax defaults. The
        /// rate actually used is stored on the line, never re-derived at render time.
        /// </summary>
        /// <remarks>
        /// The version is copied too. It is not a column the line owns, it is the token
        /// the store matches the UPDATE against, so a clone that drops it saves as
        /// version 0 and is refused as a conflict even when nobody else has touched the
        /// invoice.
        /// </remarks>
        internal static List<InvoiceLine> PrepareLines(Invoice invoice, InvoiceDefaults defaults)
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
                    Version = line.Version,
                    InvoiceID = invoice.ID,
                    ContractID = line.ContractID,
                    Description = line.Description?.Trim(),
                    Qty = line.Qty,
                    TenantRateID = line.TenantRateID,
                    RateTypeID = line.RateTypeID,
                    BilledRateIncl = line.BilledRateIncl,
                    TaxSchemeID = (short?)scheme,
                    TaxRate = rate,
                    TaxNote = string.IsNullOrWhiteSpace(line.TaxNote) ? null : line.TaxNote.Trim(),
                    TotalTax = tax
                };
            }).ToList();
        }

        internal readonly record struct InvoiceDefaults(TaxScheme? IssuerTaxSchemeID, decimal DefaultTaxRate);

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
