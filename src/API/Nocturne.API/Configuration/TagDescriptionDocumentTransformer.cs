using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Nocturne.API.Configuration;

/// <summary>
/// Adds human-readable descriptions to OpenAPI tags so Scalar displays an overview
/// for each controller group. Descriptions use GitHub-flavored markdown.
/// Embeds ER diagrams from the diagram manifest into matching tag descriptions.
/// </summary>
public sealed class TagDescriptionDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly Dictionary<string, List<DiagramRef>> _tagDiagrams;

    public TagDescriptionDocumentTransformer(IWebHostEnvironment env)
    {
        _tagDiagrams = BuildTagDiagramMap(env);
    }

    private static readonly Dictionary<string, string> Descriptions = new()
    {
        // ── Nocturne document (V4 + Auth) ────────────────────────────────

        ["Authentication"] = """
            Sign-in, token management, and multi-factor authentication.

            Covers five authentication mechanisms:

            - **OAuth 2.0** — Authorization Code + PKCE and Device Authorization Grant (RFC 8628). All clients are public; PKCE is mandatory — there are no client secrets.
            - **OIDC** — Federated login via external identity providers, callback handling, and session management.
            - **Passkeys** — WebAuthn/FIDO2 registration and login ceremonies (discoverable and non-discoverable credentials), plus recovery codes.
            - **TOTP** — Time-based one-time password setup, verification, and credential lifecycle.
            - **Direct Grants** — Programmatic API tokens (prefixed `noc_`) for headless / automation use cases. These bypass OAuth entirely.

            > **Footgun:** Direct grant tokens are long-lived and have no automatic expiry. Treat them like passwords.
            """,

        ["OIDC Discovery"] = """
            Standard `.well-known` endpoints that make Nocturne act as its own OAuth 2.0 / OIDC issuer.

            Returns the OpenID Provider configuration (`openid-configuration`) and JSON Web Key Set (`jwks.json`). These endpoints are **unauthenticated** by design — they must be publicly reachable for token validation.
            """,

        ["Analytics"] = """
            Pre-computed data for dashboards, charts, and retrospective analysis.

            Key endpoints:

            - **Chart Data** — Returns *everything* the glucose chart needs in a single call: readings, IOB/COB series, basal delivery, treatment markers, state spans, system events, and tracker markers. Prefer this over calling individual endpoints.
            - **Correlation** — Query across all V4 repositories by correlation ID to trace related records.
            - **Data Overview** — Year-level availability and day-level record counts for heatmap visualisation.
            - **Predictions** — Glucose forecasts from DeviceStatus sources (AAPS / Trio / Loop) or the OrefWasm engine.
            - **Retrospective** — Day-in-review snapshots combining IOB, COB, glucose, and basal at specific points in time.
            - **State Spans** — Time-ranged system states (pump modes, connectivity, overrides).
            - **Summary** — Widget-friendly data designed for mobile widgets, watch faces, and other constrained displays.
            - **Analytics** — Transparency controls for analytics collection — view, configure, and opt out.
            """,

        ["Connectors"] = """
            Configuration management for data source connectors (Dexcom, Glooko, Libre, etc.).

            > **Internal only.** These endpoints are intended for server-to-server use by connector services via mTLS. They are not designed for end-user consumption and will eventually be gated behind mTLS authentication.
            """,

        ["Devices"] = """
            Device telemetry and consumable age tracking.

            - **Battery** — Track and analyse battery status across diabetes devices.
            - **Device Age** — CAGE (cannula), SAGE (sensor), IAGE (insulin reservoir), and BAGE (battery) age tracking, backed by the V4 DeviceEvents system.
            """,

        ["Health"] = """
            Biometric and activity data beyond glucose.

            - **Heart Rate** — Heart rate readings sourced from xDrip.
            - **Step Count** — Step count data from xDrip's PebbleMovement integration.
            - **Body Weight** — Weight and body composition time-series.
            - **Patient Record** — Patient metadata: records, devices, and insulin formulations in use.

            > **Note:** Heart rate and step count endpoints are currently xDrip-specific. Data from other sources (Garmin, Apple Health, etc.) is not yet ingested here.
            """,

        ["Identity"] = """
            Multi-tenancy, membership, roles, and cross-platform identity linking.

            - **My Tenants** — List tenants the authenticated user belongs to.
            - **My Permissions** — Effective permissions for the current tenant, computed from roles intersected with token scopes.
            - **Roles** — RBAC role and permission management.
            - **Member Invites** — Invite links, member listing, and role assignment.
            - **Connected Apps** — OAuth app grants ("connected apps") for the authenticated user.
            - **Chat Identity** — Tenant-scoped linking of chat platform accounts (Discord, Telegram, etc.).
            - **Chat Identity Directory** — Cross-tenant directory for routing chat platform identities to the correct tenant. Server-to-server only.

            > **Footgun:** The Chat Identity Directory operates cross-tenant and is authenticated by instance key, not user tokens. Do not expose it to end users.
            """,

        ["Monitoring"] = """
            Alerting, notifications, and flexible tracker management.

            - **Alert Rules** — CRUD for alert rules with nested schedules, escalation steps, and notification channels.
            - **Alerts** — Active alert state, history, and acknowledgement.
            - **Alert Invites** — Shareable invite links that grant others permission to receive your alerts.
            - **Alert Custom Sounds** — Upload, list, stream, and delete custom alert audio files.
            - **Tracker Alerts** — Alerts tied to tracker events (e.g. "site change overdue").
            - **Trackers** — Flexible tracker management for consumables, appointments, and reminders.
            - **Notifications** — In-app notification delivery and management.
            """,

        ["Platform"] = """
            System-level status, diagnostics, and service metadata.

            - **Status** — V4 JSON status endpoint with detailed system information.
            - **System** — Service health and coordination endpoints.
            - **System Events** — Point-in-time system events (alarms, warnings, info).
            - **Services** — Metadata about available data sources, connectors, and integrations.
            - **Compatibility** — Dashboard data for Nightscout compatibility analysis.
            - **Debug** — Query inspection and MongoDB query debugging tools.
            - **API Secret** — Legacy API secret management.

            > **Footgun:** Debug endpoints expose raw query details and are intended for development use. They should be disabled or restricted in production deployments.
            """,

        ["PlatformAdmin"] = """
            Super-admin tenant lifecycle management.

            Provides tenant creation, listing, and administration for platform operators. These endpoints require platform-level admin privileges — they are not accessible to regular tenant users.
            """,

        ["Profiles"] = """
            User and therapy configuration.

            - **Profile** — Therapy settings: basal schedules, carb ratio schedules, and insulin sensitivity scales.
            - **UI Settings** — Aggregated frontend configuration from multiple sources (units, ranges, display preferences).
            - **User Preferences** — Per-user preference storage.
            - **Clock Faces** — Watch face configuration management.
            - **MyFitnessPal Settings** — Global settings for MyFitnessPal food matching integration.
            """,

        ["TenantAdmin"] = """
            Administrative operations for tenant data management, migration, and maintenance.

            - **Migration** — Import data from a Nightscout MongoDB instance.
            - **Nightscout Transition** — Aggregated migration progress and write-compatibility status for the migration dashboard.
            - **Backfill** — Decompose all existing legacy entries and treatments into V4 granular tables.
            - **Deduplication** — Run and monitor deduplication jobs across data tables.
            - **Discrepancy** — Compatibility analysis between legacy and V4 data representations.
            - **Compression Low** — Detect and review compression low artefacts in CGM data.
            - **Processing** — Async processing job status tracking.
            - **OIDC Provider Admin** — Manage OIDC identity provider configurations for the tenant.
            - **Subject Admin** — Manage user/subject records within the tenant.

            > **Footgun:** The Backfill endpoint decomposes *all* legacy data. On large datasets this is a long-running operation — it runs asynchronously and progress can be tracked via the Processing endpoints.
            """,

        ["Treatments"] = """
            V4 treatment data: meals, boluses, site changes, and nutrition tracking.

            - **Treatments** — V4 treatment CRUD with automatic tracker integration.
            - **Nutrition** — Carbohydrate intakes, food breakdown, and meal records.
            - **Foods** — Food favourites, recent foods, and food lifecycle management.
            - **Connector Food Entries** — Food entries imported by external connectors.
            - **Meal Matching** — Match nutrition data to treatment events.

            > **Footgun:** Unlike V1–V3, the V4 treatments endpoint does **not** include StateSpan-derived basal data. For basal delivery, use the State Spans endpoints under Analytics instead. This is an intentional separation of concerns.
            """,

        ["Metadata"] = """
            Static, read-only reference catalogs for populating app UI with prefilled lists.

            - **Device Catalog** — Known pump, CGM, and meter hardware models, filterable by category.
            - **Insulin Catalog** — Insulin formulations with pharmacokinetic profiles (onset, peak, duration).

            This is reference data — it is not tenant-specific and cannot be modified via the API.
            """,

        // ── Nightscout document (V1 / V2 / V3) ──────────────────────────

        ["V1"] = """
            Legacy Nightscout V1 API — **1:1 compatible** with the original JavaScript implementation.

            Covers the core Nightscout data model: entries (SGV, MBG, CAL), treatments (bolus, temp basal, carb corrections, site changes), profiles, device status, and food records. Also includes Alexa voice assistant integration and Pebble smartwatch endpoints.

            > **Timestamps:** V1 uses a "mills-first" convention. `Entry.Mills` (Unix milliseconds) is the source of truth; `Date` and `DateString` are computed from it. Clients that write entries must provide `date` in epoch milliseconds.

            > **Authentication:** V1 endpoints accept the legacy `api_secret` header (SHA-1 hash) or token-based auth via `?token=` query parameter. Both are supported for backwards compatibility.
            """,

        ["V2"] = """
            Enhanced Nightscout V2 API — maintains compatibility with legacy V2 consumers.

            - **Authorization** — Token permission checking.
            - **DData** — Direct data access for aggregated reads.
            - **Loop** — Apple Push Notification Service (APNS) integration for the iOS Loop app.
            - **Notifications** — Enhanced notification system with push support.
            - **Properties** — Client properties and runtime settings.
            - **Summary** — Aggregated data endpoints for dashboard widgets.
            """,

        ["V3"] = """
            Nightscout V3 RESTful API — full CRUD with `Last-Modified` / `If-Modified-Since` support.

            Provides a consistent RESTful interface across all core collections: entries, treatments, device status, food, profiles, and settings. Each collection supports filtering, pagination, field projection, and soft-delete semantics.

            - **Last Modified** — Timestamps for when each collection was last modified, enabling efficient polling via conditional requests.
            - **Status** — Extended status with permissions and authorization details.
            - **Version** — Server version information.

            > **Note:** V3 uses `identifier` (a string field) as the primary key for records, not the MongoDB `_id`. When migrating from V1, ensure your client uses the correct identifier field.
            """,
    };

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        // Collect every tag name already referenced by operations.
        var usedTags = new HashSet<string>(StringComparer.Ordinal);

        foreach (var pathItem in document.Paths.Values)
        {
            if (pathItem.Operations is null) continue;
            foreach (var operation in pathItem.Operations.Values)
            {
                if (operation.Tags is null) continue;
                foreach (var tag in operation.Tags)
                {
                    if (tag is IOpenApiTag openApiTag && openApiTag.Name is not null)
                        usedTags.Add(openApiTag.Name);
                }
            }
        }

        // Build the document-level tag set with descriptions.
        var tags = new HashSet<OpenApiTag>(TagNameComparer.Instance);

        foreach (var tagName in usedTags.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
        {
            Descriptions.TryGetValue(tagName, out var description);

            // Append any ER diagrams mapped to this tag.
            if (_tagDiagrams.TryGetValue(tagName, out var diagrams))
            {
                var sb = new System.Text.StringBuilder();
                if (!string.IsNullOrWhiteSpace(description))
                {
                    sb.AppendLine(description);
                    sb.AppendLine();
                }

                sb.AppendLine("## Data Model");
                sb.AppendLine();

                foreach (var diagram in diagrams)
                {
                    sb.AppendLine($"**{diagram.Title}**");
                    if (!string.IsNullOrWhiteSpace(diagram.Description))
                        sb.AppendLine($"_{diagram.Description}_");
                    sb.AppendLine();
                    sb.AppendLine($"[![{diagram.Title}]({diagram.SvgPath})]({diagram.SvgPath})");
                    sb.AppendLine();
                }

                description = sb.ToString().TrimEnd();
            }

            tags.Add(new OpenApiTag
            {
                Name = tagName,
                Description = description,
            });
        }

        document.Tags = tags;

        return Task.CompletedTask;
    }

    private static Dictionary<string, List<DiagramRef>> BuildTagDiagramMap(IWebHostEnvironment env)
    {
        var manifestPath = Path.Combine(env.ContentRootPath, "..", "..", "..", "docs", "diagrams", "diagrams.yaml");
        var map = new Dictionary<string, List<DiagramRef>>(StringComparer.Ordinal);

        if (!File.Exists(manifestPath))
            return map;

        var yaml = File.ReadAllText(manifestPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        var manifest = deserializer.Deserialize<DiagramManifest>(yaml);

        foreach (var diagram in manifest.Diagrams)
        {
            if (diagram.Tags is not { Count: > 0 })
                continue;

            var svgName = Path.GetFileNameWithoutExtension(diagram.Source) + ".svg";
            var diagramRef = new DiagramRef(diagram.Title, diagram.Description, $"/diagrams/{svgName}");

            foreach (var tag in diagram.Tags)
            {
                if (!map.TryGetValue(tag, out var list))
                {
                    list = [];
                    map[tag] = list;
                }
                list.Add(diagramRef);
            }
        }

        return map;
    }

    private sealed record DiagramRef(string Title, string? Description, string SvgPath);

    private sealed class DiagramManifest
    {
        public List<DiagramEntry> Diagrams { get; set; } = [];
    }

    private sealed class DiagramEntry
    {
        public string Source { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public List<string>? Tags { get; set; }
        public string? Auto { get; set; }
        public string? Module { get; set; }
    }

    private sealed class TagNameComparer : IEqualityComparer<OpenApiTag>
    {
        public static readonly TagNameComparer Instance = new();

        public bool Equals(OpenApiTag? x, OpenApiTag? y) =>
            string.Equals(x?.Name, y?.Name, StringComparison.Ordinal);

        public int GetHashCode(OpenApiTag obj) =>
            obj.Name?.GetHashCode(StringComparison.Ordinal) ?? 0;
    }
}
