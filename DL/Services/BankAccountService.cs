using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Queries;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract;
using _0nline.Shared.Contract.Interfaces;
using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Shared.Contract.Models;
using _0nline.Shared.Db.Contract.Interfaces;

namespace _0nline.Biller.DL.Services
{
    /// <summary>
    /// The tenant's bank accounts. Beyond plain CRUD this keeps one invariant:
    /// a tenant with an active account always has exactly one default account,
    /// which is the one invoice payment instructions use.
    /// </summary>
    public class BankAccountService : BillerBaseService<BankAccount>, IBankAccountService, IDLReadableService<BankAccount>, IDLWritableService<BankAccount>
    {
        private readonly IBankAccountQueries _queries;

        public BankAccountService(IDbContextProvider<BillerDbConfig> ctx
                                , ISqlQueryProvider<BankAccount> queryService
                                , ITenantContext tenancyContext
                                , IBankAccountQueries queries)
            : base(ctx, queryService, tenancyContext)
        {
            _queries = queries;
        }

        public new Task<Result<BankAccount[]>> GetAllAsync() => base.GetAllAsync();
        public new Task<Result<IReadOnlyDictionary<int, string>>> GetLookupAsync() => base.GetLookupAsync();

        public new Task<Result<PageOf<BankAccount>>> GetPageAsync(int page, int pageSize)
            => base.GetPageAsync(page, pageSize);

        public new Task<Result<BankAccount>> CreateAsync(BankAccount entity) => CreateAccountAsync(entity);
        public new Task<Result<BankAccount>> UpdateAsync(BankAccount entity) => UpdateAccountAsync(entity);

        public new async Task<Result<BankAccount?>> DeleteOrDeactivateAsync(int id)
        {
            var deleted = await base.DeleteOrDeactivateAsync(id);

            // Removing the default must not leave the tenant without one.
            if (deleted.Successful)
                await _queries.EnsureDefaultAsync();

            return deleted;
        }

        private async Task<Result<BankAccount>> CreateAccountAsync(BankAccount entity)
        {
            if (entity.ID > 0)
                entity.ID = 0;

            Normalize(entity);

            // Demote first: the partial unique index allows only one default,
            // and it is checked per statement.
            if (entity.IsDefault == true)
                await _queries.ClearDefaultAsync(excludeId: null);

            try
            {
                var created = await base.CreateAsync(entity);

                // The first account becomes the default even if the client sent false.
                await _queries.EnsureDefaultAsync();

                // EnsureDefaultAsync writes straight to the database, so the entity
                // still carries the stale flag. Return the persisted row instead.
                return await FreshAsync(entity.ID, created);
            }
            catch (Exception ex) when (IsUniqueViolation(ex))
            {
                // Only UX_BankAccount_Default can raise this: another tab promoted
                // an account between the demote and the insert.
                await _queries.EnsureDefaultAsync();

                return Result<BankAccount>.Failure(
                    ErrorCode.Conflict,
                    "Another account was just made the default. Reload and try again.");
            }
            catch
            {
                // The demote above already ran; the tenant must not be left with
                // no default just because the insert failed.
                await _queries.EnsureDefaultAsync();
                throw;
            }
        }

        private async Task<Result<BankAccount>> UpdateAccountAsync(BankAccount entity)
        {
            if (entity.ID <= 0)
                throw new KeyNotFoundException("Bank account not found.");

            // Fail before demoting anything: the update below is tenant-scoped,
            // but a bad id should not change which account is the default.
            var existing = await GetOneAsync(entity.ID);
            if (!existing.Successful || existing.Value is not BankAccount)
                throw new KeyNotFoundException("Bank account not found.");

            Normalize(entity);

            if (entity.IsDefault == true)
                await _queries.ClearDefaultAsync(entity.ID);

            try
            {
                var updated = await base.UpdateAsync(entity);

                await _queries.EnsureDefaultAsync();

                // Same stale-flag issue as create: return the persisted row.
                return await FreshAsync(entity.ID, updated);
            }
            catch
            {
                // The demote above already ran; restore a default before failing.
                await _queries.EnsureDefaultAsync();
                throw;
            }
        }

        /// <summary>
        /// Re-reads a row after a write. The default-invariant maintenance writes
        /// straight to the database, so the entity a write started from is stale
        /// by the time the maintenance has run.
        /// </summary>
        private async Task<Result<BankAccount>> FreshAsync(int id, Result<BankAccount> fallback)
        {
            var fresh = await GetOneAsync(id);
            return fresh.Successful && fresh.Value is BankAccount row
                ? Result<BankAccount>.Success(row)
                : fallback;
        }

        /// <summary>
        /// Clears whitespace the user may have typed. The account number is stored
        /// without spaces so a number copied from a statement still compares well.
        /// </summary>
        private static void Normalize(BankAccount entity)
        {
            entity.AccountName = entity.AccountName?.Trim();
            entity.BankName = entity.BankName?.Trim();
            entity.AccountNumber = entity.AccountNumber?.Replace(" ", string.Empty);
            entity.BranchCode = NullIfBlank(entity.BranchCode);
            entity.AccountType = NullIfBlank(entity.AccountType);
            entity.SwiftCode = NullIfBlank(entity.SwiftCode)?.ToUpperInvariant();

            // IsDefault is NOT NULL in the database and has no default value, so a
            // missing value must be sent as false rather than NULL.
            entity.IsDefault ??= false;

            // Ownership is server-owned; the base service also strips it.
            entity.Tenant = null;
        }

        private static string? NullIfBlank(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    }
}
