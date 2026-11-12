using _0nline.Biller.DL.Contract.Enums;

namespace _0nline.Biller.DL.Contract.Models
{
    /// <summary>
    /// A sensible first tax scheme for a country, so a registered business does not
    /// start with an empty picker. It is only a default: the user can change it, and
    /// an unknown country falls back to "Other", which prints as "Tax".
    /// </summary>
    public static class TaxSchemeDefaults
    {
        public static TaxScheme ForCountry(string? isoOrName)
        {
            string value = (isoOrName ?? string.Empty).Trim().ToUpperInvariant();

            return value switch
            {
                "US" or "USA" or "UNITED STATES" => TaxScheme.SalesTax,
                "AU" or "AUSTRALIA"
                    or "NZ" or "NEW ZEALAND"
                    or "CA" or "CANADA"
                    or "SG" or "SINGAPORE"
                    or "IN" or "INDIA"
                    or "MY" or "MALAYSIA" => TaxScheme.Gst,
                "" or "OTHER" => TaxScheme.Other,
                // The common VAT jurisdictions the catalogue already carries.
                "ZA" or "SOUTH AFRICA"
                    or "GB" or "UNITED KINGDOM"
                    or "IE" or "IRELAND"
                    or "DE" or "GERMANY"
                    or "FR" or "FRANCE"
                    or "NL" or "NETHERLANDS"
                    or "BE" or "BELGIUM"
                    or "ES" or "SPAIN"
                    or "IT" or "ITALY"
                    or "PT" or "PORTUGAL"
                    or "AE" or "UNITED ARAB EMIRATES"
                    or "KE" or "KENYA"
                    or "NG" or "NIGERIA"
                    or "GH" or "GHANA"
                    or "TZ" or "TANZANIA"
                    or "UG" or "UGANDA"
                    or "ZM" or "ZAMBIA"
                    or "ZW" or "ZIMBABWE"
                    or "BW" or "BOTSWANA"
                    or "NA" or "NAMIBIA"
                    or "LS" or "LESOTHO"
                    or "SZ" or "ESWATINI" => TaxScheme.Vat,
                _ => TaxScheme.Other
            };
        }
    }
}
