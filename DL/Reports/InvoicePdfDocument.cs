using _0nline.Biller.DL.Contract.Models;
using _0nline.Shared.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace _0nline.Biller.DL.Reports
{
    /// <summary>
    /// Biller's invoice layout. Renders only from <see cref="InvoicePdfModel"/>;
    /// it must never read a database, because the same model is replayed for a
    /// public link long after the rows have changed.
    /// </summary>
    public sealed class InvoicePdfDocument : IPdfDocument<InvoicePdfModel>
    {
        private static readonly string Ink = Colors.Grey.Darken3;
        private static readonly string Muted = Colors.Grey.Darken1;
        private static readonly string Rule = Colors.Grey.Lighten2;

        // Configurable presentation colours for the grey areas. Adjust here to theme them.
        private static readonly string HeaderBackground = Colors.Grey.Lighten3;
        private static readonly string HeaderText = Colors.Grey.Darken3;
        private static readonly string BankBackground = Colors.Grey.Lighten4;
        private static readonly string BankBorder = Colors.Grey.Lighten2;
        private static readonly string BankText = Colors.Grey.Darken3;

        public void Compose(IDocumentContainer container, InvoicePdfModel model)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(9).FontColor(Ink).LineHeight(1.25f));

                page.Header().Element(header => Header(header, model));

                page.Content()
                    .PaddingVertical(0.8f, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(14);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Element(cell => Party(cell, "FROM", model.Issuer, personFirst: true));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(cell => Party(cell, "BILL TO", model.Customer));
                        });

                        column.Item().Element(cell => Lines(cell, model));

                        column.Item().AlignRight().Width(70, Unit.Millimetre).Element(cell => Totals(cell, model));

                        if (model.Bank is not null)
                            column.Item().Element(cell => Bank(cell, model.Bank, model.PaymentRef));
                    });

                page.Footer().Element(cell => Footer(cell, model));
            });
        }

        private static void Header(IContainer container, InvoicePdfModel model)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    if (model.Issuer.Logo is { Length: > 0 } logo)
                    {
                        column.Item().Width(30, Unit.Millimetre).Image(logo).FitWidth();
                        column.Item().PaddingBottom(4);
                    }

                    var (issuerPrimary, issuerSecondary) = HeaderIdentity(model.Issuer);

                    column.Item().Text(string.IsNullOrWhiteSpace(issuerPrimary)
                            ? "Invoice"
                            : issuerPrimary)
                        .SemiBold().FontSize(16);

                    if (!string.IsNullOrWhiteSpace(issuerSecondary))
                        column.Item().Text(issuerSecondary).FontSize(10).FontColor(Muted);

                    if (!string.IsNullOrWhiteSpace(model.Issuer.Registration))
                        column.Item().Text($"Registration {model.Issuer.Registration}")
                            .FontSize(8).FontColor(Muted);
                });

                row.ConstantItem(60, Unit.Millimetre).AlignRight().Column(column =>
                {
                    column.Item().AlignRight().Text("INVOICE")
                        .FontSize(20).SemiBold().FontColor(Colors.Grey.Darken2);

                    column.Item().AlignRight().Text(model.InvoiceNumber).FontSize(11).SemiBold();

                    if (model.InvoiceDate != DateTime.MinValue)
                        column.Item().AlignRight().Text(model.InvoiceDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture))
                            .FontColor(Muted);

                    if (!string.IsNullOrWhiteSpace(model.StatusName))
                        column.Item().AlignRight().Text(model.StatusName).FontColor(Muted);
                });
            });
        }

        private static void Party(IContainer container, string heading, InvoicePdfParty party, bool personFirst = false)
        {
            container.Column(column =>
            {
                column.Item().Text(heading).FontSize(7).SemiBold().FontColor(Muted);

                if (personFirst)
                {
                    var (primary, secondary) = Identity(party, personFirst: true);

                    if (!string.IsNullOrWhiteSpace(primary))
                        column.Item().PaddingTop(2).Text(primary).SemiBold();

                    if (!string.IsNullOrWhiteSpace(secondary))
                        column.Item().Text(secondary);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(party.Name))
                        column.Item().PaddingTop(2).Text(party.Name).SemiBold();

                    if (!string.IsNullOrWhiteSpace(party.ContactName))
                        column.Item().Text(party.ContactName);
                }

                if (!string.IsNullOrWhiteSpace(party.Address))
                    column.Item().Text(party.Address);

                var cityLine = Join(party.AddressCity, party.AddressCode);
                if (!string.IsNullOrWhiteSpace(cityLine))
                    column.Item().Text(cityLine);

                if (!string.IsNullOrWhiteSpace(party.AddressCountry))
                    column.Item().Text(party.AddressCountry);

                if (!string.IsNullOrWhiteSpace(party.TaxNumber))
                {
                    column.Item().Text($"{party.TaxSchemeName ?? "Tax"} No {party.TaxNumber}")
                        .FontSize(8).FontColor(Muted);
                }

                if (!string.IsNullOrWhiteSpace(party.Email))
                    column.Item().PaddingTop(2).Text(party.Email).FontColor(Muted);

                if (!string.IsNullOrWhiteSpace(party.Phone))
                    column.Item().Text(party.Phone).FontColor(Muted);
            });
        }

        /// <summary>
        /// The issuer's title. A registered company shows only the company name; an
        /// independent / sole trader leads with the person's name and shows the
        /// business / trading name or job title underneath.
        /// </summary>
        private static (string Primary, string? Secondary) HeaderIdentity(InvoicePdfParty party)
        {
            if (party.IsIndependent)
                return Identity(party, personFirst: true);

            return (string.IsNullOrWhiteSpace(party.Name)
                ? party.ContactName ?? string.Empty
                : party.Name, null);
        }

        /// <summary>
        /// Chooses which name leads. With <paramref name="personFirst"/>, the person's
        /// name leads and the business / trading name or job title follows.
        /// </summary>
        private static (string Primary, string? Secondary) Identity(InvoicePdfParty party, bool personFirst)
        {
            if (personFirst && !string.IsNullOrWhiteSpace(party.ContactName))
                return (party.ContactName, string.IsNullOrWhiteSpace(party.Name) ? null : party.Name);

            if (!string.IsNullOrWhiteSpace(party.Name))
                return (party.Name, string.IsNullOrWhiteSpace(party.ContactName) ? null : party.ContactName);

            return (party.ContactName ?? string.Empty, null);
        }

        private static void Lines(IContainer container, InvoicePdfModel model)
        {
            bool showTax = ShowsTax(model);

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(6);
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(62);
                    if (showTax)
                        columns.ConstantColumn(52);
                    columns.ConstantColumn(62);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCellFirst).Text("Description");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderCell).AlignRight().Text(showTax ? "Excl" : "Rate");
                    if (showTax)
                        header.Cell().Element(HeaderCell).AlignRight().Text("Tax");
                    header.Cell().Element(HeaderCellLast).AlignRight().Text("Amount");
                });

                foreach (var line in model.Lines)
                {
                    table.Cell().Element(BodyCell).Column(column =>
                    {
                        column.Item().Text(line.Description);
                        if (!string.IsNullOrWhiteSpace(line.RateName))
                            column.Item().Text(line.RateName).FontSize(7).FontColor(Muted);
                    });

                    table.Cell().Element(BodyCell).AlignRight().Text(line.Qty.ToString("0.##", CultureInfo.InvariantCulture));
                    table.Cell().Element(BodyCell).AlignRight()
                        .Text(Money(showTax ? line.UnitPriceExcl : line.UnitPrice, model));
                    if (showTax)
                        table.Cell().Element(BodyCell).AlignRight().Text(Money(line.UnitTax, model));
                    table.Cell().Element(BodyCell).AlignRight().Text(Money(line.Amount, model));
                }
            });
        }

        private static void Totals(IContainer container, InvoicePdfModel model)
        {
            container.Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem()
                        .Text(model.TaxSchemeName is { } scheme ? $"Tax ({scheme})" : "Tax (VAT)")
                        .FontColor(Muted);
                    row.ConstantItem(32, Unit.Millimetre).AlignRight().Text(Money(model.TotalTax, model));
                });

                column.Item().PaddingTop(4).BorderTop(1).BorderColor(Rule).PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Text("Total").SemiBold().FontSize(11);
                    row.ConstantItem(32, Unit.Millimetre).AlignRight().Text(Money(model.Total, model))
                        .SemiBold().FontSize(11);
                });
            });
        }

        private static bool ShowsTax(InvoicePdfModel model)
            => !string.IsNullOrWhiteSpace(model.TaxSchemeName);

        private static void Bank(IContainer container, InvoicePdfBank bank, string? paymentRef)
        {
            container.Border(0.5f).BorderColor(BankBorder).Background(BankBackground).CornerRadius(8).Padding(10).Column(column =>
            {
                column.Item().Text("Payment details").SemiBold().FontColor(BankText);

                column.Item().PaddingTop(4).Text($"Account holder: {bank.AccountName}").FontColor(BankText);
                column.Item().Text($"Bank: {bank.BankName}").FontColor(BankText);
                column.Item().Text($"Account number: {bank.AccountNumber}").FontColor(BankText);

                var branch = Join(bank.BranchCode, bank.AccountType);
                if (!string.IsNullOrWhiteSpace(branch))
                    column.Item().Text(branch).FontColor(BankText);

                if (!string.IsNullOrWhiteSpace(bank.SwiftCode))
                    column.Item().Text($"SWIFT: {bank.SwiftCode}").FontColor(BankText);

                if (!string.IsNullOrWhiteSpace(paymentRef))
                {
                    column.Item().PaddingTop(4).Text(text =>
                    {
                        text.Span("Payment reference: ").SemiBold().FontColor(BankText);
                        text.Span(paymentRef).FontColor(BankText);
                    });
                }
            });
        }

        private static void Footer(IContainer container, InvoicePdfModel model)
        {
            container.Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(7).FontColor(Muted));

                    if (!string.IsNullOrWhiteSpace(model.Issuer.Name))
                        text.Span(model.Issuer.Name);

                    if (!string.IsNullOrWhiteSpace(model.Issuer.Email))
                    {
                        if (!string.IsNullOrWhiteSpace(model.Issuer.Name))
                            text.Span("  ·  ");

                        text.Span(model.Issuer.Email);
                    }
                });

                row.ConstantItem(40, Unit.Millimetre).AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(style => style.FontSize(7).FontColor(Muted));
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }

        private static IContainer HeaderCell(IContainer container)
            => container
                .Background(HeaderBackground)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(style => style.SemiBold().FontColor(HeaderText));

        private static IContainer HeaderCellFirst(IContainer container)
            => container
                .Background(HeaderBackground)
                .CornerRadiusTopLeft(4)
                .CornerRadiusBottomLeft(4)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(style => style.SemiBold().FontColor(HeaderText));

        private static IContainer HeaderCellLast(IContainer container)
            => container
                .Background(HeaderBackground)
                .CornerRadiusTopRight(4)
                .CornerRadiusBottomRight(4)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(style => style.SemiBold().FontColor(HeaderText));

        private static IContainer BodyCell(IContainer container)
            => container
                .BorderBottom(0.5f)
                .BorderColor(Rule)
                .PaddingVertical(4)
                .PaddingHorizontal(3);

        private static string? Join(params string?[] parts)
        {
            var value = string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Money as the invoice stored it. The symbol and the number of decimal places
        /// are frozen in the model, so this is independent of the server's culture and
        /// of later changes to the currency catalogue.
        /// </summary>
        private static string Money(decimal value, InvoicePdfModel model)
        {
            int decimals = Math.Clamp(model.CurrencyMinorUnits, 0, 4);
            return $"{model.CurrencySymbol} {value.ToString("N" + decimals, CultureInfo.InvariantCulture)}";
        }
    }
}
