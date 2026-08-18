# ADR-0009: Multi-stage Docker Builds

## Status
Accepted

## Context
Containerization requirements:
- Small production images (security, transfer speed, startup time)
- Build-time dependencies excluded from runtime
- Consistent build across CI/CD and local
- Separate frontend (Node) and backend (.NET) build processes
- Nginx for frontend serving + reverse proxy

## Decision
Use **multi-stage Docker builds** for both frontend and backend.

### Backend Dockerfile

```dockerfile
# Stage 1: Build (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build
WORKDIR /src

# Copy project files for layer caching
COPY ["src/backend/src/BookingPlatform.Api/BookingPlatform.Api.csproj", "BookingPlatform.Api/"]
COPY ["src/backend/src/BookingPlatform.Application/BookingPlatform.Application.csproj", "BookingPlatform.Application/"]
COPY ["src/backend/src/BookingPlatform.Domain/BookingPlatform.Domain.csproj", "BookingPlatform.Domain/"]
COPY ["src/backend/src/BookingPlatform.Infrastructure/BookingPlatform.Infrastructure.csproj", "BookingPlatform.Infrastructure/"]

RUN dotnet restore "BookingPlatform.Api/BookingPlatform.Api.csproj"

# Copy source and build
COPY src/backend/src/ .
WORKDIR /src/BookingPlatform.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime (ASP.NET Runtime only - no SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim AS runtime
WORKDIR /app

# Non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health/live || exit 1

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "BookingPlatform.Api.dll"]
```

### Frontend Dockerfile

```dockerfile
# Stage 1: Build (Node)
FROM node:20-bookworm-slim AS build
WORKDIR /app

# Copy package files for layer caching
COPY src/frontend/package*.json ./
RUN npm ci --prefer-offline --no-audit --no-fund

# Copy source and build
COPY src/frontend/ .
RUN npm run build  # Outputs to /app/dist

# Stage 2: Runtime (Nginx)
FROM nginx:1.25-alpine AS runtime

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# Copy nginx config
COPY infrastructure/nginx/frontend.conf /etc/nginx/conf.d/default.conf

# Non-root (nginx runs as nginx user by default in alpine)
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

### Nginx Frontend Config (SPA Fallback)

```nginx
# infrastructure/nginx/frontend.conf
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    # Static assets - long cache
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }

    # SPA fallback - serve index.html for all routes
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy (handled by main nginx in docker-compose)
    # This stage only serves static files
}
```

### Docker Compose (Production)

```yaml
# docker-compose.yml
services:
  postgres:
    image: postgres:16-bookworm
    environment:
      POSTGRES_DB: booking
      POSTGRES_USER: ${DB_USER}
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./infrastructure/database/migrations:/docker-entrypoint-initdb.d
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER} -d booking"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: src/backend/src/BookingPlatform.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=booking;Username=${DB_USER};Password=${DB_PASSWORD}"
      AzureAd__ClientId: ${AZURE_CLIENT_ID}
      AzureAd__ClientSecret: ${AZURE_CLIENT_SECRET}
      AzureAd__TenantId: ${AZURE_TENANT_ID}
    depends_on:
      postgres:
        condition: service_healthy
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M

  frontend:
    build:
      context: .
      dockerfile: src/frontend/Dockerfile
    depends_on:
      - api

  nginx:
    image: nginx:1.25-alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./infrastructure/nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./infrastructure/nginx/conf.d:/etc/nginx/conf.d:ro
      - ./certbot/conf:/etc/letsencrypt:ro
      - ./certbot/www:/var/www/certbot:ro
    depends_on:
      - frontend
      - api
    deploy:
      resources:
        limits:
          memory: 64M

  worker:
    build:
      context: .
      dockerfile: src/backend/src/BookingPlatform.Api/Dockerfile
    command: ["dotnet", "BookingPlatform.Api.dll", "--worker"]
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=booking;Username=${DB_USER};Password=${DB_PASSWORD}"
    depends_on:
      postgres:
        condition: service_healthy
    deploy:
      resources:
        limits:
          memory: 256M

volumes:
  postgres_data:
```

## Consequences

### Positive
- **Small Images**: Runtime ~100MB (ASP.NET) / ~25MB (Nginx alpine)
- **Security**: No SDK, no source code, no build tools in production
- **Layer Caching**: Package restore cached separately from source changes
- **Non-Root**: Containers run as unprivileged users
- **Health Checks**: Built-in for orchestration readiness

### Negative
- **Build Time**: Two-stage builds slower than single-stage (mitigated by caching)
- **Complexity**: Multiple Dockerfiles, compose files
- **Debugging**: Harder to debug production image (use `docker run -it --entrypoint sh`)

### Neutral
- Requires `.dockerignore` to exclude `bin/`, `obj/`, `node_modules/`, `.git/`
- Environment variables via `.env` file (not committed)

## Alternatives Considered

1. **Single-Stage Build (SDK in Runtime)**
   - Rejected: ~1.5GB image, security risk, slower deploy

2. **Distroless / Scratch Base Images**
   - Rejected: Debugging difficulty, no shell, marginal size benefit over alpine

3. **Podman / Buildah**
   - Rejected: Docker standard in CI/CD, team familiarity

## References
- [Docker Multi-stage Builds](https://docs.docker.com/build/building/multi-stage/)
- [.NET Docker Images](https://github.com/dotnet/dotnet-docker)
- [Nginx Alpine](https://hub.docker.com/_/nginx)