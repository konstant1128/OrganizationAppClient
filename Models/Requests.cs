// Models/Requests.cs
namespace OrganizationAppClient.Models;

public record CreateOrganizationRequest
{
    public string Name { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? ProductionType { get; set; }
    public string? ExtractionRegion { get; set; }
    public decimal? DailyProduction { get; set; }
}

public record UpdateOrganizationRequest
{
    public string? Name { get; set; }
    public string? Inn { get; set; }
    public string? LicenseNumber { get; set; }
    public string? ProductionType { get; set; }
    public string? ExtractionRegion { get; set; }
    public decimal? DailyProduction { get; set; }
}

public record GetOrganizationsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Search { get; set; }
    public string? Type { get; set; }
}