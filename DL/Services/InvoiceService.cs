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
using Dapper;
using InvoiceStatusKind = _0nline.Biller.DL.Contract.Enums.InvoiceStatus;

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

        public InvoiceService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<Invoice> queryService,
            ITenantContext tenancyContext,
            IInvoiceLineService invoiceLineService,
            IInvoicePdfService pdfService)
            : base(ctx, queryService, tenancyContext)
        {
            _invoiceLineService = invoiceLineService;
            _pdfService = pdfService;
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

            if (invoice.InvoiceStatusID != (short)InvoiceStatusKind.Draft)
                return Result<Invoice?>.Failure(
                    ErrorCode.Conflict, "Only a draft invoice can be deleted.");

            return await base.DeleteOrDeactivateAsync(id);
        }

        public async Task<Result<Invoice>> FinalizeAsync(int id)
        {
            var loaded = await GetOneAsync(id);
            if (!loaded.Successful || loaded.Value is not Invoice invoice)
                return Result<Invoice>.None();

            if (invoice.InvoiceStatusID != (short)InvoiceStatusKind.Draft)
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

            invoice.InvoiceStatusID = (short)InvoiceStatusKind.Finalized;
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
            invoice.InvoiceDate ??= DateTime.Today;
            invoice.InvoiceStatusID = invoice.InvoiceStatusID == 0
                ? (short)InvoiceStatusKind.Draft
                : invoice.InvoiceStatusID;
            invoice.CompanyID = invoice.CompanyID is > 0 ? invoice.CompanyID : null;

            var autoNumber = string.IsNullOrWhiteSpace(invoice.InvoiceNumber);
            if (autoNumber)
                invoice.InvoiceNumber = "TMP-" + Guid.NewGuid().ToString("N")[..20];
            else
                invoice.InvoiceNumber = invoice.InvoiceNumber!.Trim();

            var validationError = await ValidateAsync(invoice);
            if (validationError is not null)
                return Result<Invoice>.Failure(ErrorCode.ValidationFailed, validationError);

            var lines = CleanLines(invoice);
            var invoiceProperties = GetCachedProperties<Invoice>();
            var lineProperties = GetCachedProperties<InvoiceLine>();

            ClearNavigation(invoice);

            using var conn = Connection;
            conn.Open();
            using var tran = conn.BeginTransaction();

            try
            {
                await InsertAsync(invoice, invoiceProperties, conn, tran);

                if (autoNumber)
                {
                    invoice.InvoiceNumber =
                        $"INV-{invoice.InvoiceDate?.Year ?? DateTime.Today.Year}-{invoice.ID:D5}";
                    await UpdateAsync(invoice, invoiceProperties, conn, tran);
                }

                foreach (var line in lines)
                {
                    line.ID = 0;
                    line.InvoiceID = invoice.ID;
                    await InsertAsync(line, lineProperties, conn, tran);
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

            if (current.InvoiceStatusID != (short)InvoiceStatusKind.Draft)
                return Result<Invoice>.Failure(
                    ErrorCode.Conflict, "This invoice has been finalized and can no longer be edited.");

            invoice.InvoiceDate ??= current.InvoiceDate ?? DateTime.Today;
            invoice.InvoiceStatusID = (short)InvoiceStatusKind.Draft;
            invoice.CompanyID = invoice.CompanyID is > 0 ? invoice.CompanyID : null;
            invoice.InvoiceNumber = string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
                ? current.InvoiceNumber
                : invoice.InvoiceNumber!.Trim();

            var validationError = await ValidateAsync(invoice);
            if (validationError is not null)
                return Result<Invoice>.Failure(ErrorCode.ValidationFailed, validationError);

            var lines = CleanLines(invoice);
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

        private async Task<string?> ValidateAsync(Invoice invoice)
        {
            if (invoice.IssuedToContactID <= 0)
                return "Choose a contact to invoice.";

            if (invoice.InvoiceDate is null)
                return "Choose an invoice date.";

            if (!Enum.IsDefined((InvoiceStatusKind)invoice.InvoiceStatusID))
                return "Choose a valid invoice status.";

            if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber) && invoice.InvoiceNumber.Length > 30)
                return "The invoice number may not exceed 30 characters.";

            if (invoice.PaymentRef?.Length > 250)
                return "The payment reference may not exceed 250 characters.";

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

                if (line.TotalTax is null or < 0)
                    return "Tax may not be negative.";
            }

            using var conn = Connection;
            var tenantId = RequireTenantId();

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
        /// Copies only the columns a line owns. Navigation graphs from the client
        /// are never persisted: a line must not be able to update its rate row.
        /// </summary>
        private static List<InvoiceLine> CleanLines(Invoice invoice)
        {
            var lines = invoice.InvoiceLines ?? new List<InvoiceLine>();

            return lines.Select(line => new InvoiceLine
            {
                ID = line.ID,
                InvoiceID = invoice.ID,
                ContractID = line.ContractID,
                Description = line.Description?.Trim(),
                Qty = line.Qty,
                TenantRateID = line.TenantRateID,
                TotalTax = line.TotalTax,
                BilledRateIncl = line.BilledRateIncl
            }).ToList();
        }

        private static void ClearNavigation(Invoice invoice)
        {
            invoice.Tenant = null;
            invoice.IssuedToContact = null;
            invoice.InvoiceStatus = null;
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
