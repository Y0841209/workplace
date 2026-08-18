# Frontend Dockerfile - Multi-stage Build
# Build: docker build -f src/frontend/Dockerfile -t booking-frontend .
# Context: repository root (../..)

# ==========================================
# STAGE 1: Build (Node.js)
# ==========================================
FROM node:20-bookworm-slim AS build
WORKDIR /app

# Install build dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    python3 \
    make \
    g++ \
    && rm -rf /var/lib/apt/lists/*

# Copy package files for layer caching
COPY src/frontend/package*.json ./

# Install dependencies
RUN npm ci --prefer-offline --no-audit --no-fund

# Copy source code
COPY src/frontend/ .

# Build application
ARG VITE_API_BASE_URL=/api/v1
ARG VITE_APP_TITLE=Workplace Booking
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_APP_TITLE=${VITE_APP_TITLE}
RUN npm run build

# ==========================================
# STAGE 2: Runtime (Nginx)
# ==========================================
FROM nginx:1.25-alpine AS runtime

# Copy built assets
COPY --from=build /app/dist /usr/share/nginx/html

# Copy nginx configuration
COPY infrastructure/nginx/frontend.conf /etc/nginx/conf.d/default.conf

# Create non-root user (nginx user already exists in alpine)
# RUN adduser -D -H -u 1000 -s /sbin/nologin appuser

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]