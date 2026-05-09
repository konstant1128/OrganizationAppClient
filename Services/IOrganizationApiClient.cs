// Services/IOrganizationApiClient.cs
using OrganizationAppClient.Models;

namespace OrganizationAppClient.Services;

public interface IOrganizationApiClient
{
    Task<PagedResponse<OrganizationDto>> GetOrganizationsAsync(
        GetOrganizationsRequest request, CancellationToken ct = default);
    
    Task<OrganizationDto?> GetOrganizationByIdAsync(int id, CancellationToken ct = default);
    
    Task<OrganizationDto> CreateOrganizationAsync(
        CreateOrganizationRequest request, CancellationToken ct = default);
    
    Task<bool> DeleteOrganizationAsync(int id, CancellationToken ct = default);
}