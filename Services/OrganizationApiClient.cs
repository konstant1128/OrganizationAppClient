// Services/OrganizationApiClient.cs
using System.Net.Http.Json;
using OrganizationAppClient.Models;

namespace OrganizationAppClient.Services;

public class OrganizationApiClient : IOrganizationApiClient
{
    private readonly HttpClient _httpClient;

    public OrganizationApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PagedResponse<OrganizationDto>> GetOrganizationsAsync(
        GetOrganizationsRequest request, CancellationToken ct = default)
    {
        var query = $"?page={request.Page}&pageSize={request.PageSize}";
        if (!string.IsNullOrEmpty(request.Search))
            query += $"&inn={Uri.EscapeDataString(request.Search)}";
        if (!string.IsNullOrEmpty(request.Type))
            query += $"&type={Uri.EscapeDataString(request.Type)}";
            
        var response = await _httpClient.GetAsync($"/api/organizations{query}", ct);
        HandleErrorResponse(response);
        
        var result = await response.Content.ReadFromJsonAsync<PagedResponse<OrganizationDto>>(cancellationToken: ct);
        return result ?? new PagedResponse<OrganizationDto>(new(), 0, request.Page, request.PageSize);
    }

    public async Task<OrganizationDto?> GetOrganizationByIdAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.GetAsync($"/api/organizations/{id}", ct);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
            
        HandleErrorResponse(response);
        return await response.Content.ReadFromJsonAsync<OrganizationDto>(cancellationToken: ct);
    }

    public async Task<OrganizationDto> CreateOrganizationAsync(
        CreateOrganizationRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/organizations", request, ct);
        HandleErrorResponse(response);
        
        var result = await response.Content.ReadFromJsonAsync<OrganizationDto>(cancellationToken: ct);
        return result ?? throw new InvalidOperationException("Сервер не вернул созданную организацию");
    }

    public async Task<bool> DeleteOrganizationAsync(int id, CancellationToken ct = default)
    {
        var response = await _httpClient.DeleteAsync($"/api/organizations/{id}", ct);
        HandleErrorResponse(response);
        return response.IsSuccessStatusCode;
    }

    private void HandleErrorResponse(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            throw new ServiceUnavailableException((int)response.StatusCode);
            
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // Упрощённая обработка валидации
            throw new ValidationException(new Dictionary<string, string[]> 
            { 
                { "error", new[] { "Некорректные данные запроса" } } 
            });
        }
        
        if (!response.IsSuccessStatusCode)
            throw new NetworkException($"HTTP ошибка: {response.StatusCode}", inner: null);
    }
}