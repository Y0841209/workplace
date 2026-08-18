# CI/CD & Security Workflows Summary

## Workflows Created

### 1. `.github/workflows/ci.yml` - Main CI Pipeline
Complete CI/CD pipeline with 12 jobs:

| Job | Trigger | Purpose |
|-----|---------|---------|
| `lint-and-format` | PR/Push | Code formatting, linting, outdated packages |
| `backend-unit-tests` | PR/Push | Backend unit tests with coverage |
| `backend-integration-tests` | PR/Push | Integration tests with Testcontainers |
| `frontend-unit-tests` | PR/Push | Frontend unit tests + typecheck |
| `frontend-e2e-tests` | PR/Push | Playwright E2E tests |
| `codeql-analysis` | PR/Push | CodeQL SAST (C# + JS) |
| `dependency-scan` | PR/Push | Trivy FS scan (HIGH/CRITICAL) |
| `secret-scan` | PR/Push | TruffleHog secret scanning |
| `docker-build` | Push to main/develop | Multi-arch Docker build & push |
| `container-scan` | After docker-build | Trivy container scan |
| `deploy-dev` | Push to develop | Deploy to DEV environment |
| `deploy-qa` | Push to main | Deploy to QA + contract tests |
| `deploy-prod` | Push to main (tag) | Production deploy + release |

### Key Features:
- **Parallel execution** for speed
- **Required checks** on PRs (unit tests, lint, security scans)
- **Environment protection** for QA/PROD deployments
- **Multi-arch Docker builds** (amd64/arm64)
- **Automatic release creation** on tags

### 2. `.github/workflows/security-scheduled.yml` - Scheduled Security Scans

| Schedule | Scan Type | Tools |
|----------|-----------|-------|
| Daily 2 AM | CodeQL | CodeQL (csharp + javascript) |
| Weekly Sun 3 AM | Deep CodeQL | Extended queries |
| Daily 2 AM | Dependency Scan | dotnet list package, npm audit, Trivy FS |
| Weekly | OWASP ZAP | DAST against running stack |
| Weekly | Container Scan | Trivy on built images |
| Weekly | License Check | dotnet-licenses, license-checker |

### Security Tools Used:
| Category | Tool | Purpose |
|----------|------|---------|
| SAST | CodeQL | C# + JS analysis |
| SCA | Dependabot + Trivy | Dependency vulnerabilities |
| DAST | OWASP ZAP | Runtime scanning |
| Container | Trivy | Image vulnerabilities |
| Secrets | TruffleHog | Secret detection |
| License | License Checker | Compliance |

---

## Custom CodeQL Queries Created

### C# Queries (`.github/codeql-queries/csharp/`)
| Query | Purpose | Severity |
|-------|---------|----------|
| `insecure-random.ql` | Detect `Random.Next()` usage | Warning |
| `hardcoded-credentials.ql` | Connection strings, API keys, JWT secrets | Error |
| `sql-injection.ql` | String concat in SQL, FromSqlRaw, ExecuteSqlRaw | Error |
| `xss.ql` | HTML output without encoding, HtmlHelper.Raw | Error |

### JavaScript/TypeScript Queries
| Query | Purpose | Severity |
|-------|---------|----------|
| `xss.ql` | innerHTML, dangerouslySetInnerHTML, eval() | Error |
| `path-traversal.ql` | path.join, fs.readFile/writeFile, path.resolve | Error |

---

## Dependabot Configuration (`.github/dependabot.yml`)

### Update Groups (to reduce PR noise)

| Ecosystem | Groups | Example |
|-----------|--------|---------|
| **NuGet** | Microsoft, ASP.NET Core, EF Core, Testing, Serilog, MediatR, AutoMapper, FluentValidation, Ardalis, OpenTelemetry | 12 groups |
| **NPM** | React, MUI, TanStack, Testing, Build tools, Axios, date-fns, Zod | 8 groups |
| **Docker** | Base images | Weekly |
| **GitHub Actions** | Actions | Weekly |

### Schedule: Weekly Mondays 06:00 America/Bogota
### PR Limits: 10 per ecosystem

---

## Required GitHub Repository Settings

### Branch Protection (main/develop)
```
Required status checks:
  - lint-and-format
  - backend-unit-tests
  - backend-integration-tests
  - frontend-unit-tests
  - frontend-e2e-tests
  - codeql-analysis
  - dependency-scan
  - secret-scan

Require review: 1 approval
Dismiss stale reviews: true
Require up-to-date branches: true
```

### Environments Required
| Environment | Protection Rules |
|-------------|------------------|
| `dev` | Auto-deploy on develop branch |
| `qa` | Required reviewers (1), wait timer 5min |
| `production` | Required reviewers (2), wait timer 30min, deployment branch policy |

### Required Secrets (Repository/Org)
| Secret | Description |
|--------|-------------|
| `GITHUB_TOKEN` | Auto-provided |
| `KUBECONFIG_DEV` | Kubeconfig for DEV cluster |
| `KUBECONFIG_QA` | Kubeconfig for QA cluster |
| `KUBECONFIG_PROD` | Kubeconfig for PROD cluster |
| `AZURE_AD_CLIENT_SECRET` | Entra ID app secret |
| `EMAIL_SMTP_PASSWORD` | SMTP password |
| `POSTGRES_PASSWORD` | Database password |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint URL |

---

## Coverage Requirements (Enforced in CI)

| Metric | Threshold |
|--------|-----------|
| Line Coverage | ≥ 80% |
| Branch Coverage | ≥ 70% |
| Critical Rules Tests | 100% pass |

---

## Security Scanning Summary

| Scan Type | Frequency | Tools | Fail On |
|-----------|-----------|-------|---------|
| SAST | PR + Daily | CodeQL | HIGH/CRITICAL |
| SCA | PR + Daily | Dependabot + Trivy FS | HIGH/CRITICAL |
| Container | Weekly + Build | Trivy Image | HIGH/CRITICAL |
| DAST | Weekly | OWASP ZAP | HIGH |
| Secrets | PR + Daily | TruffleHog | ANY |
| License | Weekly | License Checker | GPL/AGPL |

---

*All workflows use `permissions` for least-privilege access and run on `ubuntu-latest` runners.*