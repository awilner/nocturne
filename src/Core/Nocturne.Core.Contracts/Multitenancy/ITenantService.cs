namespace Nocturne.Core.Contracts.Multitenancy;

public interface ITenantService
{
    /// <summary>Creates a new tenant with the specified subject as its owner.</summary>
    Task<TenantCreatedDto> CreateAsync(string slug, string displayName, Guid creatorSubjectId, string? apiSecret = null, CancellationToken ct = default);

    /// <summary>Creates a new tenant without assigning an owner.</summary>
    Task<TenantCreatedDto> CreateWithoutOwnerAsync(string slug, string displayName, string? apiSecret = null, CancellationToken ct = default);

    /// <summary>Re-seeds roles, public membership, and OAuth clients for an existing tenant after a data purge.</summary>
    Task SeedAfterResetAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Returns all tenants on the platform.</summary>
    Task<List<TenantDto>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns the tenant with its member list, or null if not found.</summary>
    Task<TenantDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Updates a tenant's display name, active state, and access-request policy.</summary>
    Task<TenantDto> UpdateAsync(Guid id, string displayName, bool isActive, bool? allowAccessRequests = null, CancellationToken ct = default);

    /// <summary>Permanently deletes a tenant and all associated data.</summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Adds a subject as a member of the specified tenant with the given roles and permissions.</summary>
    Task AddMemberAsync(Guid tenantId, Guid subjectId, List<Guid> roleIds, List<string>? directPermissions = null, string? label = null, bool limitTo24Hours = false, CancellationToken ct = default);

    /// <summary>Removes a subject's membership from the specified tenant.</summary>
    Task RemoveMemberAsync(Guid tenantId, Guid subjectId, CancellationToken ct = default);

    /// <summary>Returns all tenants that the specified subject is a member of.</summary>
    Task<List<TenantDto>> GetTenantsForSubjectAsync(Guid subjectId, CancellationToken ct = default);

    /// <summary>Checks whether a slug is valid and available for use.</summary>
    Task<SlugValidationResult> ValidateSlugAsync(string slug, CancellationToken ct = default);

    /// <summary>Replaces the tenant's API secret with the provided value.</summary>
    Task<string> UpdateApiSecretAsync(Guid tenantId, string newApiSecret, CancellationToken ct = default);

    /// <summary>Generates and stores a new random API secret for the tenant.</summary>
    Task<string> RegenerateApiSecretAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Returns whether the tenant has an API secret configured.</summary>
    Task<bool> HasApiSecretAsync(Guid tenantId, CancellationToken ct = default);

    /// <summary>Creates a new tenant and its owner subject in a single operation, using either a passkey credential or an OIDC identity.</summary>
    Task<ProvisionResult> ProvisionWithOwnerAsync(
        string slug, string displayName, string ownerUsername, string ownerEmail,
        ProvisionCredentialData? credential, ProvisionOidcIdentityData? oidcIdentity,
        CancellationToken ct = default);
}

public record TenantDto(Guid Id, string Slug, string DisplayName, bool IsActive, DateTime SysCreatedAt);

public record TenantCreatedDto(Guid Id, string Slug, string DisplayName, bool IsActive, DateTime SysCreatedAt, string ApiSecret);

public record TenantDetailDto(Guid Id, string Slug, string DisplayName, bool IsActive, DateTime SysCreatedAt, List<TenantMemberDto> Members);

public record TenantMemberDto(
    Guid Id,
    Guid SubjectId,
    string? Name,
    List<TenantMemberRoleDto> Roles,
    List<string>? DirectPermissions,
    string? Label,
    bool LimitTo24Hours,
    DateTime? LastUsedAt,
    DateTime SysCreatedAt);

public record TenantMemberRoleDto(Guid RoleId, string Name, string Slug);

public record SlugValidationResult(bool IsValid, string? Message = null);

public record ProvisionCredentialData(
    string CredentialId,
    string PublicKey,
    uint SignCount,
    List<string> Transports,
    Guid? AaGuid,
    Guid? SubjectId);

public record ProvisionOidcIdentityData(
    string Provider,
    string OidcSubjectId,
    string Issuer,
    string Email,
    Guid? SubjectId);

public record ProvisionResult(Guid TenantId, Guid SubjectId, string Slug);
