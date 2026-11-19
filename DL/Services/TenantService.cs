using _0nline.Biller.DL.Contract.Enums;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Queries;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Auth.Contract;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;
using System.Text.Json;

namespace _0nline.Biller.DL.Services
{
    public class TenantService : BillerBaseService<Tenant>, ITenantService, IDLWritableService<Tenant>
    {
        private IAuthenticationService _authService { get; }
        private ITierService _tierService { get; }
        private IFileStoreService _fileStoreService { get; }
        private IImageService _imageService { get; }
        private ITenantSettingService _tenantSettings { get; }
        private ITenantQueries _queries { get; }
        private IContactService _contacts { get; }
        private ICompanyService _companies { get; }

        public TenantService(IDbContextProvider<BillerDbConfig> ctx
                            , ISqlQueryProvider<Tenant> queryService
                            , ITierService tierService
                            , IAuthenticationService authService
                            , ITenantContext tenancyContext
                            , IImageService imageService
                            , IFileStoreService fileStoreService
                            , ITenantSettingService tenantSettings
                            , ITenantQueries queries
                            , IContactService contacts
                            , ICompanyService companies)
            : base(ctx, queryService, tenancyContext)
        {
            _authService = authService;
            _tierService = tierService;
            _fileStoreService = fileStoreService;
            _imageService = imageService;
            _tenantSettings = tenantSettings;
            _queries = queries;
            _contacts = contacts;
            _companies = companies;
        }

        public new Task<Result<Tenant>> CreateAsync(Tenant entity) => CreateTenantAsync(entity);
        public new Task<Result<Tenant>> UpdateAsync(Tenant entity) => UpdateTenantAsync(entity);
        public new Task<Result<Tenant?>> DeleteOrDeactivateAsync(int id) => CloseAndResultAsync(id);

        /// <summary>
        /// Creates a tenant for the signed-in user only. Identity, ownership and
        /// the profile pointers are all server-owned; the body cannot inject them.
        /// </summary>
        private async Task<Result<Tenant>> CreateTenantAsync(Tenant entity)
        {
            entity.ID = 0;
            entity.UserId = RequireUserId();
            entity.ContactID = null;
            entity.CompanyID = null;
            entity.Contact = null;
            entity.Company = null;

            // Never take the tier from the body: a caller could self-upgrade.
            entity.theTier = await GetFreeTierAsync();

            if (entity.UserId is long userId)
            {
                var existing = await GetTenantByUserIdAsync(userId);
                if (existing.Successful)
                    return Result<Tenant>.Failure(
                        ErrorCode.Conflict, "This user already has a workspace.");
            }

            try
            {
                return await base.CreateAsync(entity);
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // Two tabs can pass the pre-check together; the unique index on
                // Tenant.UserId is the real guard. Report the race, not a 500.
                return Result<Tenant>.Failure(
                    ErrorCode.Conflict, "This user already has a workspace.");
            }
        }

        /// <summary>
        /// The only mutable part of a tenant is which of its own contact and
        /// company rows are its profile. Everything else is ignored.
        /// </summary>
        private async Task<Result<Tenant>> UpdateTenantAsync(Tenant entity)
        {
            if (entity.ID <= 0)
                throw new KeyNotFoundException("Tenant not found.");

            var existing = await GetOneAsync(entity.ID);
            if (!existing.Successful || existing.Value is not Tenant current)
                throw new KeyNotFoundException("Tenant not found.");

            if (current.UserId != RequireUserId())
                throw new UnauthorizedAccessException("This tenant does not belong to the signed-in user.");

            // AttachProfileAsync writes the tenant named by the token; the body id
            // must be that same tenant, not merely another row owned by the user.
            if (_tenantContext.TenantId is int contextTenant && contextTenant != entity.ID)
                throw new UnauthorizedAccessException("This tenant is not the caller's active workspace.");

            int? contactId = entity.ContactID is > 0
                ? entity.ContactID
                : (entity.Contact?.ID is > 0 ? entity.Contact.ID : null);

            int? companyId = entity.CompanyID is > 0
                ? entity.CompanyID
                : (entity.Company?.ID is > 0 ? entity.Company.ID : null);

            return await AttachProfileAsync(contactId, companyId);
        }

        private async Task<Result<Tenant?>> CloseAndResultAsync(int id)
        {
            var closed = await CloseAsync(id);
            return closed.Successful
                ? Result<Tenant?>.Success()
                : Result<Tenant?>.Failure(closed.ErrorCode, closed.Errors);
        }

        public async Task<Result<Tenant>> CreateOrUpdateAsync(Tenant entity)
        {
            if (_tenantContext.TenantId is int tenantId && tenantId > 0)
            {
                if (entity.ID > 0)
                    return await UpdateAsync(entity);

                var current = await GetOneAsync(tenantId);
                return current.Successful && current.Value is Tenant existing
                    ? Result<Tenant>.Success(existing)
                    : Result<Tenant>.Failure(current.ErrorCode, current.Errors);
            }

            var ensured = await EnsureAsync();
            if (!ensured.Successful || ensured.Value is not TenantRegistrationResult registration)
                return Result<Tenant>.Failure(ensured.ErrorCode, ensured.Errors);

            return Result<Tenant>.Success(registration.Tenant);
        }

        public async Task<Result<Tenant?>> GetTenantByUserIdAsync(long userId)
        {
            var entity = await GetFirstAsync(new Dictionary<string, object?>
            {
                ["UserId"] = userId
            });

            return entity is null
                ? Result<Tenant?>.None()
                : Result<Tenant?>.Success(entity);
        }

        /// <summary>
        /// Kept for compatibility. Registration is now the ensure step: the body
        /// is ignored so a caller cannot name the user or the tenant.
        /// </summary>
        public Task<Result<TenantRegistrationResult>> RegisterAsync(Tenant entity) => EnsureAsync();

        public async Task<Result<TenantRegistrationResult>> EnsureAsync(string? currentToken = null)
        {
            var userId = RequireUserId();
            var appId = _tenantContext.AppId;

            if (string.IsNullOrWhiteSpace(appId))
                throw new InvalidOperationException("AppId is missing from the token.");

            var existing = await GetTenantByUserIdAsync(userId);
            Tenant tenant;

            if (existing.Successful && existing.Value is Tenant found)
            {
                if (found.Active == false)
                    return Result<TenantRegistrationResult>.Failure(
                        ErrorCode.Unauthorized, "This workspace is closed.");

                tenant = found;
            }
            else if (existing.IsNone)
            {
                var created = await CreateAsync(new Tenant { UserId = userId, TierID = 0 });

                if (created.Successful && created.Value is Tenant createdTenant)
                {
                    tenant = createdTenant;
                }
                else if (created.ErrorCode == ErrorCode.Conflict)
                {
                    // Another tab/request won the race; use the row it created.
                    var raced = await GetTenantByUserIdAsync(userId);
                    if (!raced.Successful || raced.Value is not Tenant racedTenant)
                        return Result<TenantRegistrationResult>.Failure(raced.ErrorCode, raced.Errors);

                    tenant = racedTenant;
                }
                else
                {
                    return Result<TenantRegistrationResult>.Failure(created.ErrorCode, created.Errors);
                }
            }
            else
            {
                return Result<TenantRegistrationResult>.Failure(existing.ErrorCode, existing.Errors);
            }

            // Settings must exist before the profile form can save an invoice default.
            // Best-effort: a failure here must not strand first login, and the read
            // path creates the row again when the user opens Settings.
            try
            {
                await _tenantSettings.EnsureForTenantAsync(tenant.ID, userId);
            }
            catch
            {
                // The settings page repairs a missing row with EnsureForTenantAsync.
            }

            // The subscription is what puts the tenantId claim into the next token.
            // If it already exists, reissue the caller's token instead.
            var subscription = await _authService.AddUserSubscriptionAsync(
                userId.ToString(), appId, tenant.ID, tenant.TierID);

            if (subscription.Successful
                && subscription.Value is AuthResult createdAuth
                && !string.IsNullOrEmpty(createdAuth.Token))
            {
                return Result<TenantRegistrationResult>.Success(new TenantRegistrationResult
                {
                    Tenant = tenant,
                    Token = createdAuth.Token
                });
            }

            if (subscription.IsFailed)
                return Result<TenantRegistrationResult>.Failure(subscription.ErrorCode, subscription.Errors);

            if (!string.IsNullOrWhiteSpace(currentToken))
            {
                var renewed = await _authService.RenewTokenAsync(currentToken);
                if (renewed.Successful
                    && renewed.Value is AuthResult renewedAuth
                    && !string.IsNullOrEmpty(renewedAuth.Token))
                {
                    int? tokenTenant = ReadTenantId(renewedAuth.Token);

                    if (tokenTenant is null || tokenTenant == tenant.ID)
                    {
                        return Result<TenantRegistrationResult>.Success(new TenantRegistrationResult
                        {
                            Tenant = tenant,
                            Token = renewedAuth.Token
                        });
                    }

                    // The subscription survived a close/purge and still points at
                    // the old tenant. Replace it, or the next token keeps sending
                    // this user to a workspace that no longer exists.
                    var reset = await _authService.RemoveUserSubscriptionAsync(
                        userId.ToString(), appId);

                    if (!reset.Successful)
                        return Result<TenantRegistrationResult>.Failure(reset.ErrorCode, reset.Errors);

                    var readded = await _authService.AddUserSubscriptionAsync(
                        userId.ToString(), appId, tenant.ID, tenant.TierID);

                    if (readded.Successful
                        && readded.Value is AuthResult readdedAuth
                        && !string.IsNullOrEmpty(readdedAuth.Token))
                    {
                        return Result<TenantRegistrationResult>.Success(new TenantRegistrationResult
                        {
                            Tenant = tenant,
                            Token = readdedAuth.Token
                        });
                    }

                    return Result<TenantRegistrationResult>.Failure(
                        ErrorCode.ServerError, "The workspace subscription could not be repaired.");
                }
            }

            return Result<TenantRegistrationResult>.Success(new TenantRegistrationResult
            {
                Tenant = tenant,
                Token = null
            });
        }

        /// <summary>
        /// Reads an unvalidated tenantId claim. Only used on a token we just minted
        /// on this request, to detect a subscription pointing at a stale tenant.
        /// </summary>
        private static int? ReadTenantId(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            string[] parts = token.Split('.');
            if (parts.Length < 2) return null;

            try
            {
                string payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

                using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
                if (document.RootElement.TryGetProperty("tenantId", out var claim)
                    && int.TryParse(claim.ToString(), out int tenantId))
                {
                    return tenantId;
                }
            }
            catch
            {
                // A malformed payload is not a tenant claim.
            }

            return null;
        }

        public async Task<Result<Tenant>> AttachProfileAsync(int? contactId, int? companyId)
        {
            var userId = RequireUserId();
            int tenantId = _tenantContext.TenantId
                ?? throw new UnauthorizedAccessException("A tenantId claim is required.");

            var current = await GetFirstAsync(new Dictionary<string, object?>
            {
                ["ID"] = tenantId,
                ["UserId"] = userId
            });

            if (current is null)
                throw new KeyNotFoundException("Tenant not found.");

            if (current.Active == false)
                throw new UnauthorizedAccessException("This workspace is closed.");

            // 0 is what an empty form binds to; treat it as "not set", not as a
            // request to attach row zero.
            if (contactId is <= 0) contactId = null;
            if (companyId is <= 0) companyId = null;

            int? newContact = contactId ?? current.ContactID;
            int? newCompany = companyId ?? current.CompanyID;

            // Ownership is the owning service's answer, under the claim, which is this
            // tenant: a contact or company from another workspace is simply absent.
            if (newContact.HasValue && !await _contacts.ExistsAsync(newContact.Value))
                throw new ArgumentException("The contact does not belong to this tenant.");

            if (newCompany.HasValue && !await _companies.ExistsAsync(newCompany.Value))
                throw new ArgumentException("The company does not belong to this tenant.");

            int updated = await _queries.AttachProfileAsync(
                tenantId, userId, newContact, newCompany);

            if (updated == 0)
                throw new KeyNotFoundException("Tenant not found.");

            var attached = await GetOneAsync(tenantId);
            return attached.Successful && attached.Value is Tenant tenant
                ? Result<Tenant>.Success(tenant)
                : Result<Tenant>.Failure(attached.ErrorCode, attached.Errors);
        }

        public async Task<Result> CloseAsync(int tenantId)
        {
            var userId = RequireUserId();

            int count = await _queries.CloseAsync(tenantId, userId);

            if (count == 0)
                return Result.NotFound(string.Empty, "Tenant not found.");

            // The tenant is detached even if the membership cleanup fails; the
            // caller can no longer resolve it by user id.
            try
            {
                await _authService.RemoveUserSubscriptionAsync(userId.ToString(), _tenantContext.AppId);
            }
            catch
            {
                // Do not turn a completed close into a failed request.
            }

            return Result.Success();
        }

        public async Task<Result> PurgeAsync(int tenantId)
        {
            var userId = RequireUserId();
            var appId = _tenantContext.AppId;

            var stored = await InTransactionAsync(async (conn, tran) =>
            {
                long? owner = await _queries.GetOwnerIdAsync(tenantId, conn, tran);

                if (owner != userId)
                    throw new UnauthorizedAccessException("This tenant does not belong to the signed-in user.");

                // Read the file names before the rows that reference them go.
                var files = await _queries.GetFilesAsync(tenantId, conn, tran);
                await _queries.DeleteGraphAsync(tenantId, conn, tran);
                return files;
            });

            // Physical files are removed after the commit: a rollback must never leave
            // rows pointing at deleted files.
            foreach (var url in stored.ImageUrls)
                await _fileStoreService.DeleteAsync($"{appId}/{url}", isPublic: true);

            foreach (var url in stored.FileUrls)
                await _fileStoreService.DeleteAsync($"{appId}/{url}", isPublic: false);

            try
            {
                await _authService.RemoveUserSubscriptionAsync(userId.ToString(), appId);
            }
            catch
            {
                // The workspace is already gone; membership cleanup is best effort.
            }

            return Result.Success();
        }

        protected override async Task<Tenant> SingleAugment(Tenant tenant)
        {
            if (tenant?.Company?.LogoImageID != null)
            {
                var result = await _imageService.GetOneAsync(tenant.Company.LogoImageID.Value);
                if (result.Value is Image logoImage)
                    tenant.Company.LogoImage = logoImage;
            }
            return tenant!;
        }

        private long RequireUserId()
        {
            if (_tenantContext.UserId is long userId && userId > 0)
                return userId;

            throw new UnauthorizedAccessException("The signed-in user could not be resolved.");
        }

        private async Task<Tier> GetFreeTierAsync()
        {
            var result = await _tierService.GetAllAsync();
            var tier = result.Value?
                .Where(t => t.Active != false)
                .OrderBy(t => t.Rank ?? int.MaxValue)
                .ThenBy(t => t.ID)
                .FirstOrDefault();

            if (tier == null)
                throw new InvalidOperationException("No active tier is configured.");

            return (Tier)tier.ID;
        }

    }
}
