using OfficeOpenXml;
using OfficeOpenXml.Style;
using OrganizationAppClient.Models;
using System.Drawing;

namespace OrganizationAppClient.Services;

public class ExcelExporter
{
    public async Task ExportToExcelAsync(List<OrganizationDto> organizations, string filePath)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        
        // ← Используем обычный using, не await using
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Организации");
        
        // Заголовки
        worksheet.Cells["A1"].Value = "ID";
        worksheet.Cells["B1"].Value = "Название";
        worksheet.Cells["C1"].Value = "ИНН";
        worksheet.Cells["D1"].Value = "Тип";
        worksheet.Cells["E1"].Value = "Сотрудников";
        
        // Стилизация заголовков
        using (var range = worksheet.Cells["A1:E1"])
        {
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(45, 85, 125));
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.Font.Bold = true;
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }
        
        // Данные
        for (int i = 0; i < organizations.Count; i++)
        {
            var org = organizations[i];
            var row = i + 2;
            
            worksheet.Cells[$"A{row}"].Value = org.Id;
            worksheet.Cells[$"B{row}"].Value = org.Name;
            worksheet.Cells[$"C{row}"].Value = org.Inn;
            worksheet.Cells[$"D{row}"].Value = org.Type;
            worksheet.Cells[$"E{row}"].Value = org.EmployeeCount;
            
            if (org.Type == "Государственная")
                worksheet.Cells[$"D{row}"].Style.Font.Color.SetColor(Color.Blue);
            else if (org.Type == "Частная")
                worksheet.Cells[$"D{row}"].Style.Font.Color.SetColor(Color.DarkGreen);
        }
        
        // Автоширина колонок
        worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        
        // Итоговая строка
        var lastRow = organizations.Count + 2;
        worksheet.Cells[$"A{lastRow}"].Value = "ИТОГО:";
        worksheet.Cells[$"A{lastRow}"].Style.Font.Bold = true;
        worksheet.Cells[$"E{lastRow}"].Formula = $"SUM(E2:E{lastRow - 1})";
        worksheet.Cells[$"E{lastRow}"].Style.Font.Bold = true;
        
        // ← Save() без параметров, синхронный
        await Task.Run(() => package.SaveAs(new FileInfo(filePath)));
        Console.WriteLine($"✓ Экспортировано {organizations.Count} записей в Excel: {filePath}");
    }
}