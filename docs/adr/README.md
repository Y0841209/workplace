# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records for the Workplace Booking Platform.

## ADR Index

| ID | Title | Status | Date |
|----|-------|--------|------|
| [0001](0001-clean-architecture.md) | Clean Architecture Layering | Accepted | 2026-08-15 |
| [0002](0002-cqrs-mediatr.md) | CQRS with MediatR for Backend | Accepted | 2026-08-15 |
| [0003](0003-postgresql-exclusion-constraints.md) | PostgreSQL Exclusion Constraints for Booking Conflicts | Accepted | 2026-08-15 |
| [0004](0004-entra-id-oidc.md) | Microsoft Entra ID with OIDC Authentication | Accepted | 2026-08-15 |
| [0005](0005-hybrid-rbac-abac.md) | Hybrid RBAC + ABAC Authorization Model | Accepted | 2026-08-15 |
| [0006](0006-transactional-outbox.md) | Transactional Outbox Pattern for Notifications | Accepted | 2026-08-15 |
| [0007](0007-qr-checkin-design.md) | QR Check-in with Public UUID per Resource | Accepted | 2026-08-15 |
| [0008](0008-react-tanstack-query.md) | React with TanStack Query for State Management | Accepted | 2026-08-15 |
| [0009](0009-multi-stage-docker.md) | Multi-stage Docker Builds | Accepted | 2026-08-15 |
| [0010](0010-nginx-reverse-proxy.md) | Nginx Reverse Proxy with Security Hardening | Accepted | 2026-08-15 |
| [0011](0011-ef-core-code-first.md) | EF Core Code-First Migrations | Accepted | 2026-08-15 |
| [0012](0012-result-pattern.md) | Result Pattern for Error Handling | Accepted | 2026-08-15 |
| [0013](0013-audit-logging.md) | Dual Audit Logging (Middleware + Domain Events) | Accepted | 2026-08-15 |
| [0014](0014-background-jobs-hangfire.md) | Hangfire for Background Job Processing | Accepted | 2026-08-15 |
| [0015](0015-api-versioning.md) | URL Path API Versioning | Accepted | 2026-08-15 |
| [0016](0016-testing-pyramid.md) | Testing Pyramid with Contract Tests | Accepted | 2026-08-15 |
| [0017](0017-observability-otel.md) | OpenTelemetry Observability Stack | Accepted | 2026-08-15 |
| [0018](0018-localization.md) | Spanish (es-CO) Primary Localization | Accepted | 2026-08-15 |

---

## ADR Template

```markdown
# ADR-NNN: Title

## Status
[Proposed | Accepted | Superseded | Deprecated]

## Context
What is the issue that we're seeing that is motivating this decision or change?

## Decision
What is the change that we're proposing and/or doing?

## Consequences
What becomes easier or more difficult to do because of this change?

### Positive
- 

### Negative
- 

### Neutral
- 

## Alternatives Considered
1. **Alternative 1** - Why rejected
2. **Alternative 2** - Why rejected

## References
- Links to relevant documentation, issues, or discussions
```

---

*All ADRs follow the template above. New ADRs should be added sequentially.*