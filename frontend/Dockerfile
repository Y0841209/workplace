# Frontend Dockerfile - Multi-stage Build
# Usage: docker build -f frontend/Dockerfile -t booking-frontend .
# Build context MUST be the repository root (contains frontend/ and infrastructure/)

# ==========================================
# STAGE 1: Build (Node.js)
# ==========================================
FROM node:20-alpine AS build
WORKDIR /app

# Install build dependencies
RUN apk add --no-cache python3 make g++

# Copy package files for layer caching
COPY frontend/package*.json ./

# Install dependencies
# TODO: commit package-lock.json and switch back to `npm ci`
RUN npm install --prefer-offline --no-audit --no-fund

# Copy source code
COPY frontend/ .

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

EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
