# ADR-0010: Nginx Reverse Proxy with Security Hardening

## Status
Accepted

## Context
Production deployment requirements:
- TLS termination (HTTPS only)
- Route `/api/*` to backend, `/` to frontend
- Rate limiting (DDoS protection)
- Security headers (CSP, HSTS, etc.)
- Gzip/Brotli compression
- Static asset caching
- Health check endpoints accessible

## Decision
Use **Nginx** as the sole entry point with comprehensive security configuration.

### Main Nginx Configuration

```nginx
# infrastructure/nginx/nginx.conf
user nginx;
worker_processes auto;
error_log /var/log/nginx/error.log warn;
pid /var/run/nginx.pid;

events {
    worker_connections 1024;
    use epoll;
    multi_accept on;
}

http {
    include /etc/nginx/mime.types;
    default_type application/octet-stream;

    # Logging
    log_format main '$remote_addr - $remote_user [$time_local] "$request" '
                    '$status $body_bytes_sent "$http_referer" '
                    '"$http_user_agent" "$http_x_forwarded_for" '
                    'rt=$request_time uct="$upstream_connect_time" '
                    'uht="$upstream_header_time" urt="$upstream_response_time"';
    access_log /var/log/nginx/access.log main;

    # Performance
    sendfile on;
    tcp_nopush on;
    tcp_nodelay on;
    keepalive_timeout 65;
    types_hash_max_size 2048;
    server_tokens off;  # Hide version

    # Compression
    gzip on;
    gzip_vary on;
    gzip_min_length 1024;
    gzip_proxied any;
    gzip_comp_level 6;
    gzip_types text/plain text/css text/xml text/javascript application/javascript application/json application/xml application/rss+xml image/svg+xml;

    # Brotli (requires nginx built with --with-http_brotli_module)
    brotli on;
    brotli_vary on;
    brotli_comp_level 5;
    brotli_types text/plain text/css text/xml application/javascript application/json application/xml;

    # Rate Limiting Zones
    limit_req_zone $binary_remote_addr zone=api_limit:10m rate=100r/s;
    limit_req_zone $binary_remote_addr zone=auth_limit:10m rate=10r/s;
    limit_req_zone $binary_remote_addr zone=general_limit:10m rate=200r/s;

    # Upstreams
    upstream api_backend {
        server api:8080;
        keepalive 32;
    }

    upstream frontend_backend {
        server frontend:80;
        keepalive 32;
    }

    # HTTP → HTTPS Redirect
    server {
        listen 80;
        listen [::]:80;
        server_name booking.company.com;

        # Allow ACME challenge for Let's Encrypt
        location /.well-known/acme-challenge/ {
            root /var/www/certbot;
        }

        # Redirect all other traffic to HTTPS
        location / {
            return 301 https://$host$request_uri;
        }
    }

    # HTTPS Server
    server {
        listen 443 ssl http2;
        listen [::]:443 ssl http2;
        server_name booking.company.com;

        # SSL Configuration
        ssl_certificate /etc/letsencrypt/live/booking.company.com/fullchain.pem;
        ssl_certificate_key /etc/letsencrypt/live/booking.company.com/privkey.pem;
        ssl_trusted_certificate /etc/letsencrypt/live/booking.company.com/chain.pem;

        ssl_protocols TLSv1.2 TLSv1.3;
        ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
        ssl_prefer_server_ciphers off;
        ssl_session_cache shared:SSL:10m;
        ssl_session_timeout 10m;

        # OCSP Stapling
        ssl_stapling on;
        ssl_stapling_verify on;

        # Security Headers
        add_header Strict-Transport-Security "max-age=31536000; includeSubDomains; preload" always;
        add_header X-Frame-Options "DENY" always;
        add_header X-Content-Type-Options "nosniff" always;
        add_header Referrer-Policy "strict-origin-when-cross-origin" always;
        add_header Permissions-Policy "geolocation=(), microphone=(), camera=()" always;
        
        # CSP - Adjust for Material UI inline styles
        add_header Content-Security-Policy "
            default-src 'self';
            script-src 'self' 'unsafe-inline' 'unsafe-eval';
            style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
            font-src 'self' https://fonts.gstatic.com data:;
            img-src 'self' data: https:;
            connect-src 'self' https://booking.company.com;
            frame-ancestors 'none';
            base-uri 'self';
            form-action 'self';
        " always;

        # Rate Limiting
        limit_req zone=general_limit burst=200 nodelay;

        # Frontend (SPA)
        location / {
            proxy_pass http://frontend_backend;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_cache_bypass $http_upgrade;
            
            # SPA fallback handled by frontend nginx
        }

        # API
        location /api/ {
            limit_req zone=api_limit burst=50 nodelay;
            
            proxy_pass http://api_backend;
            proxy_http_version 1.1;
            proxy_set_header Host $host;
            proxy_set_header X-Real-IP $remote_addr;
            proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
            proxy_set_header X-Forwarded-Proto $scheme;
            proxy_set_header X-Correlation-ID $request_id;
            
            # Timeouts
            proxy_connect_timeout 5s;
            proxy_send_timeout 30s;
            proxy_read_timeout 30s;
            
            # Buffering
            proxy_buffering on;
            proxy_buffer_size 4k;
            proxy_buffers 8 4k;
        }

        # Auth endpoints - stricter rate limit
        location ~ ^/api/v1/auth/ {
            limit_req zone=auth_limit burst=20 nodelay;
            proxy_pass http://api_backend;
            # ... same proxy settings
        }

        # Health Checks (no rate limit)
        location /health {
            access_log off;
            proxy_pass http://api_backend;
            # ... proxy settings
        }

        # Static Assets (served by frontend nginx, but proxy here for simplicity)
        location /assets/ {
            proxy_pass http://frontend_backend;
            expires 1y;
            add_header Cache-Control "public, immutable";
        }
    }
}
```

### Frontend Nginx (SPA Fallback)

```nginx
# infrastructure/nginx/frontend.conf
server {
    listen 80;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    # Security Headers (defense in depth)
    add_header X-Frame-Options "DENY" always;
    add_header X-Content-Type-Options "nosniff" always;

    # Static Assets - Long Cache
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|map)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
        try_files $uri =404;
    }

    # SPA Fallback
    location / {
        try_files $uri $uri/ /index.html;
    }

    # Health
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }
}
```

## Consequences

### Positive
- **Single Entry Point**: All traffic through Nginx (security, logging, routing)
- **TLS Termination**: Backend/frontend receive plain HTTP internally
- **Rate Limiting**: Two-tier (Nginx + ASP.NET Core) defense in depth
- **Security Headers**: Comprehensive CSP, HSTS, frame options
- **Compression**: Gzip + Brotli for ~70% size reduction
- **Caching**: Static assets cached 1 year (hashed filenames)
- **Observability**: Rich access logs with upstream timing

### Negative
- **Complexity**: Nginx config requires expertise
- **CSP Tuning**: Material UI uses inline styles (`'unsafe-inline'`)
- **Certificate Management**: Let's Encrypt automation needed (certbot)
- **Single Point of Failure**: Nginx down = everything down (mitigate with HA setup)

### Neutral
- Internal network: HTTP between Nginx → API/Frontend (isolated VPC)
- Correlation ID propagation for distributed tracing

## Alternatives Considered

1. **Azure Application Gateway / AWS ALB**
   - Rejected: Cost, vendor lock-in, less control over config

2. **Traefik / Caddy**
   - Rejected: Nginx team expertise, mature, ubiquitous

3. **ASP.NET Core Kestrel Direct (No Reverse Proxy)**
   - Rejected: Not recommended for production (no TLS termination, no rate limiting, no static files)

## References
- [Nginx Security Headers](https://github.com/h5bp/server-configs-nginx)
- [Mozilla SSL Configuration Generator](https://ssl-config.mozilla.org/)
- [OWASP Secure Headers](https://owasp.org/www-project-secure-headers/)