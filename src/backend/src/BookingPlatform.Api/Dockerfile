# Backend Dockerfile - Multi-stage Build
# Build: docker build -f src/backend/src/BookingPlatform.Api/Dockerfile -t booking-api .
# Context: repository root (../../..)

# ==========================================
# STAGE 1: Build (SDK)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Install required tools
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy solution and project files for layer caching
COPY src/backend/BookingPlatform.sln ./
COPY src/backend/src/BookingPlatform.Domain/BookingPlatform.Domain.csproj BookingPlatform.Domain/
COPY src/backend/src/BookingPlatform.Application/BookingPlatform.Application.csproj BookingPlatform.Application/
COPY src/backend/src/BookingPlatform.Infrastructure/BookingPlatform.Infrastructure.csproj BookingPlatform.Infrastructure/
COPY src/backend/src/BookingPlatform.Api/BookingPlatform.Api.csproj BookingPlatform.Api/

# Restore dependencies
RUN dotnet restore BookingPlatform.sln

# Copy source code
COPY src/backend/src/ ./ 

# Build and publish
WORKDIR /src/BookingPlatform.Api
RUN dotnet publish -c Release -o /app/publish --no-restore \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# ==========================================
# STAGE 2: Runtime (ASP.NET Runtime)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN adduser --disabled-password --gecos '' --uid 1000 appuser \
    && chown -R appuser:appuser /app
USER appuser

# Copy published app
COPY --from=build --chown=appuser:appuser /app/publish .

# Environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Health Check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

# Entry Point
ENTRYPOINT ["dotnet", "BookingPlatform.Api.dll"]