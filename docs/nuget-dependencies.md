# NuGet Dependencies - Workplace Booking Platform

## Backend Projects

### WorkplaceBooking.API (ASP.NET Core Web API)

| Package | Version | Reason |
|---------|---------|--------|
| `Microsoft.AspNetCore.OpenApi` | 8.0.0 | OpenAPI/Swagger support for API documentation |
| `Swashbuckle.AspNetCore` | 6.5.0 | Swagger/OpenAPI generation and UI |
| `Scalar.AspNetCore` | 1.2.0 | Modern API reference documentation UI |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 | JWT Bearer authentication for Microsoft Entra ID |
| `Serilog.AspNetCore` | 8.0.0 | Structured logging integration with ASP.NET Core |
| `Serilog.Sinks.Console` | 6.0.0 | Console logging sink for Serilog |
| `Serilog.Sinks.File` | 6.0.0 | File logging sink with rolling intervals |
| `Serilog.Enrichers.Environment` | 3.0.0 | Enrich logs with environment info |
| `Serilog.Enrichers.Process` | 3.0.0 | Enrich logs with process/thread info |
| `Serilog.Enrichers.Thread` | 4.0.0 | Enrich logs with thread context |
| `Microsoft.Extensions.Http.Polly` | 8.0.0 | HTTP client resilience with Polly |
| `AspNetCore.HealthChecks.NpgSql` | 8.0.0 | PostgreSQL health check |
| `AspNetCore.HealthChecks.UI.Client` | 8.0.0 | Health checks UI client |
| `OpenTelemetry.Exporter.OpenTelemetryProtocol` | 1.7.0 | OTLP exporter for traces/metrics |
| `OpenTelemetry.Extensions.Hosting` | 1.7.0 | OpenTelemetry hosting integration |
| `OpenTelemetry.Instrumentation.AspNetCore` | 1.7.0 | ASP.NET Core auto-instrumentation |
| `OpenTelemetry.Instrumentation.EntityFrameworkCore` | 1.7.0 | EF Core query tracing |
| `OpenTelemetry.Instrumentation.Http` | 1.7.0 | HTTP client instrumentation |
| `Scalar.AspNetCore` | 1.2.0 | Modern API reference UI (alternative to Swagger UI) |
| `AspNetCore.RateLimit` | 5.0.0 | Rate limiting middleware |

---

### WorkplaceBooking.Application

| Package | Version | Reason |
|---------|---------|--------|
| `MediatR` | 12.2.0 | CQRS mediator pattern for commands/queries |
| `FluentValidation` | 11.9.0 | Request validation with fluent rules |
| `AutoMapper` | 13.0.0 | Object-to-object mapping for DTOs |
| `Ardalis.Result` | 10.0.0 | Standardized result pattern (Success/Failure) |
| `Ardalis.Specification` | 8.0.0 | Specification pattern for queries |
| `Microsoft.Extensions.Logging.Abstractions` | 8.0.0 | Logging abstractions |

---

### WorkplaceBooking.Domain

| Package | Version | Reason |
|---------|---------|--------|
| `Ardalis.Result` | 10.0.0 | Result pattern for domain operations |
| `Ardalis.Specification` | 8.0.0 | Specification pattern for queries |

---

### WorkplaceBooking.Infrastructure

| Package | Version | Reason |
|---------|---------|--------|
| `Microsoft.EntityFrameworkCore` | 8.0.0 | ORM for PostgreSQL |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.0 | EF Core design-time tools (migrations) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.0 | PostgreSQL provider for EF Core |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 8.0.0 | JWT validation for Entra ID tokens |
| `Microsoft.IdentityModel.Protocols.OpenIdConnect` | 7.5.0 | OpenID Connect protocol support |
| `MediatR` | 12.2.0 | Mediator pattern for domain events |
| `MailKit` | 4.7.0 | SMTP email sending |
| `Hangfire.Core` | 1.8.0 | Background job processing |
| `Hangfire.PostgreSql` | 1.8.0 | Hangfire storage in PostgreSQL |
| `Serilog` | 4.0.0 | Structured logging |
| `OpenTelemetry.Api` | 1.7.0 | OpenTelemetry API for custom metrics |
| `Ardalis.Result` | 10.0.0 | Result pattern for infrastructure |
| `Ardalis.Specification` | 8.0.0 | Specification pattern for EF Core |
| `Ardalis.Specification.EntityFrameworkCore` | 8.0.0 | EF Core integration for specifications |

### WorkplaceBooking.SharedKernel

No direct NuGet dependencies (only internal project references).

---

### Frontend (React + TypeScript)

#### Dependencies

| Package | Version | Reason |
|---------|---------|--------|
| `react` | ^18.2.0 | React library |
| `react-dom` | ^18.2.0 | React DOM renderer |
| `react-router-dom` | ^6.20.0 | Client-side routing |
| `@mui/material` | ^5.14.0 | Material UI component library |
| `@mui/icons-material` | ^5.14.0 | Material Design icons |
| `@mui/x-date-pickers` | ^6.18.0 | Date/time pickers |
| `@mui/x-data-grid` | ^6.19.0 | Data grid for admin panels |
| `@emotion/react` | ^11.11.0 | Emotion CSS-in-JS for MUI |
| `@emotion/styled` | ^11.11.0 | Styled components for MUI |
| `@tanstack/react-query` | ^5.0.0 | Server state management |
| `@tanstack/react-query-devtools` | ^5.20.0 | DevTools for React Query |
| `@tanstack/react-virtual` | ^3.2.0 | Virtualized lists |
| `@tanstack/react-query-devtools` | ^5.20.0 | React Query DevTools |
| `axios` | ^1.6.0 | HTTP client |
| `date-fns` | ^3.3.0 | Date formatting/manipulation |
| `date-fns-tz` | ^2.0.0 | Timezone support for date-fns |
| `react-hook-form` | ^7.50.0 | Form validation |
| `@hookform/resolvers` | ^3.3.0 | Zod resolver for react-hook-form |
| `zod` | ^3.22.0 | Schema validation |
| `date-fns` | ^3.3.0 | Date formatting |
| `date-fns-tz` | ^2.0.0 | Timezone support |
| `react-hook-form` | ^7.50.0 | Form handling |
| `@hookform/resolvers` | ^3.3.0 | Zod resolver for RHF |
| `zod` | ^3.22.0 | Schema validation |
| `react-hot-toast` | ^2.4.0 | Toast notifications |
| `clsx` | ^2.1.0 | Conditional class names |
| `tailwind-merge` | ^2.2.0 | Tailwind class merging |
| `i18next` | ^23.10.0 | Internationalization |
| `react-i18next` | ^14.1.0 | React integration for i18next |

#### Development Dependencies

| Package | Version | Reason |
|---------|---------|--------|
| `@types/react` | ^18.2.0 | React type definitions |
| `@types/react-dom` | ^18.2.0 | React DOM types |
| `@typescript-eslint/eslint-plugin` | ^7.0.0 | TypeScript ESLint rules |
| `@typescript-eslint/parser` | ^7.0.0 | TypeScript ESLint parser |
| `@vitejs/plugin-react` | ^4.2.0 | Vite React plugin |
| `typescript` | ^5.3.0 | TypeScript compiler |
| `vite` | ^5.1.0 | Build tool / dev server |
| `vitest` | ^1.0.0 | Unit testing framework |
| `@vitest/ui` | ^1.0.0 | Vitest UI |
| `@testing-library/react` | ^14.2.0 | React component testing |
| `@testing-library/jest-dom` | ^6.4.0 | Jest DOM matchers |
| `@testing-library/user-event` | ^14.5.0 | User interaction testing |
| `jsdom` | ^24.0.0 | DOM environment for tests |
| `playwright` | ^1.42.0 | E2E testing |
| `@playwright/test` | ^1.42.0 | Playwright test runner |
| `eslint` | ^8.56.0 | Linting |
| `@typescript-eslint/eslint-plugin` | ^7.0.0 | TypeScript ESLint rules |
| `@typescript-eslint/parser` | ^7.0.0 | TypeScript ESLint parser |
| `eslint-plugin-react` | ^7.33.0 | React ESLint rules |
| `eslint-plugin-react-hooks` | ^4.6.0 | React Hooks rules |
| `eslint-plugin-jsx-a11y` | ^6.8.0 | Accessibility rules |
| `prettier` | ^3.2.0 | Code formatting |
| `husky` | ^9.0.0 | Git hooks |
| `lint-staged` | ^15.2.0 | Pre-commit linting |

---

## Summary by Category

| Category | Package Count |
|----------|---------------|
| **API / Web Framework** | 6 |
| **Authentication / Auth** | 3 |
| **Database / ORM** | 5 |
| **MediatR / CQRS** | 2 |
| **Validation / Mapping** | 3 |
| **Result / Specification Pattern** | 4 |
| **Logging / Serilog** | 6 |
| **OpenTelemetry / Observability** | 6 |
| **Health Checks** | 2 |
| **Rate Limiting** | 1 |
| **API Documentation** | 3 |
| **Background Jobs** | 2 |
| **Email** | 1 |
| **PostgreSQL** | 1 |
| **OpenTelemetry** | 4 |
| **Rate Limiting** | 1 |
| **Frontend Core** | 7 |
| **UI Components (MUI)** | 4 |
| **State Management** | 1 |
| **Forms / Validation** | 5 |
| **Date/Time** | 2 |
| **Forms / HTTP** | 2 |
| **Testing** | 11 |
| **Linting / Formatting** | 9 |
| **Build Tools** | 1 |

**Total NuGet/NPM Packages: ~85**

---

*Generated on: 2026-08-16 | Target Framework: .NET 8.0 / React 18 | All versions aligned with .NET 8 LTS*