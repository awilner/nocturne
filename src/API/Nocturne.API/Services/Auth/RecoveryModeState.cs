namespace Nocturne.API.Services.Auth;

/// <summary>
/// Singleton that tracks instance authentication state as determined at startup by
/// <see cref="RecoveryModeCheckService"/>.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><see cref="IsSetupRequired"/>: no non-system subjects exist (fresh install).</item>
///   <item><see cref="IsEnabled"/>: orphaned subjects exist with no passkey or OIDC binding (post-upgrade).</item>
/// </list>
/// </remarks>
public class RecoveryModeState
{
    /// <summary>
    /// Gets or sets whether recovery mode is active.
    /// Set to <see langword="true"/> when active subjects exist with no authentication credentials.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets whether setup mode is active.
    /// Set to <see langword="true"/> when no non-system subjects exist (fresh database).
    /// </summary>
    public bool IsSetupRequired { get; set; }
}
