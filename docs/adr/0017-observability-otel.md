# ADR-0017: OpenTelemetry Observability Stack

## Status
Accepted

## Context
Observability requirements:
- Structured logging with correlation IDs
- Metrics for business and technical monitoring
- Distributed tracing across frontend → API → DB → Workers
- Alerting on errors, latency, business anomalies
- Vendor-neutral (avoid lock-in)

## Decision
Adopt **OpenTelemetry** as the observability framework with **Serilog** for logging, **Prometheus** for metrics, and **OTLP** export.

### Logging (Serilog + OpenTelemetry)

```csharp
// Program.cs
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "BookingPlatform.Api")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .Enrich.With<CorrelationIdEnricher>()
    .Enrich.With<SpanIdEnricher>()
    .Enrich.With<TraceIdEnricher>()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}",
        formatter: new JsonFormatter())
    .WriteTo.OpenTelemetry(options =>
    {
        options.Endpoint = builder.Configuration["Otlp:Endpoint"];
        options.Protocol = OtlpProtocol.Grpc;
    })
    .CreateLogger();

builder.Host.UseSerilog();
```

### Correlation ID Enricher

```csharp
public class CorrelationIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var correlationId = httpContext?.TraceIdentifier 
                         ?? httpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                         ?? Activity.Current?.TraceId.ToString();
        
        if (!string.IsNullOrEmpty(correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        }
    }
}
```

### Metrics (Prometheus)

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddProcessInstrumentation()
        .AddMeter("BookingPlatform.Api")
        .AddPrometheusExporter());

// Custom Business Metrics
public static class BusinessMetrics
{
    private static readonly Meter Meter = new("BookingPlatform.Api");
    
    public static readonly Counter<long> ReservationsCreated = Meter.CreateCounter<long>(
        "booking_reservations_created_total", 
        description: "Total reservations created");
    
    public static readonly Counter<long> ReservationsCancelled = Meter.CreateCounter<long>(
        "booking_reservations_cancelled_total");
    
    public static readonly Counter<long> CheckInsCompleted = Meter.CreateCounter<long>(
        "booking_checkins_completed_total");
    
    public static readonly Histogram<double> AvailabilitySearchDuration = Meter.CreateHistogram<double>(
        "booking_availability_search_seconds",
        unit: "s",
        description: "Time to search available resources");
    
    public static readonly Gauge<int> ActiveReservations = Meter.CreateGauge<int>(
        "booking_active_reservations",
        description: "Currently active reservations");
}

// Usage in Handler
public async Task<Result<ReservationDto>> Handle(CreateReservationCommand cmd, CancellationToken ct)
{
    using var activity = ActivitySource.StartActivity("CreateReservation");
    var stopwatch = Stopwatch.StartNew();
    
    try 
    {
        var result = await DoCreate(cmd, ct);
        BusinessMetrics.ReservationsCreated.Add(1, 
            new KeyValuePair<string, object?>("resource_type", cmd.ResourceTypeCode));
        return result;
    }
    finally
    {
        BusinessMetrics.AvailabilitySearchDuration.Record(stopwatch.Elapsed.TotalSeconds);
    }
}
```

### Tracing (OpenTelemetry)

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
            options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
            options.SetDbStatementForStoredProcedure = true;
        })
        .AddSource("BookingPlatform.Api")
        .AddOtlpExporter(options =>
        {
            options.Endpoint = builder.Configuration["Otlp:Endpoint"];
            options.Protocol = OtlpProtocol.Grpc;
        }));

// Custom Activity Source
public static class ActivitySources
{
    public static readonly ActivitySource BookingOperations = new("BookingPlatform.Api");
}

// Usage
using var activity = ActivitySources.BookingOperations.StartActivity("CheckAvailability", ActivityKind.Internal);
activity?.SetTag("resource_type", params.Type);
activity?.SetTag("date", params.Date.ToString());
activity?.SetTag("floor", params.FloorId?.ToString());
```

### Frontend Tracing

```typescript
// instrumentation.ts
import { registerInstrumentations } from '@opentelemetry/instrumentation';
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch';
import { DocumentLoadInstrumentation } from '@opentelemetry/instrumentation-document-load';
import { UserInteractionInstrumentation } from '@opentelemetry/instrumentation-user-interaction';
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web';
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http';
import { ZoneContextManager } from '@opentelemetry/context-zone';

const provider = new WebTracerProvider();
provider.addSpanProcessor(new SimpleSpanProcessor(
  new OTLPTraceExporter({ url: '/api/traces' }) // Proxied by Nginx to collector
));
provider.register({ contextManager: new ZoneContextManager() });

registerInstrumentations({
  instrumentations: [
    new FetchInstrumentation({
      propagateTraceHeaderCorsUrls: [/.*/],
      clearTimingResources: true,
    }),
    new DocumentLoadInstrumentation(),
    new UserInteractionInstrumentation(),
  ],
});
```

### Nginx → OTLP Collector

```nginx
# infrastructure/nginx/nginx.conf (add to http block)
location /api/traces {
    proxy_pass http://otel-collector:4318/v1/traces;
    proxy_http_version 1.1;
    proxy_set_header Content-Type application/json;
}

location /api/metrics {
    proxy_pass http://otel-collector:4318/v1/metrics;
}

location /api/logs {
    proxy_pass http://otel-collector:4318/v1/logs;
}
```

### Docker Compose - OTel Collector

```yaml
# docker-compose.observability.yml
services:
  otel-collector:
    image: otel/opentelemetry-collector-contrib:0.106
    command: ["--config=/etc/otelcol-contrib/config.yaml"]
    volumes:
      - ./infrastructure/otel/collector-config.yaml:/etc/otelcol-contrib/config.yaml
    ports:
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
      - "8889:8889"   # Prometheus metrics
    depends_on:
      - tempo
      - loki
      - prometheus

  tempo:
    image: grafana/tempo:2.3
    volumes:
      - ./infrastructure/tempo/tempo.yaml:/etc/tempo.yaml
    ports: ["3200:3200"]

  loki:
    image: grafana/loki:2.9
    volumes:
      - ./infrastructure/loki/loki.yaml:/etc/loki.yaml
    ports: ["3100:3100"]

  prometheus:
    image: prom/prometheus:v2.47
    volumes:
      - ./infrastructure/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
    ports: ["9090:9090"]

  grafana:
    image: grafana/grafana:10.1
    volumes:
      - ./infrastructure/grafana/dashboards:/etc/grafana/provisioning/dashboards
      - ./infrastructure/grafana/datasources:/etc/grafana/provisioning/datasources
    ports: ["3000:3000"]
    depends_on: [tempo, loki, prometheus]
```

## Consequences

### Positive
- **Vendor Neutral**: OTLP standard, switch backends (Tempo/Jaeger, Loki/Elastic, Prometheus/Thanos)
- **Full Stack**: Logs + Metrics + Traces correlated via TraceId
- **Auto-Instrumentation**: ASP.NET Core, HttpClient, EF Core zero-code
- **Business Metrics**: Custom counters/histograms for domain KPIs
- **Frontend Included**: Web tracing connected to backend traces

### Negative
- **Complexity**: Collector, multiple backends, configuration
- **Resource Usage**: Collector + Tempo + Loki + Prometheus + Grafana
- **Learning Curve**: OpenTelemetry concepts (spans, attributes, context propagation)
- **Sampling**: Need tail-based sampling for production volume

### Neutral
- Start with minimal: Console logging + Prometheus metrics only
- Add Tempo/Loki/Grafana incrementally
- Sampling: 100% for errors, 10% for success (configure in collector)

## Alternatives Considered

1. **ELK Stack (Elasticsearch, Logstash, Kibana)**
   - Rejected: Resource heavy, licensing changes, single vendor

2. **Datadog / New Relic / Dynatrace**
   - Rejected: Cost, vendor lock-in

3. **Custom Logging + Prometheus Only**
   - Rejected: No distributed tracing, harder debugging

## References
- [OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet)
- [OpenTelemetry JS](https://github.com/open-telemetry/opentelemetry-js)
- [OTel Collector](https://opentelemetry.io/docs/collector/)
- [Grafana Tempo/Loki/Prometheus](https://grafana.com/oss/)