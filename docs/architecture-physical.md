# Arquitectura Física - Workplace Booking Platform

## 1. Arquitectura Física

### Topología de Despliegue

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         UBUNTU 24.04 LTS (VM / Bare Metal)                  │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                        DOCKER ENGINE                                │   │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ │   │
│  │  │  Nginx   │ │ Frontend │ │  Backend │ │ Worker   │ │PostgreSQL│ │   │
│  │  │ (Proxy)  │ │ (Static) │ │  (API)   │ │(Hangfire)│ │   16     │ │   │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ │   │
│  │       │           │           │            │            │         │   │
│  │       └───────────┼───────────┼────────────┼────────────┘         │   │
│  │                   ▼           ▼            ▼                      │   │
│  │            ┌─────────────────────────────────────┐               │   │
│  │            │         Docker Network              │               │   │
│  │            │        (booking-network)            │               │   │
│  │            └─────────────────────────────────────┘               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                    │                                        │
│                    ┌───────────────┼───────────────┐                       │
│                    ▼               ▼               ▼                       │
│             ┌────────────┐  ┌────────────┐  ┌────────────┐               │
│             │  Volumen   │  │  Volumen   │  │  Volumen   │               │
│             │  postgres  │  │  certbot   │  │   logs     │               │
│             │  _data     │  │  _conf     │  │  _nginx    │               │
│             └────────────┘  └────────────┘  └────────────┘               │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
       ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
       │ Microsoft   │ │  Email      │ │   Power BI  │
       │ Entra ID    │ │  Provider   │ │  (Cloud)    │
       │  (Cloud)    │ │  (SMTP/API) │ │             │
       └─────────────┘ └─────────────┘ └─────────────┘
```

### Especificaciones de Hardware Mínimo (VM)

| Recurso | Mínimo | Recomendado | Notas |
|---------|--------|-------------|-------|
| vCPU | 4 | 8 | Backend + Worker + DB comparten |
| RAM | 8 GB | 16 GB | PostgreSQL: 4-6 GB, Backend: 1-2 GB, Worker: 512 MB |
| Disco | 50 GB SSD | 100 GB NVMe | DB + logs + backups + imágenes Docker |
| Red | 1 Gbps | 1 Gbps | Latencia < 5 ms a Entra ID / Email |

---

## 2. Contenedores Docker

### Servicios Definidos en docker-compose.yml

| Servicio | Imagen Base | Puerto Interno | Recursos | Dependencias |
|----------|-------------|----------------|----------|--------------|
| **nginx** | `nginx:1.27-alpine` | 80, 443 | CPU: 0.25, Mem: 128 MB | frontend, api |
| **frontend** | `nginx:1.27-alpine` (multi-stage) | 80 | CPU: 0.1, Mem: 64 MB | - |
| **api** | `mcr.microsoft.com/dotnet/aspnet:8.0` | 8080 | CPU: 1.0, Mem: 1 GB | postgres |
| **worker** | `mcr.microsoft.com/dotnet/aspnet:8.0` | - | CPU: 0.5, Mem: 512 MB | postgres |
| **postgres** | `postgres:16-alpine` | 5432 | CPU: 2.0, Mem: 4 GB | - |

### Redes Docker

| Red | Driver | Servicios | Propósito |
|-----|--------|-----------|-----------|
| `booking-frontend` | bridge | nginx, frontend | Tráfico estático / SPA |
| `booking-backend` | bridge | nginx, api, worker, postgres | API, workers, BD |
| `booking-monitoring` | bridge | nginx, api, worker, prometheus (opcional) | Métricas / health |

### Volúmenes Persistentes

| Volumen | Servicio | Ruta Contenedor | Backup |
|---------|----------|-----------------|--------|
| `postgres_data` | postgres | `/var/lib/postgresql/data` | Diario (pg_dump + WAL-G) |
| `nginx_certs` | nginx | `/etc/letsencrypt` | Certificados TLS |
| `nginx_logs` | nginx | `/var/log/nginx` | Rotación semanal |
| `api_logs` | api, worker | `/app/logs` | Rotación diaria |

### Variables de Entorno Críticas (`.env`)

```env
# Base de datos
POSTGRES_DB=booking
POSTGRES_USER=booking_user
POSTGRES_PASSWORD=<strong-random>
POSTGRES_MAX_CONNECTIONS=200

# Backend
ASPNETCORE_ENVIRONMENT=Production
CONNECTION_STRING=Host=postgres;Database=booking;Username=booking_user;Password=${POSTGRES_PASSWORD}
JWT_AUTHORITY=https://login.microsoftonline.com/{tenant-id}/v2.0
JWT_AUDIENCE=api://{client-id}
ALLOWED_ORIGINS=https://booking.empresa.com

# Entra ID
AZURE_TENANT_ID=<tenant-guid>
AZURE_CLIENT_ID=<app-reg-guid>
AZURE_CLIENT_SECRET=<secret>

# Email
SMTP_HOST=smtp.office365.com
SMTP_PORT=587
SMTP_USER=noreply@empresa.com
SMTP_PASS=<app-password>
EMAIL_FROM=noreply@empresa.com

# Worker (Hangfire)
HANGFIRE_CONNECTION=Host=postgres;Database=booking;Username=booking_user;Password=${POSTGRES_PASSWORD}
HANGFIRE_DASHBOARD_USER=admin
HANGFIRE_DASHBOARD_PASS=<strong-pass>

# Nginx
SERVER_NAME=booking.empresa.com
CERTBOT_EMAIL=admin@empresa.com
```

### Health Checks

| Servicio | Endpoint | Intervalo | Timeout | Retries |
|----------|----------|-----------|---------|---------|
| nginx | `GET /health` | 30s | 5s | 3 |
| api | `GET /health/ready` | 30s | 10s | 3 |
| worker | Hangfire heartbeat | 30s | 10s | 3 |
| postgres | `pg_isready` | 10s | 5s | 5 |

---

## 3. Nginx

### Rol
- Reverse proxy único (edge)
- Terminación TLS (Let's Encrypt / Certbot)
- Rate limiting por IP y por endpoint
- Compresión (gzip / brotli)
- Headers de seguridad (CSP, HSTS, X-Frame-Options, etc.)
- Servir assets estáticos del frontend (cache largo)
- Proxy a API (`/api/`) y Worker (Hangfire dashboard `/hangfire/`)

### Configuración Principal (`nginx.conf`)

```nginx
# Upstreams
upstream api_backend {
    server api:8080;
    keepalive 32;
}

upstream frontend_backend {
    server frontend:80;
    keepalive 16;
}

# Rate limiting zones
limit_req_zone $binary_remote_addr zone=api_limit:10m rate=100r/s;
limit_req_zone $binary_remote_addr zone=auth_limit:10m rate=10r/s;
limit_req_zone $binary_remote_addr zone=qr_limit:10m rate=30r/s;

# HTTP → HTTPS redirect
server {
    listen 80;
    server_name booking.empresa.com;
    location /.well-known/acme-challenge/ { root /var/www/certbot; }
    location / { return 301 https://$host$request_uri; }
}

# HTTPS Server
server {
    listen 443 ssl http2;
    server_name booking.empresa.com;

    ssl_certificate /etc/letsencrypt/live/booking.empresa.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/booking.empresa.com/privkey.pem;
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers HIGH:!aNULL:!MD5;

    # Security headers
    add_header Strict-Transport-Security "max-age=31536000; includeSubDomains" always;
    add_header X-Frame-Options DENY always;
    add_header X-Content-Type-Options nosniff always;
    add_header Referrer-Policy strict-origin-when-cross-origin always;
    add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com data:; img-src 'self' data: https:; connect-src 'self'; frame-ancestors 'none';" always;

    # Frontend (SPA)
    location / {
        proxy_pass http://frontend_backend;
        proxy_cache_valid 200 1y;
        add_header Cache-Control "public, max-age=31536000, immutable";
    }

    # API
    location /api/ {
        limit_req zone=api_limit burst=20 nodelay;
        proxy_pass http://api_backend;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
    }

    # Auth endpoints - stricter
    location ~ ^/api/v1/auth/ {
        limit_req zone=auth_limit burst=5 nodelay;
        proxy_pass http://api_backend;
    }

    # QR check-in - specific limit
    location ~ ^/api/v1/check-in/ {
        limit_req zone=qr_limit burst=10 nodelay;
        proxy_pass http://api_backend;
    }

    # Hangfire Dashboard (protected by auth policy)
    location /hangfire/ {
        limit_req zone=auth_limit burst=5 nodelay;
        proxy_pass http://api_backend/hangfire/;
    }

    # Health
    location /health { access_log off; proxy_pass http://api_backend/health; }
}
```

### Renovación TLS (Certbot)
- Contenedor `certbot` en modo daemon
- Renovación automática cada 12h
- Hook `nginx -s reload` tras renovación exitosa
- Certificados en volumen `nginx_certs` compartido

---

## 4. PostgreSQL 16

### Configuración del Servidor (`postgresql.conf`)

```ini
# Conexiones
max_connections = 200
superuser_reserved_connections = 3

# Memoria
shared_buffers = 2GB
effective_cache_size = 6GB
work_mem = 16MB
maintenance_work_mem = 512MB
random_page_cost = 1.1

# Paralelismo
max_worker_processes = 8
max_parallel_workers_per_gather = 4
max_parallel_workers = 8

# WAL / Checkpoint
wal_level = replica
max_wal_size = 4GB
min_wal_size = 1GB
checkpoint_completion_target = 0.9
wal_buffers = 64MB

# Logging
log_destination = 'stderr'
logging_collector = on
log_directory = '/var/log/postgresql'
log_filename = 'postgresql-%Y-%m-%d.log'
log_statement = 'ddl'
log_min_duration_statement = 1000
log_line_prefix = '%t [%p]: [%l-1] user=%u,db=%d,app=%a,client=%h '

# Autovacuum
autovacuum = on
autovacuum_max_workers = 4
autovacuum_naptime = 30s
autovacuum_vacuum_threshold = 50
autovacuum_analyze_threshold = 50
```

### Esquema y Objetos (según FRD Anexo A)
- Esquema: `booking`
- 15 tablas principales + índices + constraints + triggers + funciones
- Extensiones: `pgcrypto`, `btree_gist`, `citext`
- Tipos enum: `reservation_status`, `notification_status`, `notification_type`, `checkin_method`

### Mantenimiento
| Tarea | Frecuencia | Herramienta |
|-------|------------|-------------|
| Backup lógico (pg_dump) | Diario 02:00 | pg_dump + compresión |
| Backup físico (WAL-G) | Continuo | WAL-G a S3/MinIO |
| Vacuum/Analyze | Automático | autovacuum |
| Reindex | Semanal | `REINDEX CONCURRENTLY` |
| Statistics | Diario | `ANALYZE` |

### Conexiones por Servicio
| Servicio | Pool Size | Max Overflow | Timeout |
|----------|-----------|--------------|---------|
| API (backend) | 20 | 10 | 30s |
| Worker (Hangfire) | 10 | 5 | 30s |
| Migrations (CI/CD) | 1 | 0 | 60s |

---

## 5. Integración Microsoft Entra ID

### Registro de Aplicación (App Registration)

| Configuración | Valor |
|---------------|-------|
| Nombre | Workplace Booking Platform |
| Tipos de cuenta soportadas | Solo este directorio organizativo (Single tenant) |
| URI de redirección (SPA) | `https://booking.empresa.com/auth/callback` |
| URI de redirección (Logout) | `https://booking.empresa.com/` |
| Concesiones implícitas | Access tokens ✓, ID tokens ✓ |
| Permisos API | `User.Read`, `email`, `profile`, `openid` |
| Secretos de cliente | 1 secreto (rotación 180 días) |
| Certificados | Opcional (para mayor seguridad) |

### Configuración OIDC

| Parámetro | Valor |
|-----------|-------|
| Authority | `https://login.microsoftonline.com/{tenant-id}/v2.0` |
| Client ID | `{app-reg-client-id}` |
| Response Type | `code` |
| Response Mode | `query` |
| Scope | `openid profile email offline_access` |
| PKCE | `S256` (obligatorio) |
| Nonce | Generado por frontend (crypto.randomUUID) |
| State | Generado por frontend (crypto.randomUUID) |

### Claims Recibidos y Mapeo

| Claim Entra ID | Uso en Sistema |
|----------------|----------------|
| `sub` / `oid` | `entra_object_id` (PK usuario) |
| `preferred_username` / `email` | `email` (unique, citext) |
| `name` | `display_name` |
| `jobTitle` | `job_title` (enriquecimiento) |
| `department` | `department` (enriquecimiento) |
| `groups` (IDs) | Mapeo a `application_roles` via tabla de mapping o app roles |

### Validación en Backend
- Middleware `JwtBearer` con `TokenValidationParameters`:
  - `ValidateIssuer = true` (Authority)
  - `ValidateAudience = true` (Client ID como audience)
  - `ValidateLifetime = true`
  - `ValidateIssuerSigningKey = true` (JWKS de `/.well-known/openid-configuration`)
  - `ClockSkew = TimeSpan.Zero`
- Cache de claves JWKS (5 min default)
- Revocación: short-lived access tokens (60-90 min), refresh token rotation

### Conditional Access (Recomendado)
- Requerir MFA para todos los usuarios
- Bloquear acceso desde países no autorizados
- Requerir dispositivo conforme (Intune) opcional
- Riesgo de inicio de sesión: bloquear alto riesgo

---

## 6. Power BI

### Modelo de Conexión
- **Modo**: DirectQuery (lectura en tiempo real)
- **Gateway**: On-premises Data Gateway (en misma VM o VM separada)
- **Autenticación**: Service Principal (App Registration dedicado) o Basic (usuario servicio)

### App Registration para Power BI
| Configuración | Valor |
|---------------|-------|
| Nombre | Workplace Booking - Power BI |
| Permisos API | Ninguno (solo BD) |
| Cliente confidencial | Sí |
| Secreto | Rotación 180 días |

### Conjunto de Datos (Dataset)

| Tabla / Vista | Descripción | Actualización |
|---------------|-------------|---------------|
| `v_resource_utilization` | Ocupación por recurso/día/hora | Tiempo real |
| `v_user_reservations` | Reservas por usuario/perfil/fecha | Tiempo real |
| `v_occupancy_by_floor` | % ocupación por piso/hora | Tiempo real |
| `v_no_show_rate` | No-shows por perfil/semana | Tiempo real |
| `v_booking_conflicts` | Intentos fallidos por conflicto | Tiempo real |

### Medidas DAX Clave

```dax
-- Tasa de ocupación
Occupancy Rate = 
DIVIDE(
    COUNTROWS(FILTER(v_resource_utilization, [status] IN {"CHECKED_IN","CONFIRMED"})),
    COUNTROWS(v_resource_utilization)
)

-- No-show rate
NoShow Rate = 
DIVIDE(
    COUNTROWS(FILTER(v_user_reservations, [status] = "NOT_CHECKED_IN")),
    COUNTROWS(v_user_reservations)
)

-- Promedio horas reservadas por usuario/semana
Avg Hours Per User Week = 
AVERAGEX(
    VALUES(v_user_reservations[user_id]),
    CALCULATE(SUMX(v_user_reservations, DATEDIFF([start_time], [end_time], MINUTE))/60)
)
```

### Informes (Reports)
| Informe | Audiencia | Páginas |
|---------|-----------|---------|
| **Dashboard Ejecutivo** | Dirección | KPIs: ocupación global, no-show, top usuarios |
| **Uso por Piso/Zona** | Facility Management | Heatmaps horarios, capacidad libre |
| **Análisis de Perfiles** | RR.HH. / Administración | Reservas por perfil, límites, excepciones |
| **Auditoría de Conflictos** | Seguridad / TI | Intentos fallidos, patrones anómalos |

### Seguridad
- Row-Level Security (RLS) en Power BI por `entra_object_id` / grupo
- Solo GLOBAL_ADMIN y roles designados acceden a informes
- Datos sensibles (emails, IPs) excluidos del modelo semántico

### Actualización
- DirectQuery: sin caché, consulta directa a PostgreSQL
- Gateway configurado con Service Principal
- Timeouts: 120s consulta, 300s conexión