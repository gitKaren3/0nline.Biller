using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Db.Contract.Models;
using _0nline.Shared.Db.Service;

/// This is auto-generated from QueryGenerator.py - do not modify.
namespace _0nline.Biller.DL.Providers
{
    public partial class TenantSettingSqlQueryProvider : SqlQueryProvider<TenantSetting>
    {
        public TenantSettingSqlQueryProvider()
            : base(new DbTableDescriptor("TenantSetting")
            {
                KeyColumns = new[] { "ID" },
                EditableColumns = new[] { "TenantID", "CurrencyCode", "CurrencyMinorUnits", "TimeZoneId", "DefaultTaxRate" },
                ActiveColumn = null,
                LookupLabel = new LookupLabel(new[] { "CurrencyCode" }, " "),
                LookupFilter = null
            })
        { }
    }
}