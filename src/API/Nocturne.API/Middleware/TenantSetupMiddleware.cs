using Microsoft.EntityFrameworkCore;
using Nocturne.Core.Contracts.Multitenancy;
using Nocturne.Infrastructure.Data;

namespace Nocturne.API.Middleware;

/// <summary>
/// Middleware that returns 503 setupRequired for freshly provisioned tenants
/// that have no passkey credentials yet. Allows passkey setup and metadata
/// endpoints through so the setup wizard can complete.
///
/// Only active in multi-tenant mode (runs after TenantResolutionMiddleware).
/// Single-tenant setup is handled by RecoveryModeMiddleware.
/// </summary>
public class TenantSetupMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantSetupMiddleware> _logger;

    private static readonly string[] AllowedPrefixes =
    [
        "/api/auth/passkey/",
        "/api/auth/totp/",
        "/api/metadata",
    ];

    private static readonly string[] AllowedPaths =
    [
        "/api/admin/tenants/validate-slug",
        "/api/v4/me/tenants/validate-slug",
    ];

    public TenantSetupMiddleware(
        RequestDelegate next,
        ILogger<TenantSetupMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantAccessor tenantAccessor,
        NocturneDbContext db)
    {
        // Only check when a tenant has been resolved
        if (!tenantAccessor.IsResolved)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";

        // Allow passkey, TOTP, metadata, and slug validation paths
        if (AllowedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)) ||
            AllowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Only block API paths
        if (!path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Check if this tenant has completed setup (has at least one passkey credential)
        // PasskeyCredentialEntity is ITenantScoped — query filter applies automatically
        var hasCredentials = await db.PasskeyCredentials.AnyAsync();
        if (hasCredentials)
        {
            await _next(context);
            return;
        }

        _logger.LogDebug(
            "Tenant {TenantId} has no passkey credentials — returning setup required",
            tenantAccessor.TenantId);

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "setup_required",
            message = "Initial setup required. Please register a passkey to secure your account.",
            setupRequired = true,
            recoveryMode = false,
        });
        return;
    }
}
