using _0nline.Shared.Contract.Interfaces.DL;
using _0nline.Biller.DL.Contract.Models.db;

namespace _0nline.Biller.DL.Contract.Interfaces
{
    public interface IContactService : IDLReadableService<Contact>, IDLWritableService<Contact>
    {
        
    }
}
