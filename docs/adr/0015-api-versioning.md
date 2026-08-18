# ADR-0015: URL Path API Versioning

## Status
Accepted

## Context
API versioning requirements:
- Clear, explicit version in every request
- Support parallel versions during migration
- Cacheable (version in URL)
- Simple routing in Nginx
- Standard practice for REST APIs

## Decision
Use **URL Path Versioning**: `/api/v1/`, `/api/v2/`

### Implementation

```csharp
// Controller Attribute
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class ReservationsController : ControllerBase
{
    // ...
}

// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

### Versioning Strategy

| Version | Status | Support Until | Notes |
|---------|--------|---------------|-------|
| v1 | Current | TBD | Initial release |
| v2 | Planned | - | Breaking changes (if needed) |

### Deprecation Headers

```csharp
// Middleware or Filter
public class ApiVersionDeprecationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var apiVersion = context.HttpContext.GetRequestedApiVersion();
        if (apiVersion?.MajorVersion < 2) // v1 deprecated when v2 released
        {
            context.HttpContext.Response.Headers["Deprecation"] = "true";
            context.HttpContext.Response.Headers["Sunset"] = "Sat, 01 Jan 2027 00:00:00 GMT";
            context.HttpContext.Response.Headers["Link"] = 
                '<https://booking.company.com/api/v2/reservations>; rel="successor-version"';
        }
        await next();
    }
}
```

### Nginx Routing

```nginx
# Version-agnostic routing (all versions to same backend)
location /api/ {
    proxy_pass http://api_backend;
    # ...
}

# Or version-specific (if running parallel backends)
location /api/v1/ {
    proxy_pass http://api_v1_backend;
}
location /api/v2/ {
    proxy_pass http://api_v2_backend;
}
```

## Consequences

### Positive
- **Explicit**: Version visible in every request/log
- **Cacheable**: Different URLs = different cache keys
- **Simple**: No custom headers, works with all clients
- **Nginx Friendly**: Easy routing, rewriting, rate limiting per version
- **Standard**: Widely understood pattern

### Negative
- **URL Pollution**: Version in every path
- **Duplication**: Controllers/routes duplicated per version (mitigated by shared services)
- **Breaking Changes**: Require new version, old version maintained

### Neutral
- Alternative: Header-based (`Accept: application/vnd.booking.v1+json`) - rejected (less visible, caching issues)
- Alternative: Query string (`?version=1`) - rejected (not RESTful, caching issues)

## Alternatives Considered

1. **Header-Based Versioning** (`Accept-Version` or custom header)
   - Rejected: Invisible in logs, harder to cache, clients forget headers

2. **Query String Versioning** (`?v=1`)
   - Rejected: Not RESTful, can be stripped by proxies, caching issues

3. **No Versioning** (Eternal Compatibility)
   - Rejected: Unrealistic, breaking changes inevitable

## References
- [ASP.NET Core API Versioning](https://github.com/dotnet/aspnet-api-versioning)
- [Microsoft REST API Guidelines - Versioning](https://github.com/microsoft/api-guidelines/blob/vNext/Guidelines.md#versioning)
- [Semantic Versioning for APIs](https://semver.org/)