using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using OrganizationAppClient.Models; // Убедитесь, что namespace совпадает с вашим проектом

namespace OrganizationAppClient.Services;

public class PdfExporter
{
    public byte[] ExportToPdf(List<OrganizationDto> organizations, string title = "Отчёт по организациям")
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(c => ComposeContent(c, organizations, title));
                
                // Футер
                page.Footer().Element(container =>
                {
                    container.AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Medium));
                        text.Span("Страница ");
                        text.CurrentPageNumber();
                        text.Span(" из ");
                        text.TotalPages();
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    // Обёртываем шапку в Column, чтобы избежать ошибки multiple child elements
    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("СИСТЕМА УЧЁТА ОРГАНИЗАЦИЙ")
                        .FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().Text("Автоматический отчёт")
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                });

                row.ConstantItem(120).AlignRight().Text(text =>
                {
                    text.Span("Дата: ").FontSize(9);
                    text.Span(DateTime.Now.ToString("dd.MM.yyyy")).FontSize(9).Bold();
                });
            });

            // Линия отделяется как отдельный элемент колонки
            col.Item().PaddingTop(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container, List<OrganizationDto> organizations, string title)
    {
        container.Column(col =>
        {
            // Заголовок отчёта
            col.Item().PaddingVertical(10).Text(title)
                .FontSize(16).Bold().FontColor(Colors.Blue.Medium)
                .AlignCenter();

            // Статистика через Row
            col.Item().PaddingVertical(10).Row(row =>
            {
                var statBox = (IContainer c) => c.Padding(5).Background(Colors.Grey.Lighten4).AlignCenter();
                
                row.RelativeItem().Element(statBox).Text($"Всего: {organizations.Count}").FontSize(10);
                row.RelativeItem().Element(statBox).Text($"Типов: {organizations.Select(o => o.Type).Distinct().Count()}").FontSize(10);
                row.RelativeItem().Element(statBox).Text($"Сотрудников: {organizations.Sum(o => o.EmployeeCount)}").FontSize(10);
            });

            // Таблица данных
            col.Item().PaddingVertical(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);   // ID
                    columns.RelativeColumn(3);    // Название
                    columns.ConstantColumn(100);  // ИНН
                    columns.ConstantColumn(80);   // Тип
                    columns.ConstantColumn(70);   // Сотрудники
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("ID");
                    header.Cell().Element(CellStyle).Text("Название");
                    header.Cell().Element(CellStyle).Text("ИНН");
                    header.Cell().Element(CellStyle).Text("Тип");
                    header.Cell().Element(CellStyle).Text("Сотр.");
                });

                foreach (var org in organizations)
                {
                    table.Cell().Element(CellStyle).Text(org.Id.ToString());
                    table.Cell().Element(CellStyle).Text(org.Name);
                    table.Cell().Element(CellStyle).Text(org.Inn);
                    table.Cell().Element(CellStyle).Text(org.Type)
                        .FontColor(org.Type == "Государственная" ? Colors.Blue.Medium : Colors.Green.Medium);
                    table.Cell().Element(CellStyle).Text(org.EmployeeCount.ToString()).AlignRight();
                }
            });

            // Примечание
            col.Item().PaddingTop(20).Text("Примечание: данные актуальны на момент формирования отчёта")
                .FontSize(8).FontColor(Colors.Grey.Medium).Italic();
        });
    }

    private static IContainer CellStyle(IContainer container) =>
        container.Padding(5).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2);
}