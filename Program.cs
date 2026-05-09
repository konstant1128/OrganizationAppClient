using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OrganizationAppClient.Extensions;
using OrganizationAppClient.Models;
using OrganizationAppClient.Services;
using System.Text;

namespace OrganizationAppClient;

class Program
{
    static async Task Main(string[] args)
    {
        // Настройка хоста и зависимостей
        var builder = Host.CreateApplicationBuilder(args);

        // URL вашего API (из ЛР4)
        builder.Services.AddOrganizationClient("http://localhost:5147");

        var app = builder.Build();

        // Обработка Ctrl+C для отмены операций
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            Console.WriteLine("\nЗапрос на отмену...");
            cts.Cancel();
            e.Cancel = true;
        };

        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        try
        {
            await RunAsync(app.Services, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nОперация отменена пользователем");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nКритическая ошибка: {ex.Message}");
        }
        finally
        {
            cts.Dispose();
            Console.WriteLine("\nЗавершение работы клиента...");
        }
    }

    static async Task RunAsync(IServiceProvider services, CancellationToken ct)
    {
        var cacheService = services.GetRequiredService<CachedOrganizationService>();
        var csvExporter = services.GetRequiredService<CsvExporter>();
        var excelExporter = services.GetRequiredService<ExcelExporter>();
        var pdfExporter = services.GetRequiredService<PdfExporter>();

        PrintHeader();
        PrintMenu();

        while (!ct.IsCancellationRequested)
        {
            Console.Write("\n> ");
            var key = Console.ReadKey(intercept: true).KeyChar;
            Console.WriteLine();

            try
            {
                switch (key)
                {
                    case '1':
                        await ShowOrganizationsAsync(cacheService, ct);
                        break;
                    case '2':
                        await ExportDataAsync(cacheService, csvExporter, "csv", ct);
                        break;
                    case '3':
                        await ExportDataAsync(cacheService, excelExporter, "xlsx", ct);
                        break;
                    case '4':
                        await ExportDataAsync(cacheService, pdfExporter, "pdf", ct);
                        break;
                    case '5':
                        cacheService.InvalidateCache();
                        break;
                    case '6':
                        await SearchOrganizationsAsync(cacheService, ct);
                        break;
                    case 'H':
                    case 'h':
                        PrintMenu();
                        break;
                    case 'Q':
                    case 'q':
                        return;
                    default:
                        Console.WriteLine("Неизвестная команда. Нажмите 'H' для справки.");
                        break;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                Console.WriteLine("\nОперация отменена");
                break;
            }
            catch (ClientException ex)
            {
                Console.WriteLine($"\n[{ex.GetType().Name}] {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"   Внутренняя ошибка: {ex.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nНеожиданная ошибка: {ex.Message}");
#if DEBUG
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
#endif
            }
        }
    }

    static async Task ShowOrganizationsAsync(CachedOrganizationService cacheService, CancellationToken ct)
    {
        Console.Write("Страница [1]: ");
        var pageInput = Console.ReadLine()?.Trim();
        int page = string.IsNullOrEmpty(pageInput) ? 1 : int.Parse(pageInput);

        Console.Write("Размер страницы [10]: ");
        var sizeInput = Console.ReadLine()?.Trim();
        int pageSize = string.IsNullOrEmpty(sizeInput) ? 10 : int.Parse(sizeInput);

        var request = new GetOrganizationsRequest { Page = page, PageSize = pageSize };
        var result = await cacheService.GetOrganizationsAsync(request, ct);

        Console.WriteLine($"\nНайдено организаций: {result.TotalCount} (страница {result.Page}/{result.TotalPages})");
        Console.WriteLine(new string('─', 80));

        if (result.Items.Count == 0)
        {
            Console.WriteLine("   Нет данных для отображения");
            return;
        }

        foreach (var org in result.Items)
        {
            var typeIcon = org.Type switch
            {
                "Государственная" => "[G]",
                "Частная" => "[P]",
                "ИП" => "[I]",
                _ => "[*]"
            };
            Console.WriteLine($"   {typeIcon} [{org.Id,4}] {org.Name,-30} ИНН:{org.Inn}  сотр:{org.EmployeeCount}");
        }
        Console.WriteLine(new string('─', 80));
    }

    static async Task SearchOrganizationsAsync(CachedOrganizationService cacheService, CancellationToken ct)
    {
        Console.Write("Введите ИНН для поиска: ");
        var inn = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(inn))
        {
            Console.WriteLine("ИНН не указан. Поиск отменён.");
            return;
        }

        // Загружаем все организации (большой pageSize, чтобы захватить всё)
        Console.WriteLine("Загрузка данных для поиска...");
        var request = new GetOrganizationsRequest
        {
            Page = 1,
            PageSize = 1000,
            Search = null,
            Type = null
        };

        var result = await cacheService.GetOrganizationsAsync(request, ct);

        // Фильтрация на клиенте: оставляем только совпадения по ИНН
        var filtered = result.Items
            .Where(o => o.Inn?.Contains(inn, StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        Console.WriteLine($"\nРезультаты поиска по ИНН: {inn}");
        Console.WriteLine($"   Найдено записей: {filtered.Count} (из {result.Items.Count} загруженных)");

        if (filtered.Count == 0)
        {
            Console.WriteLine("   Организаций с указанным ИНН не найдено");
            return;
        }

        Console.WriteLine(new string('─', 90));
        foreach (var org in filtered)
        {
            Console.WriteLine($"   [{org.Id,4}] {org.Name,-30} | ИНН: {org.Inn,-12} | Тип: {org.Type,-20} | Штат: {org.EmployeeCount}");
        }
        Console.WriteLine(new string('─', 90));
    }
    static async Task ExportDataAsync<TExporter>(
        CachedOrganizationService cacheService,
        TExporter exporter,
        string format,
        CancellationToken ct)
        where TExporter : class
    {
        Console.Write("Имя файла [organizations]: ");
        var fileName = Console.ReadLine()?.Trim() ?? "organizations";
        var filePath = $"{fileName}.{format}";

        var request = new GetOrganizationsRequest { Page = 1, PageSize = 100 };
        Console.WriteLine("Загрузка данных...");
        var result = await cacheService.GetOrganizationsAsync(request, ct);

        if (result.Items.Count == 0)
        {
            Console.WriteLine("Нет данных для экспорта");
            return;
        }

        // Прогресс-бар
        var progress = new ColoredProgress();
        var progressTask = Task.Run(async () =>
        {
            for (int i = 0; i <= 100; i += 5)
            {
                if (ct.IsCancellationRequested) break;
                progress.Report(i);
                await Task.Delay(40, ct);
            }
        }, ct);

        // Экспорт в зависимости от типа экспортера
        switch (exporter)
        {
            case CsvExporter csv:
                await csv.ExportToCsvAsync(result.Items, filePath);
                break;
            case ExcelExporter excel:
                await excel.ExportToExcelAsync(result.Items, filePath);
                break;
            case PdfExporter pdf:
                var pdfBytes = pdf.ExportToPdf(result.Items, $"Отчёт по организациям ({DateTime.Now:dd.MM.yyyy})");
                await File.WriteAllBytesAsync(filePath, pdfBytes, ct);
                Console.WriteLine($"Экспортировано {result.Items.Count} записей в PDF: {filePath}");
                break;
        }

        await progressTask;
    }

    static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
╔════════════════════════════════════════════════════════════╗
║  КЛИЕНТ СИСТЕМЫ УЧЁТА ОРГАНИЗАЦИЙ (ЛР №5)                 ║
║  Устойчивое консольное приложение с кэшированием и экспортом ║
╚════════════════════════════════════════════════════════════╝");
        Console.ResetColor();
    }

    static void PrintMenu()
    {
        Console.WriteLine(@"
МЕНЮ:
   1 — Показать список организаций
   2 — Экспорт в CSV
   3 — Экспорт в Excel (.xlsx)
   4 — Экспорт в PDF
   5 — Очистить кэш
   6 — Поиск организаций
   H — Показать это меню
   Q — Выход

Подсказка: Нажмите Ctrl+C для отмены текущей операции");
    }
}

// ============================================================================
// Вспомогательные классы (в том же файле для простоты лабораторной работы)
// ============================================================================

/// <summary>
/// Цветной прогресс-бар для консоли
/// </summary>
class ColoredProgress : IProgress<int>
{
    private int _lastReported = -1;

    public void Report(int percentage)
    {
        if (percentage == _lastReported) return;

        const int barWidth = 50;
        var filled = (int)(barWidth * percentage / 100.0);
        var empty = barWidth - filled;

        Console.Write("\rПрогресс: ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(new string('█', filled));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('░', empty));
        Console.ResetColor();
        Console.Write($" {percentage,3}%");

        if (percentage == 100)
        {
            Console.WriteLine(" Завершено");
        }

        _lastReported = percentage;
    }
}

/// <summary>
/// Простой прогресс-бар без цветов (для систем без поддержки цветов)
/// </summary>
class SimpleProgress : IProgress<int>
{
    private int _last = -1;
    public void Report(int value)
    {
        if (value != _last)
        {
            Console.Write($"\rПрогресс: {value,3}% [{new string('█', value / 2)}{new string(' ', 50 - value / 2)}]");
            _last = value;
        }
        if (value == 100) Console.WriteLine(" Завершено");
    }
}