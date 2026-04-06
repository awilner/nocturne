namespace Nocturne.API.Models.OAuth;

/// <summary>
/// DTO representing an OAuth grant for the management UI
/// </summary>
public class OAuthGrantDto
{
    public Guid Id { get; set; }
    public string GrantType { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? ClientDisplayName { get; set; }
    public bool IsKnownClient { get; set; }
    public List<string> Scopes { get; set; } = new();
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedUserAgent { get; set; }
}

/// <summary>
/// Response containing a list of OAuth grants
/// </summary>
public class OAuthGrantListResponse
{
    public List<OAuthGrantDto> Grants { get; set; } = new();
}

/// <summary>
/// Request to update an existing grant's label and/or scopes
/// </summary>
public class UpdateGrantRequest
{
    public string? Label { get; set; }
    public List<string>? Scopes { get; set; }
}
