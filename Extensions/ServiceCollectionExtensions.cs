using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using OrganizationAppClient.Services; 
using System.Net;

namespace OrganizationAppClient.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationClient(this IServiceCollection services, string baseUrl)
    {
        // 1️⃣ Регистрируем MemoryCache
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100;
            options.CompactionPercentage = 0.25;
        });

        // 2️⃣ Регистрируем типизированный HTTP-клиент с политиками Polly
        services.AddHttpClient<IOrganizationApiClient, OrganizationApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());

        // 3️⃣ 🔥 ДОБАВЛЕНО: Регистрация сервисов и экспортеров в DI
        services.AddScoped<CachedOrganizationService>();
        services.AddScoped<CsvExporter>();
        services.AddScoped<ExcelExporter>();
        services.AddScoped<PdfExporter>();

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt => 
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outcome, timespan, retryCount, context) =>
                {
                    Console.WriteLine($"↻ Попытка {retryCount} через {timespan.TotalSeconds}с...");
                });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) =>
                {
                    Console.WriteLine($"⚡ Цепь разорвана на {breakDelay.TotalSeconds}с");
                },
                onReset: () => Console.WriteLine("✓ Цепь восстановлена"),
                onHalfOpen: () => Console.WriteLine("↻ Проверка доступности сервиса..."));
    }
}