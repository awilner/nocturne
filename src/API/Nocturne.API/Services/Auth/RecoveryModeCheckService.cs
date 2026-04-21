using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Nocturne.API.Multitenancy;
using Nocturne.Infrastructure.Data;

namespace Nocturne.API.Services.Auth;

/// <summary>
/// Hosted service that runs on startup to determine whether the instance should enter recovery mode.
/// </summary>
/// <remarks>
/// Recovery mode is triggered when:
/// <list type="bullet">
///   <item>The <c>NOCTURNE_RECOVERY_MODE</c> environment variable is set to <c>true</c>, or</item>
///   <item>Any active non-system subject has neither a passkey credential nor an OIDC binding.</item>
/// </list>
/// In single-tenant mode this also sets <see cref="RecoveryModeState.IsSetupRequired"/> when no
/// non-system subjects exist (fresh database). In multi-tenant mode, per-tenant setup is handled by
/// <c>TenantSetupMiddleware</c> instead, so the global setup flag is not used.
/// </remarks>
/// <seealso cref="RecoveryModeState"/>
public class RecoveryModeCheckService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly RecoveryModeState _state;
    private readonly MultitenancyConfiguration _multitenancyConfig;
    private readonly ILogger<RecoveryModeCheckService> _logger;

    /// <summary>
    /// Initialises a new <see cref="RecoveryModeCheckService"/>.
    /// </summary>
    /// <param name="serviceProvider">Root service provider; a new scope is created for the startup check.</param>
    /// <param name="state">Singleton <see cref="RecoveryModeState"/> that is mutated based on the check outcome.</param>
    /// <param name="multitenancyConfig">Multitenancy configuration used to determine single- vs multi-tenant mode.</param>
    /// <param name="logger">Logger instance.</param>
    public RecoveryModeCheckService(
        IServiceProvider serviceProvider,
        RecoveryModeState state,
        IOptions<MultitenancyConfiguration> multitenancyConfig,
        ILogger<RecoveryModeCheckService> logger
    )
    {
        _serviceProvider = serviceProvider;
        _state = state;
        _multitenancyConfig = multitenancyConfig.Value;
        _logger = logger;
    }

    private bool IsMultiTenantMode => !string.IsNullOrEmpty(_multitenancyConfig.BaseDomain);

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Check environment variable override first
        var envOverride = Environment.GetEnvironmentVariable("NOCTURNE_RECOVERY_MODE");
        if (string.Equals(envOverride, "true", StringComparison.OrdinalIgnoreCase))
        {
            _state.IsEnabled = true;
            _logger.LogWarning(
                "Recovery mode enabled via NOCTURNE_RECOVERY_MODE environment variable"
            );
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NocturneDbContext>();

            // Use IgnoreQueryFilters to bypass tenant scoping — this is a
            // global startup check that must inspect all subjects across tenants.
            var activeSubjects = await db.Subjects
                .IgnoreQueryFilters()
                .Where(s => s.IsActive && !s.IsSystemSubject)
                .AnyAsync(cancellationToken);

            if (!activeSubjects)
            {
                if (IsMultiTenantMode)
                {
                    // In multi-tenant mode, an empty database is the expected initial
                    // state. Per-tenant setup is handled by TenantSetupMiddleware, so
                    // we don't set the global setup flag which would block all traffic.
                    _logger.LogInformation(
                        "No user subjects found (fresh database) — multi-tenant mode, " +
                        "per-tenant setup will be handled by TenantSetupMiddleware"
                    );
                }
                else
                {
                    _state.IsSetupRequired = true;
                    _logger.LogWarning(
                        "Setup mode enabled: no user subjects found (fresh database)"
                    );
                }
                return;
            }

            if (IsMultiTenantMode)
            {
                _logger.LogInformation(
                    "Multi-tenant mode — per-tenant recovery handled by TenantSetupMiddleware"
                );
                return;
            }

            var hasOrphaned = await db.Subjects
                .IgnoreQueryFilters()
                .Where(s => s.IsActive && !s.IsSystemSubject)
                .Where(s =>
                    !db.SubjectOidcIdentities
                        .IgnoreQueryFilters()
                        .Any(i => i.SubjectId == s.Id) &&
                    !db.PasskeyCredentials
                        .IgnoreQueryFilters()
                        .Any(p => p.SubjectId == s.Id)
                )
                .AnyAsync(cancellationToken);

            if (hasOrphaned)
            {
                _state.IsEnabled = true;
                _logger.LogWarning(
                    "Recovery mode enabled: one or more active subjects have no passkey and no OIDC binding"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for orphaned subjects during startup");
        }
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
