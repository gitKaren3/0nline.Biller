using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Reports;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using Dapper;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace _0nline.Biller.DL.Services
{
    /// <summary>
    /// Issues and serves invoice snapshots. A snapshot is the invoice's render
    /// model as text, not a PDF: the PDF is generated on every read, and the
    /// public route addresses the snapshot by an unguessable token.
    /// </summary>
    public class InvoiceSnapshotService
        : BillerChildBaseService<InvoiceSnapshot, Invoice>, IInvoiceSnapshotService
    {
        /// <summary>
        /// One open often becomes several requests (range fetches, viewer reloads).
        /// AccessCount only moves when the previous access is older than this.
        /// </summary>
        private static readonly TimeSpan AccessWindow = TimeSpan.FromMinutes(30);

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IInvoiceService _invoices;
        private readonly ICompanyService _companies;
        private readonly IContactService _contacts;
        private readonly IBankAccountService _bankAccounts;
        private readonly IRateTypeService _rateTypes;
        private readonly IPDFRenderer _pdf;
        private readonly ILogger<InvoiceSnapshotService> _logger;

        public InvoiceSnapshotService(
            IDbContextProvider<BillerDbConfig> ctx,
            ISqlQueryProvider<InvoiceSnapshot> queryService,
            ITenantContext tenancyContext,
            IInvoiceService invoices,
            ICompanyService companies,
            IContactService contacts,
            IBankAccountService bankAccounts,
            IRateTypeService rateTypes,
            IPDFRenderer pdf,
            ILogger<InvoiceSnapshotService> logger)
            : base(ctx, queryService, tenancyContext)
        {
            _invoices = invoices;
            _companies = companies;
            _contacts = contacts;
            _bankAccounts = bankAccounts;
            _rateTypes = rateTypes;
            _pdf = pdf;
            _logger = logger;
        }

        public async Task<Result<InvoiceSnapshot>> CreateForInvoiceAsync(int invoiceId, bool regenerate = false)
        {
            // The invoice read is tenant-scoped, which is also the permission check:
            // another tenant's id resolves to nothing and never reaches a snapshot.
            var loaded = await _invoices.GetOneAsync(invoiceId);
            if (!loaded.Successful || loaded.Value is not Invoice invoice)
                return Result<InvoiceSnapshot>.None();

            if (!regenerate)
            {
                var existing = await ActiveForInvoiceAsync(invoice);
                if (existing is not null)
                    return Result<InvoiceSnapshot>.Success(existing);
            }

            var model = await BuildModelAsync(invoice);

            var snapshot = new InvoiceSnapshot
            {
                Token = Guid.NewGuid(),
                LayoutVersion = InvoicePdfModel.CurrentLayoutVersion,
                Snapshot = JsonSerializer.Serialize(model, JsonOptions)
            };

            var added = await AddAsync(invoice, snapshot);
            return added.Successful && added.Value is not null
                ? added
                : Result<InvoiceSnapshot>.None();
        }

        public async Task<Result<InvoiceSnapshot?>> GetByTokenAsync(Guid token)
        {
            if (token == Guid.Empty)
                return Result<InvoiceSnapshot?>.None();

            using var conn = Connection;

            // Deliberately unscoped: the token is the capability, and an anonymous
            // caller has no tenant. Never add a by-id variant of this read.
            var row = await conn.QueryFirstOrDefaultAsync<InvoiceSnapshot>(
                @"SELECT * FROM ""InvoiceSnapshot""
                  WHERE ""Token"" = @Token AND ""RevokedAt"" IS NULL",
                new { Token = token });

            return row is null
                ? Result<InvoiceSnapshot?>.None()
                : Result<InvoiceSnapshot?>.Success(row);
        }

        public async Task<Result<byte[]>> RenderPdfAsync(Guid token, CancellationToken cancellationToken = default)
        {
            var found = await GetByTokenAsync(token);
            if (!found.Successful || found.Value is not InvoiceSnapshot snapshot)
                return Result<byte[]>.None();

            if (string.IsNullOrWhiteSpace(snapshot.Snapshot))
                return Result<byte[]>.None();

            InvoicePdfModel? model;
            try
            {
                model = JsonSerializer.Deserialize<InvoicePdfModel>(snapshot.Snapshot, JsonOptions);
            }
            catch (JsonException)
            {
                // A row written under an incompatible layout must not take the
                // process down; the caller sees the same not-found as a bad token.
                return Result<byte[]>.Failure(
                    ErrorCode.Unspecified, "The invoice snapshot could not be read.");
            }

            if (model is null)
                return Result<byte[]>.None();

            var pdf = await _pdf.RenderAsync(model, cancellationToken);
            return Result<byte[]>.Success(pdf);
        }

        public async Task RecordAccessAsync(Guid token, CancellationToken cancellationToken = default)
        {
            if (token == Guid.Empty)
                return;

            try
            {
                using var conn = Connection;

                // All SET expressions see the old row, so the CASE still tests the
                // previous LastAccessedAt while the same statement refreshes it.
                await conn.ExecuteAsync(new CommandDefinition(
                    @"UPDATE ""InvoiceSnapshot""
                         SET ""FirstAccessedAt"" = COALESCE(""FirstAccessedAt"", now()),
                             ""LastAccessedAt""  = now(),
                             ""AccessCount""     = CASE
                                 WHEN ""LastAccessedAt"" IS NULL
                                   OR ""LastAccessedAt"" < now() - @Window
                                 THEN COALESCE(""AccessCount"", 0) + 1
                                 ELSE COALESCE(""AccessCount"", 0)
                             END
                       WHERE ""Token"" = @Token AND ""RevokedAt"" IS NULL",
                    new { Token = token, Window = AccessWindow },
                    cancellationToken: cancellationToken));
            }
            catch (Exception ex)
            {
                // Delivery telemetry must never deny the document.
                _logger.LogWarning(ex,
                    "Could not record access for invoice snapshot {Token}.", token);
            }
        }

        public async Task<Result<InvoiceSnapshotActivity>> GetActivityAsync(int invoiceId)
        {
            var loaded = await _invoices.GetOneAsync(invoiceId);
            if (!loaded.Successful || loaded.Value is not Invoice invoice)
                return Result<InvoiceSnapshotActivity>.None();

            var snapshots = (await GetByParentAsync(invoice)).Value ?? Array.Empty<InvoiceSnapshot>();

            // The live link is the one the tenant is asking about. Fall back to the
            // newest revoked one so the UI can still explain a dead link.
            var snapshot = snapshots
                    .Where(candidate => candidate.RevokedAt is null)
                    .OrderByDescending(candidate => candidate.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(candidate => candidate.ID)
                    .FirstOrDefault()
                ?? snapshots
                    .OrderByDescending(candidate => candidate.CreatedAt ?? DateTime.MinValue)
                    .ThenByDescending(candidate => candidate.ID)
                    .FirstOrDefault();

            if (snapshot is null)
                return Result<InvoiceSnapshotActivity>.None();

            return Result<InvoiceSnapshotActivity>.Success(new InvoiceSnapshotActivity
            {
                Token = snapshot.Token,
                CreatedAt = snapshot.CreatedAt ?? DateTime.MinValue,
                FirstAccessedAt = snapshot.FirstAccessedAt,
                LastAccessedAt = snapshot.LastAccessedAt,
                RevokedAt = snapshot.RevokedAt,
                AccessCount = snapshot.AccessCount ?? 0
            });
        }

        public async Task<Result<InvoiceSnapshot?>> RevokeAsync(int snapshotId)
        {
            if (snapshotId <= 0)
                return Result<InvoiceSnapshot?>.None();

            using var conn = Connection;

            // There is no TenantID on the child row, so ownership is proved through
            // the invoice. A snapshot id from another tenant updates nothing.
            var row = await conn.QueryFirstOrDefaultAsync<InvoiceSnapshot>(
                @"UPDATE ""InvoiceSnapshot"" s
                     SET ""RevokedAt"" = now()
                   WHERE s.""ID"" = @Id
                     AND s.""RevokedAt"" IS NULL
                     AND EXISTS (SELECT 1 FROM ""Invoice"" i
                                  WHERE i.""ID"" = s.""InvoiceID""
                                    AND i.""TenantID"" = @TenantId)
                  RETURNING s.*",
                new { Id = snapshotId, TenantId = RequireTenantId() });

            return row is null
                ? Result<InvoiceSnapshot?>.None()
                : Result<InvoiceSnapshot?>.Success(row);
        }

        private async Task<InvoiceSnapshot?> ActiveForInvoiceAsync(Invoice invoice)
        {
            var existing = await GetByParentAsync(invoice);
            return existing.Value?
                .Where(snapshot => snapshot.RevokedAt is null)
                .OrderByDescending(snapshot => snapshot.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(snapshot => snapshot.ID)
                .FirstOrDefault();
        }

        private async Task<InvoicePdfModel> BuildModelAsync(Invoice invoice)
        {
            var tenant = invoice.Tenant;

            Company? issuer = null;
            if (tenant?.CompanyID is int issuerCompanyId)
                issuer = (await _companies.GetOneAsync(issuerCompanyId)).Value;

            Contact? issuerContact = null;
            if (tenant?.ContactID is int issuerContactId)
                issuerContact = (await _contacts.GetOneAsync(issuerContactId)).Value;

            Company? customer = null;
            if (invoice.CompanyID is int customerCompanyId)
                customer = (await _companies.GetOneAsync(customerCompanyId)).Value;

            var accounts = (await _bankAccounts.GetAllAsync()).Value ?? Array.Empty<BankAccount>();
            var bank = accounts.FirstOrDefault(account => account.IsDefault == true && account.Active != false)
                       ?? accounts.FirstOrDefault(account => account.Active != false);

            var rateTypes = (await _rateTypes.GetAllAsync()).Value?
                .Where(rateType => rateType.ID != 0)
                .GroupBy(rateType => rateType.ID)
                .ToDictionary(group => group.Key, group => group.First().Name ?? string.Empty)
                ?? new Dictionary<short, string>();

            return InvoicePdfModelFactory.Create(
                invoice,
                issuer,
                issuerContact,
                customer,
                invoice.IssuedToContact,
                bank,
                rateTypes);
        }
    }
}
