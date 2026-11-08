using Microsoft.AspNetCore.Mvc;
using _0nline.Biller.DL.Contract.Interfaces;
using _0nline.Biller.DL.Contract.Models.db;

/// This is auto-generated from a template. 
/// Do not modify this file directly. Instead, create a partial class in a separate file to add custom logic.
namespace _0nline.Biller.Api.Lib
{
    [Route("api/biller/[controller]")]
    public partial class TenantSettingController : BillerBaseController<TenantSetting, int>
    {
        protected override ITenantSettingService BaseService { get; } 

        public TenantSettingController(ITenantSettingService baseService) : base(baseService) {
             BaseService = baseService;
        }
    }
}