using Microsoft.Extensions.Caching.Memory;
using OrganizationAppClient.Models;

namespace OrganizationAppClient.Services;

public class CachedOrganizationService
{
    private readonly IOrganizationApiClient _apiClient;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public CachedOrganizationService(IOrganizationApiClient apiClient, IMemoryCache cache)
    {
        _apiClient = apiClient;
        _cache = cache;
        
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
            .SetSlidingExpiration(TimeSpan.FromMinutes(2))
            .SetSize(1);
    }

    public async Task<PagedResponse<OrganizationDto>> GetOrganizationsAsync(
        GetOrganizationsRequest request, CancellationToken ct = default)
    {
        var cacheKey = $"orgs_page_{request.Page}_size_{request.PageSize}_search_{request.Search}_type_{request.Type}";
        
        if (_cache.TryGetValue(cacheKey, out PagedResponse<OrganizationDto>? cached))
        {
            Console.WriteLine("✓ Данные получены из кэша");
            return cached!;
        }
        
        Console.WriteLine("→ Загрузка данных из API...");
        var organizations = await _apiClient.GetOrganizationsAsync(request, ct);
        
        _cache.Set(cacheKey, organizations, _cacheOptions);
        Console.WriteLine($"✓ Загружено {organizations.Items.Count} записей, сохранено в кэш");
        
        return organizations;
    }

    public void InvalidateCache(string pattern = "*")
    {
        // Простая реализация: очистка всего кэша
        // Для продакшена лучше использовать кастомный кэш с удалением по паттерну
        ((MemoryCache)_cache).Compact(1.0);
        Console.WriteLine("✓ Кэш очищен");
    }
}