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
                            row.RelativeItem().Element(cell => Party(cell, "FROM", model.Issuer));
                            row.ConstantItem(12);
                            row.RelativeItem().Element(cell => Party(cell, "BILL TO", model.Customer));
                        });

                        column.Item().Element(cell => Lines(cell, model));

                        column.Item().AlignRight().Width(70, Unit.Millimetre).Element(cell => Totals(cell, model));

                        if (model.Bank is not null)
                            column.Item().Element(cell => Bank(cell, model.Bank));

                        if (!string.IsNullOrWhiteSpace(model.PaymentRef))
                        {
                            column.Item().Text(text =>
                            {
                                text.Span("Payment reference: ").SemiBold();
                                text.Span(model.PaymentRef);
                            });
                        }
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
                    column.Item().Text(string.IsNullOrWhiteSpace(model.Issuer.Name)
                            ? "Invoice"
                            : model.Issuer.Name)
                        .SemiBold().FontSize(16);

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
                        column.Item().AlignRight().Text(model.InvoiceDate.ToString("dd MMMM yyyy"))
                            .FontColor(Muted);

                    if (!string.IsNullOrWhiteSpace(model.StatusName))
                        column.Item().AlignRight().Text(model.StatusName).FontColor(Muted);
                });
            });
        }

        private static void Party(IContainer container, string heading, InvoicePdfParty party)
        {
            container.Column(column =>
            {
                column.Item().Text(heading).FontSize(7).SemiBold().FontColor(Muted);

                if (!string.IsNullOrWhiteSpace(party.Name))
                    column.Item().PaddingTop(2).Text(party.Name).SemiBold();

                if (!string.IsNullOrWhiteSpace(party.ContactName))
                    column.Item().Text(party.ContactName);

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

        private static void Lines(IContainer container, InvoicePdfModel model)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(6);
                    columns.ConstantColumn(28);
                    columns.ConstantColumn(62);
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(62);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Description");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Rate");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Tax");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                });

                foreach (var line in model.Lines)
                {
                    table.Cell().Element(BodyCell).Column(column =>
                    {
                        column.Item().Text(line.Description);
                        if (!string.IsNullOrWhiteSpace(line.RateName))
                            column.Item().Text(line.RateName).FontSize(7).FontColor(Muted);
                    });

                    table.Cell().Element(BodyCell).AlignRight().Text(line.Qty.ToString());
                    table.Cell().Element(BodyCell).AlignRight().Text(Money(line.UnitPrice, model));
                    table.Cell().Element(BodyCell).AlignRight().Text(Money(line.Tax, model));
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
                    row.RelativeItem().Text(model.TaxSchemeName is { } scheme ? $"Tax ({scheme})" : "Tax")
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

        private static void Bank(IContainer container, InvoicePdfBank bank)
        {
            container.Border(0.5f).BorderColor(Rule).Background(Colors.Grey.Lighten4).Padding(10).Column(column =>
            {
                column.Item().Text("Payment details").SemiBold();

                column.Item().PaddingTop(4).Text($"Account holder: {bank.AccountName}");
                column.Item().Text($"Bank: {bank.BankName}");
                column.Item().Text($"Account number: {bank.AccountNumber}");

                var branch = Join(bank.BranchCode, bank.AccountType);
                if (!string.IsNullOrWhiteSpace(branch))
                    column.Item().Text(branch);

                if (!string.IsNullOrWhiteSpace(bank.SwiftCode))
                    column.Item().Text($"SWIFT: {bank.SwiftCode}");
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
                .Background(Colors.Grey.Lighten3)
                .PaddingVertical(4)
                .PaddingHorizontal(3)
                .DefaultTextStyle(style => style.SemiBold());

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
