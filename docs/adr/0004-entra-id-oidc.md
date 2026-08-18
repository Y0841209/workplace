# ADR-0004: Microsoft Entra ID with OIDC Authentication

## Status
Accepted

## Context
Authentication requirements:
- Corporate identity provider already in place (Microsoft Entra ID)
- No local password storage (security policy)
- Single Sign-On with other corporate applications
- Rich user claims (department, job title, groups) for authorization
- Conditional Access, MFA, Identity Protection
- Support for web SPA (React) and API (.NET 8)

## Decision
Use **Microsoft Entra ID** as the sole identity provider with **OpenID Connect (OIDC) Authorization Code Flow + PKCE**.

### Architecture

```
┌─────────────┐     1. Redirect to /authorize      ┌──────────────┐
│   React     │ ──────────────────────────────────▶ │ Microsoft    │
│   SPA       │                                    │ Entra ID     │
│ (Public)    │ ◀────────────────────────────────── │ (Auth Code)  │
└─────────────┘     2. Return auth code             └──────────────┘
       │
       │ 3. POST /token (code + PKCE verifier)
       ▼
┌─────────────┐                                    ┌──────────────┐
│   .NET 8    │ ◀────────────────────────────────── │ Microsoft    │
│   API       │     4. Access Token + ID Token      │ Entra ID     │
│ (Confidential)                                       │ (Tokens)     │
└─────────────┘                                    └──────────────┘
       │
       │ 5. Validate JWT (JWKS from /keys)
       ▼
   ClaimsPrincipal
```

### Token Handling

| Token | Lifetime | Storage | Usage |
|-------|----------|---------|-------|
| Access Token | ~1 hour | Memory (Frontend) / HttpOnly Cookie (API) | API Authorization |
| Refresh Token | ~90 days | HttpOnly Secure Cookie (API only) | Token Renewal |
| ID Token | ~1 hour | Memory (Frontend) | User Info (name, email) |

**Frontend (SPA - Public Client)**:
- PKCE **mandatory** (RFC 7636)
- Tokens in memory only (no localStorage)
- Silent refresh via hidden iframe (if needed)

**Backend (Confidential Client)**:
- Client secret for token exchange
- HttpOnly Secure SameSite=Strict cookies for refresh tokens
- Validates access tokens via JWKS endpoint

### Claims Mapping

| Entra ID Claim | Application Property | Usage |
|----------------|---------------------|-------|
| `sub` / `oid` | `entra_object_id` | Unique user identifier (FK) |
| `preferred_username` / `email` | `email` | Login, notifications |
| `name` | `display_name` | UI display |
| `jobTitle` | `job_title` | Profile enrichment |
| `department` | `department` | Profile enrichment |
| `groups` | `roles` (via group mapping) | Authorization |

### Configuration

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "company.onmicrosoft.com",
    "TenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "ClientId": "yyyyyyyy-yyyy-yyyy-yyyy-yyyyyyyyyyyy",
    "ClientSecret": "********",  // Backend only
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-oidc"
  }
}
```

## Consequences

### Positive
- **Zero Credential Liability**: No passwords stored, hashed, or transmitted
- **Enterprise Security**: Conditional Access, MFA, PIM, Identity Protection inherited
- **Rich Claims**: Department, title, groups flow directly into authorization
- **SSO**: Seamless with Office 365, Teams, other corporate apps
- **Lifecycle Management**: User provisioning/deprovisioning via Entra ID (SCIM or manual sync)

### Negative
- **External Dependency**: Auth availability tied to Entra ID uptime
- **Token Validation Latency**: JWKS caching mitigates (5 min default)
- **Complexity**: PKCE, token refresh, silent auth flows
- **Vendor Lock-in**: Migration to other IdP requires code changes

### Neutral
- Requires Entra ID app registration (SPA + Web API)
- Group-based role mapping configured in Entra ID or application

## Alternatives Considered

1. **IdentityServer / Duende / Keycloak (Self-Hosted)**
   - Rejected: Operational overhead, duplicate user store, no corporate SSO benefit

2. **ASP.NET Core Identity (Local Accounts)**
   - Rejected: Violates "no local passwords" policy, no SSO, no enterprise features

3. **Auth0 / Okta / FusionAuth**
   - Rejected: Additional cost, Entra ID already provisioned and approved

## References
- [Microsoft Identity Platform (Entra ID) Docs](https://learn.microsoft.com/en-us/entra/identity-platform/)
- [OIDC Authorization Code + PKCE](https://datatracker.ietf.org/doc/html/rfc7636)
- [ASP.NET Core Authentication with Microsoft Entra ID](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/azure-active-directory)
- [SPA Best Practices](https://learn.microsoft.com/en-us/entra/identity-platform/spa-app-config)