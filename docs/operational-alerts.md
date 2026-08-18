# Workplace Booking Platform - Operational Alerts

## Alert Severity Definitions

| Severidad | SLA Respuesta | Escalación | Canales |
|-----------|---------------|------------|---------|
| **Crítica (P0)** | 5 min | Inmediata → On-call + Team Lead | PagerDuty + Slack + Email |
| **Alta (P1)** | 15 min | 30 min → Team Lead | PagerDuty + Slack |
| **Media (P2)** | 1 hora | 4 horas → Team | Slack + Email |

---

## 1. API Alertas

### Críticas (P0)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **API Down** | `up{job="api"} == 0` | `== 0` por 1m | API completamente caída |
| **API Latency P95** | `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 1` | `> 1s` por 5m | Latencia P95 > 1s |
| **Error Rate 5xx** | `rate(http_requests_total{status=~"5.."}[5m]) > 0.01` | `> 1%` por 5m | Tasa errores 5xx > 1% |
| **High Error Rate 4xx** | `rate(http_requests_total{status=~"4.."}[5m]) > 0.05` | `> 5%` por 5m | Tasa errores 4xx > 5% |
| **In-Flight Requests** | `http_requests_in_flight > 200` | `> 200` por 5m | Demasiadas requests concurrentes |

### Altas (P1)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **API Latency P95 High** | `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 0.5` | `> 500ms` por 5m | Latencia P95 > 500ms |
| **High Error Rate 4xx** | `rate(http_requests_total{status=~"4.."}[5m]) > 0.05` | `> 5%` por 5m | Tasa 4xx > 5% |
| **Auth Failure Rate** | `rate(auth_attempts_total{result="failure"}[5m]) > 0.1` | `> 10%` por 5m | Fallos auth > 10% |
| **Rate Limit Denied** | `rate(rate_limit_hits_total{result="denied"}[5m]) > 0.1` | `> 10%` por 5m | Rate limit denegando > 10% |
| **High In-Flight Requests** | `http_requests_in_flight > 150` | `> 150` por 5m | Requests concurrentes altos |
| **Slow Health Check** | `health_check_duration_seconds > 5` | `> 5s` por 5m | Health check lento |

### Medias (P2)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **API Latency P95 Elevated** | `histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 0.3` | `> 300ms` por 10m | Latencia P95 elevada |
| **High 4xx Rate** | `rate(http_requests_total{status=~"4.."}[5m]) > 0.02` | `> 2%` por 10m | Tasa 4xx > 2% |
| **Auth Failure Rate Elevated** | `rate(auth_attempts_total{result="failure"}[5m]) > 0.05` | `> 5%` por 10m | Fallos auth elevados |
| **Rate Limit Denied Elevated** | `rate(rate_limit_hits_total{result="denied"}[5m]) > 0.05` | `> 5%` por 10m | Rate limit denegado elevado |
| **Slow Health Check** | `health_check_duration_seconds > 2` | `> 2s` por 10m | Health check lento |

---

## 2. PostgreSQL Alerts

### Críticas (P0)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **PostgreSQL Down** | `pg_up == 0` | `== 0` por 1m | PostgreSQL caído |
| **Connection Exhaustion** | `pg_connections_active / pg_settings_max_connections > 0.9` | `> 90%` por 5m | Conexiones > 90% |
| **Connection Exhaustion Critical** | `pg_connections_active / pg_settings_max_connections > 0.95` | `> 95%` por 1m | Conexiones > 95% |
| **Deadlocks** | `increase(pg_deadlocks_total[5m]) > 0` | `> 0` por 5m | Deadlocks detectados |
| **Replication Lag Critical** | `pg_wal_lag_seconds > 300` | `> 5min` por 5m | Replication lag > 5min |

### Altas (P1)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **Connection Pool High** | `pg_connections_active / pg_settings_max_connections > 0.8` | `> 80%` por 5m | Pool conexiones > 80% |
| **Replication Lag High** | `pg_wal_lag_seconds > 60` | `> 60s` por 5m | Replication lag > 1min |
| **Deadlocks Detected** | `increase(pg_deadlocks_total[5m]) > 0` | `> 0` por 10m | Deadlocks detectados |
| **High Rollback Rate** | `rate(pg_transactions_rolled_back_total[5m]) / rate(pg_transactions_committed_total[5m]) > 0.05` | `> 5%` por 10m | Rollback rate > 5% |
| **High Lock Waits** | `pg_locks_waiting > 10` | `> 10` por 5m | Locks en espera |
| **Blocking Queries** | `pg_blocking_pids > 0` | `> 0` por 5m | Queries bloqueando |
| **High Rollback Rate** | `rate(pg_transactions_rolled_back_total[5m]) / rate(pg_transactions_committed_total[5m]) > 0.01` | `> 1%` por 5m | Rollback rate > 1% |

### Medias (P2)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **Connection Pool Elevated** | `pg_connections_active / pg_settings_max_connections > 0.7` | `> 70%` por 15m | Pool conexiones > 70% |
| **Replication Lag Elevated** | `pg_wal_lag_seconds > 30` | `> 30s` por 15m | Replication lag > 30s |
| **Slow Queries** | `pg_slow_queries_total > 10` | `> 10` por 15m | Queries lentas > threshold |
| **High Table Bloat** | `pg_bloat_ratio > 1.5` | `> 1.5` por 1h | Bloat ratio alto |
| **High Dead Tuples** | `pg_dead_tuples / pg_live_tuples > 0.2` | `> 20%` por 1h | Dead tuples ratio alto |
| **Autovacuum Not Running** | `pg_autovacuum_runs_total == 0` | `== 0` por 6h | Autovacuum no ejecutándose |
| **WAL Lag Growing** | `rate(pg_wal_lag_seconds[5m]) > 0` | `> 0` por 15m | WAL lag creciendo |
| **Replication Slot Inactive** | `pg_replication_slots_active < expected` | `< expected` por 1h | Slot replicación inactivo |

---

## 3. Docker / Container Alerts

### Críticas (P0)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **Container Down** | `container_status{phase="running"} == 0` | `== 0` por 1m | Contenedor caído |
| **Container OOM Killed** | `container_last_termination_reason == "OOMKilled"` | `== 1` por 1m | OOM Killed |
| **Container Crash Loop** | `rate(container_restarts_total[15m]) > 0.1` | `> 0.1/min` por 5m | Crash loop detectado |

### Altas (P1)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **High CPU Usage** | `container_cpu_usage_seconds_per_second / container_spec_cpu_quota / container_spec_cpu_period > 0.85` | `> 85%` por 5m | CPU > 85% |
| **High Memory Usage** | `container_memory_usage_bytes / container_spec_memory_limit_bytes > 0.85` | `> 85%` por 5m | Memoria > 85% |
| **Memory Limit Near** | `container_memory_usage_bytes / container_spec_memory_limit_bytes > 0.9` | `> 90%` por 5m | Memoria > 90% límite |
| **High Restart Rate** | `rate(container_restarts_total[15m]) > 0.05` | `> 0.05/min` por 10m | Reinicios frecuentes |
| **Container Not Running** | `container_status{phase="running"} == 0` | `== 0` por 5m | Contenedor no running |

### Medias (P2)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **CPU Elevated** | `container_cpu_usage_seconds_per_second / container_spec_cpu_quota / container_spec_cpu_period > 0.7` | `> 70%` por 15m | CPU elevado |
| **Memory Elevated** | `container_memory_usage_bytes / container_spec_memory_limit_bytes > 0.7` | `> 70%` por 15m | Memoria elevada |
| **High Restart Rate** | `rate(container_restarts_total[1h]) > 0.02` | `> 0.02/min` por 30m | Reinicios frecuentes |
| **Disk Usage High** | `container_fs_usage_bytes / container_fs_limit_bytes > 0.8` | `> 80%` por 1h | Disco contenedor alto |

---

## 4. Nginx Alerts

### Críticas (P0)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **Nginx Down** | `up{job="nginx"} == 0` | `== 0` por 1m | Nginx caído |
| **High 5xx Rate** | `rate(nginx_http_requests_total{status=~"5.."}[5m]) / rate(nginx_http_requests_total[5m]) > 0.05` | `> 5%` por 5m | 5xx > 5% |
| **SSL Cert Expiring Soon** | `ssl_cert_expiry_timestamp - time() < 86400 * 30` | `< 30 días` | Certificado expira en 30 días |

### Altas (P1)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **High Request Latency** | `histogram_quantile(0.95, rate(nginx_http_request_duration_seconds_bucket[5m])) > 1` | `> 1s` por 5m | Latencia P95 > 1s |
| **High Error Rate 4xx** | `rate(nginx_http_requests_total{status=~"4.."}[5m]) / rate(nginx_http_requests_total[5m]) > 0.1` | `> 10%` por 5m | 4xx > 10% |
| **High Request Rate** | `rate(nginx_http_requests_total[1m]) > 10000` | `> 10k/min` por 5m | Request rate muy alto |
| **Rate Limit Hits High** | `rate(nginx_rate_limit_exceeded_total[5m]) > 100` | `> 100/min` por 5m | Rate limit excedido |
| **SSL Cert Expiring Soon** | `ssl_cert_expiry_timestamp - time() < 86400 * 14` | `< 14 días` | Certificado expira en 14 días |

### Medias (P2)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **Upstream Failures** | `rate(nginx_upstream_failures_total[5m]) > 10` | `> 10/min` por 15m | Fallos upstream |
| **High Latency P99** | `histogram_quantile(0.99, rate(nginx_http_request_duration_seconds_bucket[5m])) > 3` | `> 3s` por 15m | Latencia P99 > 3s |
| **Upstream Unhealthy** | `nginx_upstream_health_check_status{status="unhealthy"} == 1` | `== 1` por 5m | Upstream unhealthy |
| **Worker Connections High** | `nginx_connections_active / nginx_connections_max > 0.8` | `> 80%` por 15m | Conexiones worker altas |

---

## 5. Microsoft Entra ID Alerts

### Críticas (P0)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **Entra ID Unreachable** | `probe_failed_total{job="entra-id"} > 0` | `> 0` por 1m | Entra ID inalcanzable |
| **Auth Failure Spike** | `rate(auth_attempts_total{result="failure"}[5m]) > 0.5` | `> 50%` por 5m | Spike fallos auth > 50% |
| **Token Validation Failures** | `rate(auth_token_validation_failures_total[5m]) > 10` | `> 10/min` por 5m | Fallos validación token |
| **JWKS Endpoint Unreachable** | `probe_failed_total{job="entra-id-jwks"} > 0` | `> 0` por 1m | JWKS endpoint down |

### Altas (P1)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **Auth Failure Rate High** | `rate(auth_attempts_total{result="failure"}[5m]) / rate(auth_attempts_total[5m]) > 0.1` | `> 10%` por 5m | Tasa fallos auth > 10% |
| **MFA Challenge Failures** | `rate(auth_mfa_challenges_failed_total[5m]) > 5` | `> 5/min` por 5m | Fallos MFA > 5/min |
| **Token Validation Failures** | `rate(auth_token_validation_failures_total[5m]) > 5` | `> 5/min` por 5m | Fallos validación token > 5/min |
| **JWKS Cache Miss Rate High** | `rate(jwks_cache_misses_total[5m]) / rate(jwks_cache_requests_total[5m]) > 0.1` | `> 10%` por 10m | Cache miss JWKS > 10% |
| **Entra ID Latency High** | `histogram_quantile(0.95, rate(entra_id_request_duration_seconds_bucket[5m])) > 2` | `> 2s` por 5m | Latencia Entra ID > 2s |

### Medias (P2)

| Alerta | Expresión PromQL | Condición | Descripción |
|--------|------------------|-----------|-------------|
| **Auth Failure Rate Elevated** | `rate(auth_attempts_total{result="failure"}[5m]) / rate(auth_attempts_total[5m]) > 0.05` | `> 5%` por 10m | Tasa fallos auth > 5% |
| **Token Refresh Failures** | `rate(auth_token_refresh_failures_total[5m]) > 1` | `> 1/min` por 15m | Fallos refresh token |
| **Conditional Access Failures** | `rate(auth_ca_failures_total[5m]) > 2` | `> 2/min` por 15m | Fallos Conditional Access |
| **Sign-in Risk Events** | `rate(identity_protection_risk_events_total[5m]) > 5` | `> 5/min` por 15m | Eventos riesgo Identity Protection |
| **Entra ID Latency Elevated** | `histogram_quantile(0.95, rate(entra_id_request_duration_seconds_bucket[5m])) > 1` | `> 1s` por 15m | Latencia Entra ID > 1s |

---

## Resumen de Expresiones PromQL Clave

### Plantillas Reutilizables

```promql
# Latencia P95 genérica
histogram_quantile(0.95, rate(<metric_bucket>[5m]))

# Tasa de error
rate(<metric>_total{status=~"5.."}[5m]) / rate(<metric>_total[5m])

# Tasa de uso (%)
<metric>_active / <metric>_max

# Rate of change
rate(<metric>_total[5m])

# Percentage
(<numerator> / <denominator>) * 100
```

### Labels Estándar Recomendados

| Label | Descripción |
|-------|-------------|
| `job` | Nombre del job (api, postgres, nginx, etc.) |
| `instance` | Instancia específica |
| `environment` | dev/qa/prod |
| `team` | Equipo responsable |
| `severity` | critical/high/medium/low |
| `runbook` | URL al runbook |

---

## Runbooks Requeridos (Referencia)

| Alerta | Runbook |
|---------|---------|
| API Down | `runbooks/api-down.md` |
| PostgreSQL Down | `runbooks/postgres-down.md` |
| High Latency | `runbooks/high-latency.md` |
| Deadlocks | `runbooks/deadlocks.md` |
| Container OOM | `runbooks/container-oom.md` |
| Nginx 5xx | `runbooks/nginx-5xx.md` |
| Entra ID Down | `runbooks/entra-id-down.md` |
| SSL Cert Expiry | `runbooks/ssl-cert-expiry.md` |

---

*Documento versión 1.0 | Alertas operacionales para Workplace Booking Platform*