# Dependencies Catalog

Complete dependency inventory for Workplace Booking Platform.

## Backend (.NET 8) - BookingPlatform.Api

### Runtime Dependencies (Production)

| Package | Version | Purpose | License |
|---------|---------|---------|---------|
| Microsoft.AspNetCore.OpenApi | 8.0.* | OpenAPI/Swagger generation | MIT |
| Swashbuckle.AspNetCore | 6.5.* | Swagger UI & JSON endpoint | MIT |
| MediatR | 12.2.* | CQRS Mediator pattern | MIT |
| MediatR.Extensions.Microsoft.DependencyInjection | 11.1.* | DI integration for MediatR | MIT |
| FluentValidation | 11.9.* | Input validation | Apache-2.0 |
| FluentValidation.DependencyInjectionExtensions | 11.9.* | DI integration | Apache-2.0 |
| AutoMapper | 13.0.* | Object-object mapping | MIT |
| AutoMapper.Extensions.Microsoft.DependencyInjection | 12.0.* | DI integration | MIT |
| Microsoft.EntityFrameworkCore | 8.0.* | ORM Core | MIT |
| Microsoft.EntityFrameworkCore.Design | 8.0.* | Design-time tools (dev only) | MIT |
| Microsoft.EntityFrameworkCore.Tools | 8.0.* | CLI migrations (dev only) | MIT |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.* | PostgreSQL provider | MIT |
| Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite | 8.0.* | PostGIS support (future) | MIT |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.* | JWT validation | MIT |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 7.5.* | OIDC configuration retrieval | MIT |
| Microsoft.IdentityModel.Tokens | 7.5.* | Token validation | MIT |
| System.IdentityModel.Tokens.Jwt | 7.5.* | JWT handling | MIT |
| Serilog.AspNetCore | 8.0.* | Structured logging integration | Apache-2.0 |
| Serilog.Sinks.Console | 6.0.* | Console sink | Apache-2.0 |
| Serilog.Sinks.File | 6.0.* | File sink | Apache-2.0 |
| Serilog.Sinks.PostgreSQL | 2.3.* | PostgreSQL sink (audit) | MIT |
| Serilog.Enrichers.Environment | 3.0.* | Environment enrichment | Apache-2.0 |
| Serilog.Enrichers.Process | 3.0.* | Process enrichment | Apache-2.0 |
| Serilog.Enrichers.Thread | 4.0.* | Thread enrichment | Apache-2.0 |
| Hangfire.AspNetCore | 1.8.* | Background jobs dashboard | LGPL-3.0 |
| Hangfire.PostgreSql | 1.8.* | Hangfire PostgreSQL storage | LGPL-3.0 |
| MailKit | 4.7.* | SMTP client | MIT |
| MimeKit | 4.7.* | Email message construction | MIT |
| Ardalis.Result | 10.0.* | Result pattern (Success/Failure) | MIT |
| Ardalis.Specification | 8.0.* | Specification pattern | MIT |
| Ardalis.Specification.EntityFrameworkCore | 8.0.* | EF Core spec integration | MIT |
| Microsoft.Extensions.Http.Polly | 8.0.* | HTTP resilience policies | MIT |
| Polly | 8.4.* | Resilience/transient fault handling | MIT |
| AspNetCore.HealthChecks.NpgSql | 8.0.* | PostgreSQL health check | Apache-2.0 |
| AspNetCore.HealthChecks.UI.Client | 8.0.* | Health check UI | Apache-2.0 |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.7.* | OTLP metrics/traces export | Apache-2.0 |
| OpenTelemetry.Extensions.Hosting | 1.7.* | ASP.NET Core integration | Apache-2.0 |
| OpenTelemetry.Instrumentation.AspNetCore | 1.7.* | Auto-instrumentation | Apache-2.0 |
| OpenTelemetry.Instrumentation.EntityFrameworkCore | 1.7.* | EF Core instrumentation | Apache-2.0 |
| OpenTelemetry.Instrumentation.Http | 1.7.* | HttpClient instrumentation | Apache-2.0 |
| Scalar.AspNetCore | 1.2.* | Modern API reference UI | MIT |

### Development Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NET.Test.Sdk | 17.9.* | Test runner |
| xunit | 2.7.* | Unit testing framework |
| xunit.runner.visualstudio | 2.5.* | VS Test Explorer integration |
| Moq | 4.20.* | Mocking framework |
| FluentAssertions | 6.12.* | Assertion library |
| Testcontainers.PostgreSql | 3.7.* | PostgreSQL in containers for tests |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.* | Integration testing host |
| WireMock.Net | 1.5.* | HTTP mocking for external services |
| coverlet.collector | 6.0.* | Code coverage |
| ReportGenerator | 5.3.* | Coverage reports |
| SonarAnalyzer.CSharp | 9.32.* | Static analysis (SonarQube) |
| StyleCop.Analyzers | 1.2.* | Code style enforcement |

## Backend - BookingPlatform.Domain

### Runtime Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Ardalis.Result | 10.0.* | Result pattern |
| Ardalis.Specification | 8.0.* | Specification pattern |

*Minimal by design - zero external dependencies preferred*

## Backend - BookingPlatform.Application

### Runtime Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 12.2.* | CQRS contracts |
| FluentValidation | 11.9.* | Validation |
| AutoMapper | 13.0.* | Mapping contracts |
| Ardalis.Result | 10.0.* | Result pattern |
| Ardalis.Specification | 8.0.* | Specification pattern |
| Microsoft.Extensions.Logging.Abstractions | 8.0.* | Logging abstraction |

## Backend - BookingPlatform.Infrastructure

### Runtime Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0.* | EF Core |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.* | PostgreSQL provider |
| MediatR | 12.2.* | Domain event publishing |
| Microsoft.AspNetCore.Authentication.JwtBearer | 8.0.* | JWT validation |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 7.5.* | OIDC |
| MailKit | 4.7.* | Email |
| Hangfire.Core | 1.8.* | Background jobs |
| Hangfire.PostgreSql | 1.8.* | Hangfire storage |
| Serilog | 4.0.* | Logging |
| OpenTelemetry.Api | 1.7.* | Tracing API |

## Frontend (React + TypeScript + Material UI)

### Production Dependencies

| Package | Version | Purpose | License |
|---------|---------|---------|---------|
| react | 18.2.* | UI Library | MIT |
| react-dom | 18.2.* | DOM renderer | MIT |
| react-router-dom | 6.22.* | Routing | MIT |
| @mui/material | 5.15.* | Component library | MIT |
| @mui/icons-material | 5.15.* | Icon set | MIT |
| @mui/x-date-pickers | 6.19.* | Date/Time pickers | MIT |
| @mui/x-data-grid | 6.19.* | Data grid (admin) | MIT |
| @emotion/react | 11.11.* | CSS-in-JS (MUI) | MIT |
| @emotion/styled | 11.11.* | Styled components (MUI) | MIT |
| @tanstack/react-query | 5.20.* | Server state management | MIT |
| @tanstack/react-query-devtools | 5.20.* | DevTools (dev) | MIT |
| axios | 1.6.* | HTTP client | MIT |
| react-hook-form | 7.50.* | Form management | MIT |
| @hookform/resolvers | 3.3.* | Validation resolvers | MIT |
| zod | 3.22.* | Schema validation | MIT |
| date-fns | 3.3.* | Date manipulation | MIT |
| date-fns-tz | 2.0.* | Timezone support | MIT |
| @tanstack/react-virtual | 3.2.* | Virtualized lists | MIT |
| react-hot-toast | 2.4.* | Toast notifications | MIT |
| jwt-decode | 4.0.* | JWT parsing (client) | MIT |
| qrcode.react | 3.1.* | QR code generation | MIT |
| html2canvas | 1.4.* | Screenshot/export (reports) | MIT |
| file-saver | 2.0.* | File download | MIT |
| clsx | 2.1.* | Conditional classNames | MIT |
| tailwind-merge | 2.2.* | Tailwind class merging | MIT |

### Development Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| @types/react | 18.2.* | React types |
| @types/react-dom | 18.2.* | React DOM types |
| @types/node | 20.11.* | Node types |
| typescript | 5.3.* | TypeScript compiler |
| vite | 5.1.* | Build tool / Dev server |
| @vitejs/plugin-react | 4.2.* | React plugin for Vite |
| vitest | 1.3.* | Unit testing |
| @testing-library/react | 14.2.* | Component testing |
| @testing-library/jest-dom | 6.4.* | Jest DOM matchers |
| @testing-library/user-event | 14.5.* | User interaction testing |
| jsdom | 24.0.* | DOM environment for tests |
| playwright | 1.42.* | E2E testing |
| @playwright/test | 1.42.* | Playwright test runner |
| eslint | 8.56.* | Linting |
| @typescript-eslint/eslint-plugin | 7.0.* | TypeScript ESLint rules |
| @typescript-eslint/parser | 7.0.* | TypeScript parser |
| eslint-plugin-react | 7.33.* | React ESLint rules |
| eslint-plugin-react-hooks | 4.6.* | React Hooks rules |
| eslint-plugin-jsx-a11y | 6.8.* | Accessibility rules |
| prettier | 3.2.* | Code formatting |
| @mui/material-nextjs | - | Not used (no Next.js) |
| @commitlint/cli | 19.0.* | Commit message linting |
| @commitlint/config-conventional | 19.0.* | Conventional commits |
| husky | 9.0.* | Git hooks |
| lint-staged | 15.2.* | Staged file linting |

## Infrastructure

### Docker Images (Base)

| Image | Tag | Purpose |
|-------|-----|---------|
| mcr.microsoft.com/dotnet/aspnet | 8.0-bookworm-slim | Backend runtime |
| mcr.microsoft.com/dotnet/sdk | 8.0-bookworm | Backend build |
| node | 20-bookworm-slim | Frontend build |
| nginx | 1.25-alpine | Frontend serve + Reverse proxy |
| postgres | 16-bookworm | Database |
| ghcr.io/owasp/zap2docker-stable | latest | DAST scanning |

### Nginx Modules (Built-in)

- http_ssl_module
- http_v2_module
- http_realip_module
- http_gzip_static_module
- http_brotli_filter_module (brotli)

## CI/CD (GitHub Actions)

### Actions Used

| Action | Version | Purpose |
|--------|---------|---------|
| actions/checkout | v4 | Checkout code |
| actions/setup-dotnet | v4 | .NET SDK |
| actions/setup-node | v4 | Node.js |
| docker/build-push-action | v5 | Build/push images |
| docker/login-action | v3 | Registry auth |
| github/codeql-action | v3 | SAST |
| dependabot/fetch-metadata | v2 | Dependabot metadata |
| aquasecurity/trivy-action | v0.18 | Container scanning |
| softprops/action-gh-release | v1 | Release creation |

## Development Tools

### Required on Developer Machine

| Tool | Version | Installation |
|------|---------|--------------|
| .NET SDK | 8.0.100+ | winget/chocolatey/official |
| Node.js | 20 LTS | nvm/volta/official |
| Docker Desktop | Latest | Official |
| Git | Latest | Official |
| VS Code / Rider | Latest | Official |
| PostgreSQL Client (psql) | 16 | Optional (for direct DB access) |

### VS Code Extensions (Recommended)

| Extension | ID |
|-----------|----|
| C# Dev Kit | ms-dotnettools.csdevkit |
| C# | ms-dotnettools.csharp |
| ES7+ React/Redux/React-Native Snippets | dsznajder.es7-react-js-snippets |
| TypeScript Hero | rbbit.typescript-hero |
| ESLint | dbaeumer.vscode-eslint |
| Prettier | esbenp.prettier-vscode |
| Tailwind CSS IntelliSense | bradlc.vscode-tailwindcss |
| Material Icon Theme | pkief.material-icon-theme |
| Docker | ms-azuretools.vscode-docker |
| GitHub Actions | github.vscode-github-actions |
| REST Client | humao.rest-client |
| Thunder Client | rangav.vscode-thunder-client |

## License Compliance

### Copyleft / Restrictive Licenses

| Package | License | Risk | Mitigation |
|---------|---------|------|------------|
| Hangfire.AspNetCore | LGPL-3.0 | Dynamic linking only | OK - dynamic linking exception |
| Hangfire.PostgreSql | LGPL-3.0 | Dynamic linking only | OK - dynamic linking exception |
| Npgsql | MIT | None | - |
| All others | MIT/Apache-2.0 | None | - |

**Policy**: No GPL-licensed dependencies in production runtime. LGPL allowed only with dynamic linking.

## Dependency Update Strategy

| Category | Frequency | Automation |
|----------|-----------|------------|
| Security patches | Immediate | Dependabot PRs (auto-merge patch) |
| Minor updates | Weekly | Dependabot PRs (manual review) |
| Major updates | Quarterly | Planned sprint work |
| Framework (.NET/Node) | LTS schedule | Dedicated upgrade sprints |

## SBOM Generation

```bash
# Backend
dotnet sbom generate -o src/backend/src/BookingPlatform.Api

# Frontend
npm sbom --json > frontend-sbom.json

# Docker
docker sbom <image> --format cyclonedx-json > image-sbom.json
```

---

*Keep this catalog updated with each dependency change. Run `dotnet list package --vulnerable` and `npm audit` regularly.*