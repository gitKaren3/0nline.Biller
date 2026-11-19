using System.Reflection;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Biller.DL.Services;
using _0nline.Biller.DL.Services.Base;
using _0nline.Shared.Contract.Interfaces.DL;
using Xunit;

namespace _0nline.Biller.Test;

/// <summary>
/// Where the app contract's generic existence check is answered.
/// </summary>
/// <remarks>
/// <see cref="IDLBasicService{T}"/> carries a fallback so a WASM client compiles
/// without a new endpoint. The server must not use it: the fallback reads the row,
/// with its navigation properties and augmentation, which is exactly the cost a
/// validation check is trying to avoid. A missing server implementation would not be
/// a compile error - the interface would quietly keep the fallback - so this test is
/// the warning.
/// </remarks>
public class ServiceQueryContractTests
{
    [Fact]
    public void Exists_is_answered_by_the_app_base_not_the_interface_fallback()
    {
        var map = typeof(ContactService).GetInterfaceMap(typeof(IDLBasicService<Contact>));
        var index = Array.FindIndex(
            map.InterfaceMethods,
            method => method.Name == nameof(IDLBasicService<Contact>.ExistsAsync));

        Assert.True(index >= 0, "IDLBasicService has no ExistsAsync.");
        Assert.Equal(typeof(BillerBaseService<Contact>), map.TargetMethods[index].DeclaringType);
    }
}
