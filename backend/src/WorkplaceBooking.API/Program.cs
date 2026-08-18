using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;
using WorkplaceBooking.Api.Authentication;
using WorkplaceBooking.Infrastructure;
using WorkplaceBooking.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "WorkplaceBooking.Api")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30));

// Add services
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Authentication & Authorization
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("Development")
        .AddScheme<AuthenticationSchemeOptions, DevelopmentAuthenticationHandler>("Development", null);
}
else
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["AzureAd:Instance"] + builder.Configuration["AzureAd:TenantId"];
            options.Audience = builder.Configuration["AzureAd:ClientId"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    // Claims are already validated, just log
                    return Task.CompletedTask;
                }
            };
        });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUser", policy => policy.RequireRole("USER"));
    options.AddPolicy("RequireRoomAdmin", policy => policy.RequireRole("ROOM_ADMIN"));
    options.AddPolicy("RequireSupport", policy => policy.RequireRole("SUPPORT"));
    options.AddPolicy("RequireGlobalAdmin", policy => policy.RequireRole("GLOBAL_ADMIN"));
    options.AddPolicy("CanReserveResource", policy => policy.RequireAssertion(context =>
        context.User.HasClaim("resource_type", "OPEN_WORKSPACE") ||
        context.User.HasClaim("resource_type", "CLOSED_OFFICE") ||
        context.User.HasClaim("resource_type", "MEETING_ROOM") ||
        context.User.IsInRole("GLOBAL_ADMIN") ||
        (context.User.IsInRole("ROOM_ADMIN") && context.User.HasClaim("resource_type", "MEETING_ROOM"))
    ));
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromSeconds(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    });

    options.AddFixedWindowLimiter("auth", options =>
    {
        options.PermitLimit = 10;
        options.Window = TimeSpan.FromMinutes(1);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 5;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddUrlGroup(new Uri("https://login.microsoftonline.com/"), name: "entra-id")
    .AddCheck<DiskSpaceHealthCheck>("disk-space");

builder.Services.AddHealthChecksUI()
    .AddInMemoryStorage();

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new Microsoft.AspNetCore.Mvc.Versioning.UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Workplace Booking Platform API",
        Version = "v1",
        Description = "API for workplace booking platform - offices, meeting rooms, and check-ins",
        Contact = new OpenApiContact
        {
            Name = "IT Department",
            Email = "it@company.com"
        }
    });

    // JWT Bearer authentication
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["https://localhost:3000"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
}

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Workplace Booking API v1");
        options.DisplayRequestDuration();
    });
    app.MapScalarApiReference(options =>
    {
        options.Title = "Workplace Booking API Reference";
        options.Theme = ScalarTheme.Mars;
    });
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("CorsPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = Microsoft.AspNetCore.Diagnostics.HealthChecks.UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecksUI();

app.Run();

// Health check for disk space
public class DiskSpaceHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var drive = new DriveInfo("/");
        var freeSpaceGB = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
        var totalSpaceGB = drive.TotalSize / (1024 * 1024 * 1024);
        var freePercentage = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;

        var status = freePercentage > 10
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
            : freePercentage > 5
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy;

        return Task.FromResult(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult(
            status,
            $"Free disk space: {freeSpaceGB:F1} GB / {totalSpaceGB:F1} GB ({freePercentage:F1}%)"));
    }
}