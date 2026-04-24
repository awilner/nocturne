namespace Nocturne.API.Multitenancy;

/// <summary>
/// Configuration for the multitenancy system.
/// Bound from appsettings.json section "Multitenancy".
/// </summary>
public class MultitenancyConfiguration
{
    public const string SectionName = "Multitenancy";

    /// <summary>
    /// Base domain for subdomain tenant resolution.
    /// e.g. "nocturnecgm.com" — requests to rhys.nocturnecgm.com resolve tenant "rhys".
    /// Aspire always injects this (either the custom domain or localhost:port).
    /// </summary>
    public string BaseDomain { get; set; } = "";

    /// <summary>
    /// Whether authenticated users can create their own tenants.
    /// SaaS operators set this to false to gate tenant creation behind billing.
    /// </summary>
    public bool AllowSelfServiceCreation { get; set; } = true;

    /// <summary>
    /// Optional webhook URL for custom slug validation.
    /// When configured, Nocturne POSTs { "slug": "xxx" } and expects { "isValid": bool, "message"?: string }.
    /// Used by SaaS operators to enforce custom naming rules or billing checks.
    /// </summary>
    public string? SlugValidationWebhookUrl { get; set; }

    /// <summary>
    /// Optional webhook URL for custom username validation.
    /// When configured, Nocturne POSTs { "username": "xxx" } and expects { "isValid": bool, "message"?: string }.
    /// Used by SaaS operators to enforce custom username rules (e.g. additional reserved words).
    /// </summary>
    public string? UsernameValidationWebhookUrl { get; set; }
}
