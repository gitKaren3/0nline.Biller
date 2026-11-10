using _0nline.Biller.DL.Contract.Enums;
using _0nline.Biller.DL.Contract.Models;
using _0nline.Biller.DL.Contract.Models.db;
using _0nline.Shared.Location;

namespace _0nline.Biller.DL.Reports
{
    /// <summary>
    /// Maps loaded invoice rows onto the render model. Kept separate from the
    /// snapshot service so the mapping can be exercised without a database, and
    /// separate from the layout so changing the PDF never changes what is stored.
    /// </summary>
    public static class InvoicePdfModelFactory
    {
        public static InvoicePdfModel Create(
            Invoice invoice,
            Company? issuer,
            Contact? issuerContact,
            Company? customer,
            Contact? customerContact,
            BankAccount? bank,
            IReadOnlyDictionary<short, string> rateTypes)
        {
            ArgumentNullException.ThrowIfNull(invoice);
            ArgumentNullException.ThrowIfNull(rateTypes);

            var lines = invoice.InvoiceLines
                .OrderBy(line => line.ID)
                .Select(line => new InvoicePdfLine
                {
                    Description = line.Description ?? string.Empty,
                    RateName = line.TenantRate is { RateTypeID: var rateTypeId }
                               && rateTypes.TryGetValue(rateTypeId, out var rateName)
                        ? rateName
                        : null,
                    Qty = line.Qty ?? 0m,
                    UnitPrice = line.BilledRateIncl ?? 0m,
                    Tax = line.TotalTax ?? 0m,
                    TaxRate = line.TaxRate ?? 0m,
                    TaxSchemeName = SchemeName(line.theTaxScheme),
                    TaxNote = line.TaxNote
                })
                .ToList();

            var currency = CurrencyCatalog.Default.Get(invoice.CurrencyCode);

            return new InvoicePdfModel
            {
                InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                InvoiceDate = invoice.InvoiceDate ?? DateTime.MinValue,
                PaymentRef = invoice.PaymentRef,
                StatusName = invoice.InvoiceStatus?.Name,
                CurrencyCode = currency.Code,
                CurrencySymbol = currency.Symbol,
                CurrencyMinorUnits = invoice.CurrencyMinorUnits ?? currency.MinorUnits,
                TaxSchemeName = SchemeName(issuer?.theTaxScheme),
                Issuer = ToParty(issuer, issuerContact),
                Customer = ToParty(customer, customerContact),
                Bank = ToBank(bank),
                Lines = lines,
                TotalTax = lines.Sum(line => line.Tax),
                Total = lines.Sum(line => line.Amount)
            };
        }

        private static InvoicePdfParty ToParty(Company? company, Contact? contact)
        {
            if (company is null)
            {
                return new InvoicePdfParty
                {
                    Name = ContactName(contact) ?? string.Empty,
                    Email = contact?.Email,
                    Phone = contact?.CellPhone?.ToString()
                };
            }

            return new InvoicePdfParty
            {
                Name = company.Name ?? string.Empty,
                Registration = company.Registration,
                TaxNumber = company.TaxNumber,
                TaxSchemeName = SchemeName(company.theTaxScheme),
                ContactName = ContactName(contact),
                Email = contact?.Email ?? company.BillingContact?.Email,
                Phone = company.PhoneNo ?? contact?.CellPhone?.ToString(),
                Address = company.Address,
                AddressCity = company.AddressCity,
                AddressCode = company.AddressCode,
                AddressCountry = company.AddressCountry
            };
        }

        /// <summary>
        /// A stable display label for a tax scheme. The enum member names are C#
        /// identifiers (<c>Vat</c>), not what an invoice should print.
        /// </summary>
        private static string? SchemeName(TaxScheme? scheme) => scheme switch
        {
            TaxScheme.Vat => "VAT",
            TaxScheme.Gst => "GST",
            TaxScheme.SalesTax => "Sales Tax",
            TaxScheme.Other => "Tax",
            _ => null
        };

        private static InvoicePdfBank? ToBank(BankAccount? bank)
        {
            if (bank is null)
                return null;

            return new InvoicePdfBank
            {
                AccountName = bank.AccountName ?? string.Empty,
                BankName = bank.BankName ?? string.Empty,
                AccountNumber = bank.AccountNumber ?? string.Empty,
                BranchCode = bank.BranchCode,
                AccountType = bank.AccountType,
                SwiftCode = bank.SwiftCode
            };
        }

        private static string? ContactName(Contact? contact)
        {
            if (contact is null)
                return null;

            var name = string.Join(" ", new[] { contact.FirstName, contact.Surname }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }
}
