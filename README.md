# Nocturne

A modern, high-performance diabetes management platform built with .NET 9. Nocturne is a complete rewrite of the Nightscout API with full feature parity, providing native C# implementations of all endpoints with optimized performance and modern cloud-native architecture.

## What is Nocturne?

Nocturne is a comprehensive diabetes data platform that provides:

- **Complete Nightscout API Implementation** - All Nightscout endpoints natively implemented in C# with full compatibility
- **Data Connectors** - Native integration with major diabetes platforms (Dexcom, Glooko, LibreLinkUp, MiniMed CareLink, MyFitnessPal, Nightscout)
- **Real-time Updates** - WebSocket/SignalR support for live glucose readings and alerts
- **Advanced Analytics** - Comprehensive glucose statistics, time-in-range calculations, and reports
- **Cloud-Native** - Built on .NET Aspire for seamless local development and cloud deployment

## Architecture

```
Nocturne/
├── src/
│   ├── API/                        # REST API (Nightscout-compatible)
│   ├── Connectors/                # Data source integrations
│   ├── Core/                       # Domain models and interfaces
│   ├── Infrastructure/             # Data access and caching
│   ├── Aspire/                     # .NET Aspire orchestration
│   └── Tools/                      # CLI tools
└── tests/                          # Comprehensive test suite
```

## Key Features

- **Full Nightscout API Parity** - All v1, v2, and v3 endpoints
- **High Performance** - Optimized queries with PostgreSQL and Redis caching
- **Authentication** - JWT-based auth with API_SECRET support
- **Real-time** - SignalR hubs for live data streaming
- **Data Connectors** - Dexcom Share, Glooko, LibreLinkUp, MiniMed CareLink, MyFitnessPal, Nightscout, and MyLife
- **PostgreSQL** - Modern relational database with EF Core migrations
- **Observability** - OpenTelemetry integration for monitoring (Soon)
- **Containerized** - Docker support for all services

## Quick Start with .NET Aspire (Development)

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [NodeJS](https://nodejs.org/)
- [pnpm](https://pnpm.io/)

Copy the appsettings.example.json, and rename it to `appsettings.json`. Fill in the values for the connection strings, and any other settings you want to change. If you'd like to pipe in your Nightscout values into it just to test it out, do so in the `Connector.Nightscout` section, _not_ the CompatibilityProxy; they are fundamentally different things.

.NET Aspire orchestrates all services with a single command:

```bash
dotnet aspire run
```

Aspire will automatically:

- Start PostgreSQL in a container
- Run database migrations
- Start the Nocturne API
- Launch any configured data connectors
- Set up service discovery and health checks
- Click on the link in the console, which will have Open the Aspire dashboard at `[http://localhost:17257](https://localhost:17257/)`

You can then access the frontend from the port assigned to it.

### Access the API

Once Aspire starts:

- **API**: https://localhost:1612
- **API Documentation**: https://localhost:1612/scalar
- **Aspire Dashboard**: http://localhost:15888

## Configuration

### appsettings.json

The main configuration file in the solution root:

```json
{
  "ConnectionStrings": {
    "nocturne": "Host=localhost;Port=5432;Database=nocturne;Username=nocturne;Password=nocturne"
  },
  "Authentication": {
    "ApiSecret": "your-secret-here",
    "JwtKey": "your-jwt-signing-key",
    "JwtIssuer": "Nocturne",
    "JwtAudience": "NightscoutClient"
  }
}
```

#### Environment Variables

Override configuration using environment variables:

```bash
ConnectionStrings__nocturne="Host=mydb;..."
Authentication__ApiSecret="my-secret"
ASPNETCORE_ENVIRONMENT=Production
```

You generally shouldn't have to do this, **ever** during development- configuration lives in the appsettings, and is automagically passed through.

### Multitenancy (Custom Local Domain)

By default, Aspire serves the app at `https://localhost:1612`. This works for single-tenant development but **WebAuthn passkeys fail on tenant subdomains** because browsers reject `localhost` as a passkey Relying Party ID for subdomain origins.

To test multitenancy with passkeys locally, configure a custom domain:

**1. Install mkcert**

```bash
# Windows
winget install FiloSottile.mkcert

# macOS
brew install mkcert

# Linux — use your distro's package manager
```

**2. Set the custom domain**

```bash
cd src/Aspire/Nocturne.Aspire.Host
dotnet user-secrets set "LocalDev:Domain" "nocturne.test"
```

**3. Add hosts file entries**

Add lines to your hosts file (`C:\Windows\System32\drivers\etc\hosts` on Windows, `/etc/hosts` on macOS/Linux):

```
127.0.0.1  nocturne.test
127.0.0.1  demo.nocturne.test
127.0.0.1  riley.nocturne.test
```

Add one line per tenant slug you want to use. Hosts files don't support wildcards.

**4. Start Aspire**

```bash
aspire start
```

Aspire will automatically generate a wildcard TLS certificate for `*.nocturne.test`, install the mkcert CA into your system trust store, and configure the YARP gateway to use it. Access the app at `https://demo.nocturne.test:1612`.

## Data Connectors

Nocturne includes native connectors for popular diabetes platforms:

| Connector            | Description                          | Status    |
| -------------------- | ------------------------------------ | --------- |
| **Dexcom Share**     | Dexcom CGM data via Share API        | Supported |
| **Glooko**           | Comprehensive diabetes data platform | Supported |
| **LibreLinkUp**      | FreeStyle Libre glucose readings     | Supported |
| **MiniMed CareLink** | Medtronic pump and sensor data       | Supported |
| **MyFitnessPal**     | Food and nutrition tracking          | Supported |
| **Nightscout**       | Nightscout-to-Nightscout sync        | Supported |
| **MyLife**           | Syncing for MyLife / CamAPS FX       | Supported |

### Using Connectors

If you set up the connector's settings in the appsettings, then it'll automatically start when you run `aspire run`.

## Production Deployment (Docker Compose)

The easiest way to deploy Nocturne is with the production Docker Compose bundle. Each [GitHub Release](https://github.com/nightscout/nocturne/releases) includes ready-to-use artifacts, or you can generate them locally.

### Using a release

Download `docker-compose.production.yaml`, `.env.production`, and the `container-init/` folder from the latest release. The `.env.production` comes with random PostgreSQL passwords already generated.

```bash
# Review and customise .env.production:
#   - Set PUBLIC_BASE_DOMAIN to your domain (e.g. nocturne.example.com)
#   - Optionally add Discord / Telegram / Slack / WhatsApp credentials

docker compose -f docker-compose.production.yaml up -d
```

The production compose includes [Watchtower](https://github.com/nicholas-fedor/watchtower) for automatic container updates (checks daily), and omits the Aspire dashboard and Scalar API explorer.

### Generating locally

If you have the .NET SDK and Aspire CLI installed, you can generate the production bundle from source:

```bash
./scripts/publish-production.sh          # outputs to repo root
./scripts/publish-production.sh ./deploy # or specify a directory
```

The script runs `aspire publish` with production flags and auto-generates random passwords for all PostgreSQL roles.

### PostgreSQL Roles

Nocturne uses three separate PostgreSQL roles for defense in depth. All three have `NOBYPASSRLS` so they obey Row Level Security policies, even when the database has no superuser connected.

| Role                    | Purpose                                                                                | Privileges                                                                                                     |
| ----------------------- | -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- |
| **`nocturne_migrator`** | Runs EF Core migrations (schema DDL). Owns the database and `public` schema.           | `CREATE`, `ALTER`, `DROP` on tables. Cannot bypass RLS.                                                        |
| **`nocturne_app`**      | Runtime connection pool for the .NET API. Owns nothing.                                | `SELECT`, `INSERT`, `UPDATE`, `DELETE` on migrator-created tables. Cannot bypass RLS.                          |
| **`nocturne_web`**      | SvelteKit bot framework (chat state storage). Owns only its own `chat_state_*` tables. | `CREATE` on `public` schema (for its own tables only). No access to Nocturne tenant tables. Cannot bypass RLS. |

The bootstrap user (`POSTGRES_USER`) is only used for initial container setup. After `container-init/00-init.sh` runs, all application traffic flows through the three roles above. Passwords are set via environment variables in `.env.production`.

For bring-your-own PostgreSQL (not using the bundled container), run `docs/postgres/bootstrap-roles.sql` once as a superuser. See the comments in that file for details.

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run unit tests only
dotnet test --filter "Category!=Integration&Category!=Performance"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Migrations

```bash
# Create a new migration
cd src/Infrastructure/Nocturne.Infrastructure.Data
dotnet ef migrations add YourMigrationName

# Apply migrations
dotnet ef database update
```

## Tools

### MCP Server

Model Context Protocol server for AI integration.

```bash
dotnet run --project src/Tools/Nocturne.Tools.McpServer -- server
```

See [src/Tools/README.md](src/Tools/README.md) for detailed tool documentation.

## Deployment

### Docker Compose (Recommended)

See [Production Deployment](#production-deployment-docker-compose) above.

### Azure Container Apps

```bash
# Install Azure Developer CLI
curl -fsSL https://aka.ms/install-azd.sh | bash

# Deploy to Azure
azd auth login
azd init
azd up
```

## API Documentation

### Interactive Documentation

- **Scalar UI**: https://localhost:1612/scalar
- **OpenAPI JSON**: https://localhost:1612/openapi/v1.json

### Key Endpoints

Nocturne aims to match Nightscout's API 1:1, so any Nightscout API endpoint should be usable. Nocturne-only endpoints are scoped to v4.

```
GET    /api/v1/entries          # Glucose entries
POST   /api/v1/entries
GET    /api/v1/treatments       # Treatments
POST   /api/v1/treatments
GET    /api/v1/devicestatus     # Device status
GET    /api/v1/profile          # Profile settings
GET    /api/v2/properties       # Statistics
WS     /hubs/data               # Real-time SignalR hub
```

## License

Nocturne is licensed under the [GNU Affero General Public License v3.0 (AGPL-3.0)](LICENSE). Commercial licensing is available for organizations that need to use Nocturne without AGPL obligations — contact the maintainers for details.

## Disclaimer

Nocturne is a community project and is not affiliated with or endorsed by the Nightscout Project, Abbott, Dexcom, Medtronic, Glooko, or MyFitnessPal.

**Important:** This software is provided as-is for personal use. Always verify glucose readings with approved medical devices. Never make treatment decisions based solely on data from this application.

## Acknowledgments

- Built on the shoulders of the [Nightscout Project](https://github.com/nightscout/cgm-remote-monitor)
- Powered by [.NET 10](https://dotnet.microsoft.com/) and [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/)
