using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nocturne.API.Multitenancy;
using Nocturne.Infrastructure.Data;

namespace Nocturne.API.Services.Auth;

/// <summary>
/// One-time startup check: when BaseDomain is newly configured (transitioning from
/// single-tenant to multi-tenant mode), revoke all active refresh tokens so that
/// clients are forced to re-authenticate against the correct subdomain URL.
/// <para>
/// This is idempotent — on subsequent restarts the tokens are already revoked,
/// so the UPDATE is a no-op.
/// </para>
/// </summary>
public static class BaseDomainTransitionService
{
    public static async Task CheckAndRevokeAsync(IServiceProvider services)
    {
        var config = services.GetRequiredService<IOptions<MultitenancyConfiguration>>().Value;

        // Nothing to do if BaseDomain is not configured (single-tenant mode).
        if (string.IsNullOrEmpty(config.BaseDomain))
            return;

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<NocturneDbContext>>();

        // Revoke all non-revoked refresh tokens. When BaseDomain is set, all previously
        // issued tokens carry the wrong issuer/audience context. This is safe to run on
        // every startup because once tokens are revoked, the WHERE clause matches nothing.
        var revoked = await db.RefreshTokens
            .Where(t => t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow)
                .SetProperty(t => t.RevokedReason, "BaseDomain transition"));

        if (revoked > 0)
        {
            logger.LogWarning(
                "BaseDomain transition: revoked {Count} active refresh tokens to force re-authentication against subdomain URLs",
                revoked);
        }
    }
}
