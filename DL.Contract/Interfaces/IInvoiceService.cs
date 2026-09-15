using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    public interface IInvoiceService : IDLBaseService<Invoice>, IDLReadableService<Invoice>, IDLWritableService<Invoice>
    {
        // Add contract methods here if needed
       
    }
}
