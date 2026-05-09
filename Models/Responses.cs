// Models/Responses.cs
using System.Text.Json.Serialization;

namespace OrganizationAppClient.Models;

public record PagedResponse<T>(List<T> Items, int TotalCount, int Page, int PageSize)
{
    [JsonIgnore]
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    [JsonIgnore]
    public bool HasNextPage => Page < TotalPages;
    [JsonIgnore]
    public bool HasPreviousPage => Page > 1;
}

public record OrganizationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
}