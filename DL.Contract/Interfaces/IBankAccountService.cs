using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Contract.Interfaces.DL;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    /// <summary>
    /// The tenant's own bank accounts. Rows are tenant-scoped, so a generated
    /// controller cannot read or write another tenant's accounts.
    /// </summary>
    public interface IBankAccountService : IDLReadableService<BankAccount>, IDLWritableService<BankAccount>
    {
    }
}
