namespace Nocturne.API.Models.DevOnly;

/// <summary>
/// Top-level snapshot of all tenants and their associated identity/config data.
/// Used for dev-only export/import of non-clinical setup state.
/// </summary>
public class DevSnapshotDto
{
    public DateTime ExportedAt { get; set; }
    public List<TenantSnapshotDto> Tenants { get; set; } = [];
}

public class TenantSnapshotDto
{
    public TenantEntityDto Tenant { get; set; } = null!;
    public List<SubjectEntityDto> Subjects { get; set; } = [];
    public List<PasskeyCredentialEntityDto> PasskeyCredentials { get; set; } = [];
    public List<TenantRoleEntityDto> Roles { get; set; } = [];
    public List<TenantMemberEntityDto> Members { get; set; } = [];
    public List<TenantMemberRoleEntityDto> MemberRoles { get; set; } = [];
    public List<OAuthClientEntityDto> OAuthClients { get; set; } = [];
    public List<ConnectorConfigSnapshotDto> ConnectorConfigurations { get; set; } = [];
}

// ── Tenant ──────────────────────────────────────────────────────────────

public class TenantEntityDto
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ApiSecretHash { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastReadingAt { get; set; }
    public string Timezone { get; set; } = "UTC";
    public string? SubjectName { get; set; }
    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }
    public bool QuietHoursOverrideCritical { get; set; }
    public bool AllowAccessRequests { get; set; }
    public DateTime SysCreatedAt { get; set; }
    public DateTime SysUpdatedAt { get; set; }
}

// ── Subject ─────────────────────────────────────────────────────────────

public class SubjectEntityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? AccessTokenHash { get; set; }
    public string? AccessTokenPrefix { get; set; }
    public string? Email { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemSubject { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? OriginalId { get; set; }
    public string? PreferredLanguage { get; set; }
    public string ApprovalStatus { get; set; } = "Approved";
    public string? AccessRequestMessage { get; set; }
    public bool IsPlatformAdmin { get; set; }
}

// ── PasskeyCredential (byte[] → base64 string) ─────────────────────────

public class PasskeyCredentialEntityDto
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public string CredentialId { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public uint SignCount { get; set; }
    public List<string> Transports { get; set; } = [];
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public Guid? AaGuid { get; set; }
}

// ── TenantRole ──────────────────────────────────────────────────────────

public class TenantRoleEntityDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = [];
    public bool IsSystem { get; set; }
    public DateTime SysCreatedAt { get; set; }
    public DateTime SysUpdatedAt { get; set; }
}

// ── TenantMember ────────────────────────────────────────────────────────

public class TenantMemberEntityDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTime SysCreatedAt { get; set; }
    public DateTime SysUpdatedAt { get; set; }
    public List<string>? DirectPermissions { get; set; }
    public string? Label { get; set; }
    public bool LimitTo24Hours { get; set; }
    public Guid? CreatedFromInviteId { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public string? LastUsedIp { get; set; }
    public string? LastUsedUserAgent { get; set; }
    public DateTime? RevokedAt { get; set; }
}

// ── TenantMemberRole ────────────────────────────────────────────────────

public class TenantMemberRoleEntityDto
{
    public Guid Id { get; set; }
    public Guid TenantMemberId { get; set; }
    public Guid TenantRoleId { get; set; }
    public DateTime SysCreatedAt { get; set; }
}

// ── OAuthClient ─────────────────────────────────────────────────────────

public class OAuthClientEntityDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? SoftwareId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientUri { get; set; }
    public string? LogoUri { get; set; }
    public string? CreatedFromIp { get; set; }
    public string? DisplayName { get; set; }
    public bool IsKnown { get; set; }
    public string RedirectUris { get; set; } = "[]";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// ── ConnectorConfiguration (secrets decrypted on export) ────────────────

public class ConnectorConfigSnapshotDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ConnectorName { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = "{}";
    public Dictionary<string, string>? SecretsPlaintext { get; set; }
    public int SchemaVersion { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime SysCreatedAt { get; set; }
    public DateTime SysUpdatedAt { get; set; }
    public DateTime? LastSyncAttempt { get; set; }
    public DateTime? LastSuccessfulSync { get; set; }
    public string? LastErrorMessage { get; set; }
    public DateTime? LastErrorAt { get; set; }
    public bool IsHealthy { get; set; }
}
