using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Nocturne.Core.Contracts.Multitenancy;
using Nocturne.Infrastructure.Data;

namespace Nocturne.API.Multitenancy;

/// <summary>
/// Middleware that resolves the current tenant from the request.
/// In single-tenant mode (no BaseDomain), resolves the sole active tenant automatically.
/// In multi-tenant mode (BaseDomain set), resolves from subdomain.
/// Must run before AuthenticationMiddleware in the pipeline.
/// </summary>
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;
    private readonly MultitenancyConfiguration _config;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Cache key used for the sole tenant in single-tenant mode (no BaseDomain).
    /// </summary>
    private const string SingleTenantCacheKey = "tenant:__single__";

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger,
        IOptions<MultitenancyConfiguration> config,
        IMemoryCache cache)
    {
        _next = next;
        _logger = logger;
        _config = config.Value;
        _cache = cache;
    }

    /// <summary>
    /// Paths that operate across all tenants and don't require a resolved tenant context.
    /// These are allowed through even when no matching tenant is found.
    /// </summary>
    private static readonly string[] TenantlessAllowedPaths =
    [
        "/api/v4/me/tenants/validate-slug",
        "/api/admin/tenants/validate-slug",
        "/api/v4/admin/tenants/validate-slug",
        "/api/metadata",
        "/api/v4/chat-identity/directory/resolve",
        "/api/v4/chat-identity/directory/pending-links",
    ];

    /// <summary>
    /// Prefixes that are cross-tenant by design and must never be gated on
    /// a resolved tenant. Admin tenant management (create, provision, member
    /// management) operates on arbitrary tenants by ID and cannot rely on
    /// subdomain resolution.
    /// </summary>
    private static readonly string[] TenantlessAllowedPrefixes =
    [
        "/api/admin/tenants",
        "/api/v4/admin/tenants",
        "/api/v4/platform/",
        "/api/v4/setup/",
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var tenantAccessor = context.RequestServices.GetRequiredService<ITenantAccessor>();
        // Check X-Forwarded-Host first (set by reverse proxies), then fall back to Host
        var host = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault()?.Split(':')[0]
                   ?? context.Request.Host.Host;
        var slug = ExtractSubdomain(host);
        var path = context.Request.Path.Value ?? "";
        var isTenantlessAllowedPath =
            TenantlessAllowedPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)) ||
            TenantlessAllowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        var isSingleTenantMode = string.IsNullOrEmpty(_config.BaseDomain);

        if (!isSingleTenantMode)
        {
            // Multi-tenant mode: BaseDomain is set

            // Tenantless-allowed paths on the apex (no slug) operate across tenants.
            if (slug == null && isTenantlessAllowedPath)
            {
                await _next(context);
                return;
            }

            // Apex domain (no subdomain) with a non-tenantless path: 404
            if (slug == null)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            // Subdomain present: resolve tenant by slug
            var tenantContext = await ResolveTenantBySlugAsync(context.RequestServices, slug);

            if (tenantContext == null)
            {
                if (isTenantlessAllowedPath)
                {
                    await _next(context);
                    return;
                }

                _logger.LogWarning("Tenant not found for slug '{Slug}'", slug);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            if (!tenantContext.IsActive)
            {
                _logger.LogWarning("Tenant '{Slug}' is inactive", slug);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            tenantAccessor.SetTenant(tenantContext);
            context.Items["TenantContext"] = tenantContext;

            await _next(context);
        }
        else
        {
            // Single-tenant mode: no BaseDomain configured
            var tenantContext = await ResolveSingleTenantAsync(context.RequestServices);

            if (tenantContext == null)
            {
                // No tenant found (zero tenants exist) — allow tenantless paths, 503 others
                if (isTenantlessAllowedPath)
                {
                    await _next(context);
                    return;
                }

                _logger.LogInformation("No tenants exist — returning 503 setup_required");
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "setup_required",
                    setupRequired = true,
                });
                return;
            }

            if (!tenantContext.IsActive)
            {
                _logger.LogWarning("Single tenant '{Slug}' is inactive", tenantContext.Slug);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            tenantAccessor.SetTenant(tenantContext);
            context.Items["TenantContext"] = tenantContext;

            await _next(context);
        }
    }

    private string? ExtractSubdomain(string hostname)
    {
        if (string.IsNullOrEmpty(_config.BaseDomain))
            return null;

        // Strip port from BaseDomain for hostname comparison
        // (Host.Host already excludes port, but BaseDomain may include it for frontend URL construction)
        var baseDomainHost = _config.BaseDomain.Split(':')[0];

        if (!hostname.EndsWith($".{baseDomainHost}", StringComparison.OrdinalIgnoreCase))
            return null;

        var subdomain = hostname[..^(baseDomainHost.Length + 1)];
        return string.IsNullOrEmpty(subdomain) ? null : subdomain;
    }

    /// <summary>
    /// Resolves a tenant by subdomain slug (multi-tenant mode).
    /// </summary>
    private async Task<TenantContext?> ResolveTenantBySlugAsync(IServiceProvider services, string slug)
    {
        var cacheKey = $"tenant:{slug}";

        if (_cache.TryGetValue(cacheKey, out TenantContext? cached))
            return cached;

        var factory = services.GetRequiredService<IDbContextFactory<NocturneDbContext>>();
        await using var context = await factory.CreateDbContextAsync();

        var tenant = await context.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug);

        if (tenant == null)
            return null;

        var tenantContext = new TenantContext(tenant.Id, tenant.Slug, tenant.DisplayName, tenant.IsActive);
        _cache.Set(cacheKey, tenantContext, CacheDuration);
        return tenantContext;
    }

    /// <summary>
    /// Resolves the single active tenant in single-tenant mode (no BaseDomain).
    /// Returns null if zero or more than one active tenant exists.
    /// </summary>
    private async Task<TenantContext?> ResolveSingleTenantAsync(IServiceProvider services)
    {
        if (_cache.TryGetValue(SingleTenantCacheKey, out TenantContext? cached))
            return cached;

        var factory = services.GetRequiredService<IDbContextFactory<NocturneDbContext>>();
        await using var context = await factory.CreateDbContextAsync();

        var tenants = await context.Tenants.AsNoTracking()
            .Where(t => t.IsActive)
            .Take(2)
            .ToListAsync();

        if (tenants.Count != 1)
            return null;

        var tenant = tenants[0];
        var tenantContext = new TenantContext(tenant.Id, tenant.Slug, tenant.DisplayName, tenant.IsActive);
        _cache.Set(SingleTenantCacheKey, tenantContext, CacheDuration);
        return tenantContext;
    }
}
