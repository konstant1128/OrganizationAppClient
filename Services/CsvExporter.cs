using CsvHelper;
using CsvHelper.Configuration;
using OrganizationAppClient.Models;
using System.Globalization;
using System.Text;

namespace OrganizationAppClient.Services;

public class CsvExporter
{
    public async Task ExportToCsvAsync(List<OrganizationDto> organizations, string filePath)
    {
        await using var writer = new StreamWriter(filePath, append: false, Encoding.UTF8);
        await using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            Encoding = Encoding.UTF8
        });
        
        csv.Context.RegisterClassMap<OrganizationMap>();
        await csv.WriteRecordsAsync(organizations);
        
        Console.WriteLine($"Экспортировано {organizations.Count} записей в {filePath}");
    }
}

public class OrganizationMap : ClassMap<OrganizationDto>
{
    public OrganizationMap()
    {
        Map(o => o.Id).Name("ID");
        Map(o => o.Name).Name("Название");
        Map(o => o.Inn).Name("ИНН");
        Map(o => o.Type).Name("Тип");
        Map(o => o.EmployeeCount).Name("Сотрудников");
    }
}