using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenApi.Remote.Attributes;
using Nocturne.API.Authorization;
using Nocturne.Core.Contracts;
using Nocturne.Core.Contracts.Multitenancy;
using Nocturne.Core.Models;
using Nocturne.Core.Models.Configuration;
using Nocturne.API.Services.Auth;
using Nocturne.Infrastructure.Data;
using Nocturne.Infrastructure.Data.Entities;
using SameSiteMode = Nocturne.Core.Models.Configuration.SameSiteMode;

namespace Nocturne.API.Controllers.V4;

/// <summary>
/// Two-step setup endpoints for bootstrapping a fresh Nocturne install.
/// These operate without a resolved tenant and without authentication.
/// Step 1: Create the first tenant (POST /api/v4/setup/tenant).
/// Step 2: Create the owner account for that tenant (POST /api/v4/setup/owner/*).
/// </summary>
[ApiController]
[Route("api/v4/setup")]
[Produces("application/json")]
[AllowAnonymous]
[AllowDuringSetup]
public class SetupController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IPasskeyService _passkeyService;
    private readonly IRecoveryCodeService _recoveryCodeService;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ISubjectService _subjectService;
    private readonly IDbContextFactory<NocturneDbContext> _dbFactory;
    private readonly OidcOptions _oidcOptions;
    private readonly ILogger<SetupController> _logger;

    public SetupController(
        ITenantService tenantService,
        IPasskeyService passkeyService,
        IRecoveryCodeService recoveryCodeService,
        IJwtService jwtService,
        IRefreshTokenService refreshTokenService,
        ISubjectService subjectService,
        IDbContextFactory<NocturneDbContext> dbFactory,
        IOptions<OidcOptions> oidcOptions,
        ILogger<SetupController> logger)
    {
        _tenantService = tenantService;
        _passkeyService = passkeyService;
        _recoveryCodeService = recoveryCodeService;
        _jwtService = jwtService;
        _refreshTokenService = refreshTokenService;
        _subjectService = subjectService;
        _dbFactory = dbFactory;
        _oidcOptions = oidcOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Create the first tenant on a fresh install. Only succeeds when zero tenants exist.
    /// </summary>
    [HttpPost("tenant")]
    [RemoteCommand]
    [ProducesResponseType(typeof(SetupTenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTenant(
        [FromBody] SetupTenantRequest request, CancellationToken ct)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var tenantCount = await context.Tenants.CountAsync(ct);
        if (tenantCount > 0)
            return Conflict(new { error = "setup_already_complete" });

        if (string.IsNullOrWhiteSpace(request.Slug) || string.IsNullOrWhiteSpace(request.DisplayName))
            return Problem(detail: "Slug and display name are required", statusCode: 400, title: "Bad Request");

        var validation = await _tenantService.ValidateSlugAsync(request.Slug, ct);
        if (!validation.IsValid)
            return Problem(detail: validation.Message, statusCode: 400, title: "Bad Request");

        var result = await _tenantService.CreateWithoutOwnerAsync(
            request.Slug, request.DisplayName, ct: ct);

        return Ok(new SetupTenantResponse(result.Id, result.ApiSecret));
    }

    /// <summary>
    /// Generate passkey registration options for the first owner account.
    /// Guard: exactly one tenant must exist with zero non-system members.
    /// </summary>
    [HttpPost("owner/options")]
    [RemoteCommand]
    [ProducesResponseType(typeof(SetupOwnerOptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OwnerOptions(
        [FromBody] SetupOwnerOptionsRequest request, CancellationToken ct)
    {
        var (tenant, error) = await GetSoleTenantWithoutOwnerAsync(ct);
        if (error != null)
            return error;

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.DisplayName))
            return Problem(detail: "Username and display name are required", statusCode: 400, title: "Bad Request");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Set RLS context so we can query tenant-scoped tables
        await context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_tenant_id', {0}, false)",
            tenant!.Id.ToString());

        // Idempotent: reuse existing setup subject if the WebAuthn ceremony
        // failed on a previous attempt
        var existingSubject = await context.Subjects
            .FirstOrDefaultAsync(s => !s.IsSystemSubject && s.IsActive, ct);

        Guid subjectId;
        if (existingSubject != null)
        {
            subjectId = existingSubject.Id;
            existingSubject.Name = request.DisplayName.Trim();
            existingSubject.Username = request.Username.Trim().ToLowerInvariant();
            await context.SaveChangesAsync(ct);
        }
        else
        {
            subjectId = Guid.CreateVersion7();
            context.Subjects.Add(new SubjectEntity
            {
                Id = subjectId,
                Name = request.DisplayName.Trim(),
                Username = request.Username.Trim().ToLowerInvariant(),
                IsActive = true,
                IsSystemSubject = false,
            });
            await context.SaveChangesAsync(ct);

            // Add as owner of the tenant
            var ownerRole = await context.TenantRoles
                .FirstOrDefaultAsync(r => r.TenantId == tenant!.Id && r.Slug == "owner", ct);

            if (ownerRole != null)
                await _tenantService.AddMemberAsync(tenant!.Id, subjectId, [ownerRole.Id], ct: ct);

            // Assign admin role
            await _subjectService.AssignRoleAsync(subjectId, "admin");

            _logger.LogInformation(
                "Setup: created first owner {SubjectId} ({Username}) for tenant {TenantId}",
                subjectId, request.Username.Trim(), tenant!.Id);
        }

        var result = await _passkeyService.GenerateRegistrationOptionsAsync(
            subjectId, request.Username.Trim(), tenant!.Id);

        return Ok(new SetupOwnerOptionsResponse
        {
            Options = result.OptionsJson,
            ChallengeToken = result.ChallengeToken,
            TenantId = tenant!.Id,
        });
    }

    /// <summary>
    /// Complete passkey registration for the first owner account.
    /// Verifies attestation, generates recovery codes, issues a full JWT session.
    /// </summary>
    [HttpPost("owner/complete")]
    [RemoteCommand]
    [ProducesResponseType(typeof(SetupOwnerCompleteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OwnerComplete(
        [FromBody] SetupOwnerCompleteRequest request, CancellationToken ct)
    {
        var (tenant, error) = await GetSoleTenantWithoutOwnerAsync(ct);
        if (error != null)
            return error;

        if (string.IsNullOrEmpty(request.ChallengeToken))
            return Problem(detail: "Challenge token is required", statusCode: 400, title: "Bad Request");

        try
        {
            var credResult = await _passkeyService.CompleteRegistrationAsync(
                request.AttestationResponseJson, request.ChallengeToken, tenant!.Id);

            // Generate recovery codes
            var recoveryCodes = await _recoveryCodeService.GenerateCodesAsync(credResult.SubjectId);

            // Get subject details for token generation
            var subject = await _subjectService.GetSubjectByIdAsync(credResult.SubjectId);
            if (subject == null)
                return Problem(detail: "Created subject not found", statusCode: 500, title: "Server Error");

            var roles = await _subjectService.GetSubjectRolesAsync(credResult.SubjectId);
            var permissions = await _subjectService.GetSubjectPermissionsAsync(credResult.SubjectId);

            // Issue session
            var subjectInfo = new SubjectInfo
            {
                Id = subject.Id,
                Name = subject.Name,
                Email = subject.Email,
            };

            var accessToken = _jwtService.GenerateAccessToken(subjectInfo, permissions, roles);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(
                credResult.SubjectId,
                oidcSessionId: null,
                deviceDescription: "Setup Passkey",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers.UserAgent.ToString());

            SetSessionCookies(accessToken, refreshToken);

            _logger.LogInformation(
                "Setup complete: first owner {SubjectId} registered with passkey for tenant {TenantId}",
                credResult.SubjectId, tenant!.Id);

            return Ok(new SetupOwnerCompleteResponse
            {
                Success = true,
                RecoveryCodes = recoveryCodes,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = (int)_jwtService.GetAccessTokenLifetime().TotalSeconds,
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Setup owner passkey registration failed");
            return Problem(detail: "Passkey registration failed during setup", statusCode: 400, title: "Registration Failed");
        }
    }

    #region Private Helpers

    /// <summary>
    /// Returns the sole tenant if exactly one exists and it has no non-system members,
    /// or an error result if the preconditions are not met.
    /// </summary>
    private async Task<(TenantEntity? Tenant, IActionResult? Error)> GetSoleTenantWithoutOwnerAsync(CancellationToken ct)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var tenants = await context.Tenants.Take(2).ToListAsync(ct);

        if (tenants.Count == 0)
            return (null, Conflict(new { error = "no_tenant_exists" }));

        if (tenants.Count > 1)
            return (null, Conflict(new { error = "setup_already_complete" }));

        var tenant = tenants[0];

        // Set RLS context to query tenant-scoped members table
        await context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_tenant_id', {0}, false)",
            tenant.Id.ToString());

        var hasNonSystemMembers = await context.TenantMembers
            .Where(tm => tm.TenantId == tenant.Id)
            .Join(
                context.Subjects.Where(s => !s.IsSystemSubject),
                tm => tm.SubjectId,
                s => s.Id,
                (tm, s) => tm)
            .AnyAsync(ct);

        if (hasNonSystemMembers)
            return (null, Conflict(new { error = "owner_already_exists" }));

        return (tenant, null);
    }

    private void SetSessionCookies(string accessToken, string refreshToken)
    {
        var cookieSameSite = _oidcOptions.Cookie.SameSite switch
        {
            SameSiteMode.Strict => Microsoft.AspNetCore.Http.SameSiteMode.Strict,
            SameSiteMode.Lax => Microsoft.AspNetCore.Http.SameSiteMode.Lax,
            SameSiteMode.None => Microsoft.AspNetCore.Http.SameSiteMode.None,
            _ => Microsoft.AspNetCore.Http.SameSiteMode.Lax,
        };

        Response.Cookies.Append(_oidcOptions.Cookie.AccessTokenName, accessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = _oidcOptions.Cookie.Secure,
            SameSite = cookieSameSite,
            Path = "/",
            IsEssential = true,
            MaxAge = _jwtService.GetAccessTokenLifetime(),
        });

        Response.Cookies.Append(_oidcOptions.Cookie.RefreshTokenName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = _oidcOptions.Cookie.Secure,
            SameSite = cookieSameSite,
            Path = "/",
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(7),
        });

        Response.Cookies.Append("IsAuthenticated", "true", new CookieOptions
        {
            HttpOnly = false,
            Secure = _oidcOptions.Cookie.Secure,
            SameSite = cookieSameSite,
            Path = "/",
            MaxAge = TimeSpan.FromDays(7),
        });
    }

    #endregion
}

#region Request/Response DTOs

public record SetupTenantRequest(string Slug, string DisplayName);

public record SetupTenantResponse(Guid TenantId, string ApiSecret);

public class SetupOwnerOptionsRequest
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class SetupOwnerOptionsResponse
{
    public string Options { get; set; } = string.Empty;
    public string ChallengeToken { get; set; } = string.Empty;
    public Guid TenantId { get; set; }
}

public class SetupOwnerCompleteRequest
{
    public string AttestationResponseJson { get; set; } = string.Empty;
    public string ChallengeToken { get; set; } = string.Empty;
}

public class SetupOwnerCompleteResponse
{
    public bool Success { get; set; }
    public List<string> RecoveryCodes { get; set; } = new();
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}

#endregion
