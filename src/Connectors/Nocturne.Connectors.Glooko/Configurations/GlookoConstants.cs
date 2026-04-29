namespace Nocturne.Connectors.Glooko.Configurations;

/// <summary>
///     Constants specific to Glooko connector
/// </summary>
public static class GlookoConstants
{
    /// <summary>
    ///     Known Glooko servers
    /// </summary>
    public static class Servers
    {
        public const string Eu = "eu.api.glooko.com";
        public const string Us = "api.glooko.com";
    }

    /// <summary>
    ///     Configuration specific to Glooko
    /// </summary>
    public static class Configuration
    {
        public const string DefaultServer = Servers.Eu;
    }

    /// <summary>
    ///     Resolves the API base URL from the server region in the config.
    ///     Must be called at request time (not DI time) so per-tenant DB overrides are respected.
    /// </summary>
    public static string ResolveBaseUrl(string? server) => server?.Trim().ToUpperInvariant() switch
    {
        "EU" => $"https://{Servers.Eu}",
        "US" => $"https://{Servers.Us}",
        _ => $"https://{Configuration.DefaultServer}"
    };

    /// <summary>
    ///     Resolves the web origin URL for Referer/Origin headers based on server region.
    /// </summary>
    public static string ResolveWebOrigin(string? server) => server?.Trim().ToUpperInvariant() switch
    {
        "US" => "https://my.glooko.com",
        _ => "https://eu.my.glooko.com"
    };
}
