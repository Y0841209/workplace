# Backend Dockerfile - Multi-stage Build
# Usage: docker build -f backend/src/WorkplaceBooking.API/Dockerfile -t booking-api .
# Build context MUST be the repository root (contains backend/ and infrastructure/)

# ==========================================
# STAGE 1: Build (SDK)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Copy solution and project files for layer caching.
# The copy targets reproduce the repo's relative layout (backend/src/...)
# so that `dotnet restore WorkplaceBooking.sln` resolves every reference.
COPY backend/WorkplaceBooking.sln ./
COPY backend/src/WorkplaceBooking.Domain/*.csproj src/WorkplaceBooking.Domain/
COPY backend/src/WorkplaceBooking.SharedKernel/*.csproj src/WorkplaceBooking.SharedKernel/
COPY backend/src/WorkplaceBooking.Application/*.csproj src/WorkplaceBooking.Application/
COPY backend/src/WorkplaceBooking.Infrastructure/*.csproj src/WorkplaceBooking.Infrastructure/
COPY backend/src/WorkplaceBooking.API/*.csproj src/WorkplaceBooking.API/
COPY backend/tests/WorkplaceBooking.Infrastructure.Tests/*.csproj tests/WorkplaceBooking.Infrastructure.Tests/

# Restore dependencies
RUN dotnet restore "WorkplaceBooking.sln"

# Copy source code
COPY backend/src/ src/
COPY backend/tests/ tests/

# Build and publish
WORKDIR /src/src/WorkplaceBooking.API
RUN dotnet publish -c Release -o /app/publish --no-restore

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
ENTRYPOINT ["dotnet", "WorkplaceBooking.Api.dll"]
