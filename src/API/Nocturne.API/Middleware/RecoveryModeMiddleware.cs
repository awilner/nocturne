using Microsoft.Extensions.Options;
using Nocturne.API.Authorization;
using Nocturne.API.Multitenancy;
using Nocturne.API.Services.Auth;

namespace Nocturne.API.Middleware;

/// <summary>
/// Middleware that enforces recovery mode restrictions when active.
/// In multi-tenant mode, this middleware is a no-op -- per-tenant recovery
/// is handled by <see cref="TenantSetupMiddleware"/> (which runs after tenant resolution).
/// In single-tenant mode, blocks API traffic when the global <see cref="RecoveryModeState"/>
/// is active, allowing only passkey/TOTP setup endpoints through.
/// The <c>NOCTURNE_RECOVERY_MODE</c> env var override bypasses the multi-tenant skip.
/// </summary>
/// <remarks>
/// <para>
/// Pipeline order (position 2 of 8 custom middleware):
/// <see cref="JsonExtensionMiddleware"/>, <b>RecoveryModeMiddleware</b>,
/// <see cref="OidcCallbackRedirectMiddleware"/>, <see cref="Multitenancy.TenantResolutionMiddleware"/>,
/// <see cref="TenantSetupMiddleware"/>, <see cref="AuthenticationMiddleware"/>,
/// <see cref="MemberScopeMiddleware"/>, <see cref="SiteSecurityMiddleware"/>.
/// </para>
/// <para>
/// Endpoints decorated with <see cref="AllowDuringSetupAttribute"/> bypass the recovery gate.
/// Uses <see cref="Multitenancy.MultitenancyConfiguration"/> to determine single- vs multi-tenant mode.
/// </para>
/// </remarks>
/// <seealso cref="AllowDuringSetupAttribute"/>
/// <seealso cref="TenantSetupMiddleware"/>
/// <seealso cref="RecoveryModeState"/>
public class RecoveryModeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RecoveryModeMiddleware> _logger;

    /// <summary>
    /// Creates a new instance of <see cref="RecoveryModeMiddleware"/>.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger for recovery/setup mode diagnostics.</param>
    public RecoveryModeMiddleware(
        RequestDelegate next,
        ILogger<RecoveryModeMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether recovery mode or initial setup is active and blocks API traffic accordingly.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="state">The global <see cref="RecoveryModeState"/> singleton.</param>
    /// <param name="multitenancyConfig">Multitenancy configuration for determining single- vs multi-tenant mode.</param>
    /// <returns>A task that completes when the middleware has finished processing.</returns>
    public async Task InvokeAsync(
        HttpContext context,
        RecoveryModeState state,
        IOptions<MultitenancyConfiguration> multitenancyConfig)
    {
        if (!state.IsEnabled && !state.IsSetupRequired)
        {
            await _next(context);
            return;
        }

        // In multi-tenant mode, per-tenant recovery is handled by TenantSetupMiddleware.
        // Only the env var override still triggers the global gate.
        var isMultiTenant = !string.IsNullOrEmpty(multitenancyConfig.Value.BaseDomain);
        var envOverride = string.Equals(
            Environment.GetEnvironmentVariable("NOCTURNE_RECOVERY_MODE"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (isMultiTenant && !envOverride)
        {
            await _next(context);
            return;
        }

        // Endpoints marked [AllowDuringSetup] are the bootstrap endpoints and
        // should always be reachable during recovery mode.
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<AllowDuringSetupAttribute>() is not null)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";

        // Block other API endpoints with a clear message
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            var mode = state.IsSetupRequired ? "setup" : "recovery";
            _logger.LogDebug("{Mode} mode: blocking request to {Path}", mode, path);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new
            {
                error = state.IsSetupRequired ? "setup_required" : "recovery_mode_active",
                message = state.IsSetupRequired
                    ? "Initial setup required. Please register a passkey or authenticator app."
                    : "Instance is in recovery mode. Please register a passkey or authenticator app to continue.",
                setupRequired = state.IsSetupRequired,
                recoveryMode = state.IsEnabled,
            });
            return;
        }

        // Allow non-API requests (frontend assets, health checks, etc.)
        await _next(context);
    }
}
