namespace Nocturne.Core.Models.Configuration;

/// <summary>
/// Operator-defined OIDC provider configuration from appsettings.json.
/// When one or more providers are defined in Oidc:Providers[], the database
/// is bypassed entirely and the management UI is hidden.
///
/// ClientSecret should be supplied via environment variables or a secrets manager,
/// not committed to appsettings.json.
/// </summary>
public class OidcProviderConfig
{
    public string Name { get; set; } = string.Empty;
    public string IssuerUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public List<string> Scopes { get; set; } = ["openid", "profile", "email"];
    public List<string> DefaultRoles { get; set; } = ["readable"];
    public bool IsEnabled { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    /// <summary>
    /// Known slugs: "google", "apple", "microsoft", "github".
    /// Any other value is treated as a URL.
    /// </summary>
    public string? Icon { get; set; }
    public string? ButtonColor { get; set; }
}
