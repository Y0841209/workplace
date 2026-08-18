# Workplace Booking Platform - Monitoring Metrics Strategy

## 1. API Metrics

### HTTP Request Metrics

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `http_requests_total` | Counter | `method`, `path`, `status_code`, `user_role` | Total requests HTTP | - |
| `http_request_duration_seconds` | Histogram | `method`, `path`, `status_code` | Latencia end-to-end | p95 < 300ms |
| `http_request_duration_seconds_bucket` | Histogram | `method`, `path`, `le` | Buckets latencia | - |
| `http_request_size_bytes` | Histogram | `method`, `path` | Tamaño request body | - |
| `http_response_size_bytes` | Histogram | `method`, `path` | Tamaño response body | - |
| `http_requests_in_flight` | Gauge | `method`, `path` | Requests concurrentes | < 100 |

### Autenticación y Autorización

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `auth_attempts_total` | Counter | `provider`, `result` (success/failure), `error_type` | Intentos login Entra ID | Failure < 1% |
| `auth_token_refresh_total` | Counter | `result` (success/failure) | Refresh tokens | Failure < 0.1% |
| `active_sessions` | Gauge | - | Sesiones activas concurrentes | - |
| `authorization_checks_total` | Counter | `policy`, `result` (allow/deny) | Evaluaciones policies | - |
| `rate_limit_hits_total` | Counter | `zone` (api/auth/qr), `result` (allowed/denied) | Rate limit hits | Denied < 1% |

### Recursos y Disponibilidad

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `resource_availability` | Gauge | `resource_id`, `resource_type`, `floor` | Disponibilidad real-time | - |
| `resources_total` | Gauge | `resource_type`, `active`, `reservable` | Inventario por tipo | - |
| `resource_capacity_utilization` | Gauge | `resource_id`, `resource_type` | % ocupación actual | < 85% |

### Health Checks

| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `health_check_status` | Gauge | `check` (live/ready/db/entra-id/disk), `status` (0/1) | Estado health checks |
| `health_check_duration_seconds` | Histogram | `check` | Duración health checks |

---

## 2. PostgreSQL Metrics

### Conexiones

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `pg_connections_active` | Gauge | `pool` | Conexiones activas | < 80% max_connections |
| `pg_connections_idle` | Gauge | `pool` | Conexiones idle | - |
| `pg_connections_waiting` | Gauge | `pool` | Conexiones en espera | 0 |
| `pg_connections_total` | Gauge | `pool` | Total conexiones | - |

### Performance Queries

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `pg_query_duration_seconds` | Histogram | `query_type` (select/insert/update/delete), `table`, `schema` | Latencia queries | p95 < 50ms |
| `pg_query_duration_seconds_bucket` | Histogram | `query_type`, `table`, `le` | Buckets latencia | - |
| `pg_slow_queries_total` | Counter | `table`, `threshold_ms` | Queries > threshold | 0 |
| `pg_rows_fetched_total` | Counter | `table` | Filas leídas | - |
| `pg_rows_returned_total` | Counter | `table` | Filas retornadas | - |

### Transacciones y Conflictos

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `pg_transactions_active` | Gauge | - | Transacciones activas | - |
| `pg_transactions_committed_total` | Counter | - | Commits | - |
| `pg_transactions_rolled_back_total` | Counter | - | Rollbacks | < 1% |
| `pg_deadlocks_total` | Counter | `table` | Deadlocks detectados | 0 |
| `pg_locks_waiting` | Gauge | `mode`, `table` | Locks en espera | 0 |
| `pg_blocking_pids` | Gauge | - | Procesos bloqueados | 0 |

### Exclusion Constraints (Business Critical)

| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `exclusion_conflicts_total` | Counter | `constraint_name` (resource/user), `table` | Conflictos exclusion constraints |
| `reservation_conflicts_total` | Counter | `resource_type`, `conflict_type` (resource/user) | Conflictos reservas |

### Índices y Tablas

| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `pg_index_scans_total` | Counter | `table`, `index` | Index scans |
| `pg_seq_scans_total` | Counter | `table` | Sequential scans |
| `pg_index_usage_ratio` | Gauge | `table`, `index` | % index vs seq scan |
| `pg_table_size_bytes` | Gauge | `table`, `schema` | Tamaño tabla |
| `pg_index_size_bytes` | Gauge | `index`, `table` | Tamaño índice |
| `pg_bloat_ratio` | Gauge | `table`, `index` | Bloat ratio |

### Vacuum y Maintenance

| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `pg_autovacuum_runs_total` | Counter | `table` | Autovacuum runs |
| `pg_vacuum_runs_total` | Counter | `table` | Manual vacuum runs |
| `pg_analyze_runs_total` | Counter | `table` | Analyze runs |
| `pg_dead_tuples` | Gauge | `table` | Dead tuples |
| `pg_live_tuples` | Gauge | `table` | Live tuples |

### WAL y Replication

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `pg_wal_lsn_diff` | Gauge | `slot_name` | Replication lag (bytes) | < 100MB |
| `pg_wal_lag_seconds` | Gauge | `slot_name` | Replication lag (segundos) | < 10s |
| `pg_replication_slots_active` | Gauge | - | Slots activos | - |
| `pg_wal_write_bytes_total` | Counter | - | Bytes escritos WAL | - |
| `pg_checkpoint_duration_seconds` | Histogram | - | Duración checkpoint | < 30s |
| `pg_checkpoints_timed_total` | Counter | - | Checkpoints timed | - |
| `pg_checkpoints_req_total` | Counter | - | Checkpoints requested | - |

---

## 3. Docker Metrics

### Contenedores

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `container_cpu_usage_seconds_total` | Counter | `container`, `pod`, `namespace` | CPU segundos totales | - |
| `container_cpu_usage_seconds_per_second` | Gauge | `container`, `pod` | CPU usage rate | < 80% |
| `container_memory_usage_bytes` | Gauge | `container`, `pod` | Memoria usada | < 80% limit |
| `container_memory_limit_bytes` | Gauge | `container`, `pod` | Límite memoria | - |
| `container_memory_usage_percent` | Gauge | `container`, `pod` | % memoria usada | < 80% |
| `container_network_receive_bytes_total` | Counter | `container`, `interface` | RX bytes | - |
| `container_network_transmit_bytes_total` | Counter | `container`, `interface` | TX bytes | - |
| `container_restarts_total` | Counter | `container`, `reason` | Reinicios | 0 |
| `container_status` | Gauge | `container`, `phase` (running/waiting/terminated) | Estado | running |

### Kubernetes (si aplica)

| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `kube_pod_status_phase` | Gauge | `pod`, `namespace`, `phase` | Fase pod |
| `kube_pod_container_status_restarts_total` | Counter | `container`, `pod`, `namespace` | Reinicios |
| `kube_pod_container_resource_limits` | Gauge | `container`, `resource` (cpu/memory) | Límites |
| `kube_pod_container_resource_requests` | Gauge | `container`, `resource` | Requests |
| `kube_node_status_condition` | Gauge | `node`, `condition`, `status` | Condiciones nodo |

---

## 4. Performance Metrics

### Frontend (React + Web Vitals)

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `web_vitals_fcp` | Histogram | `page`, `device` | First Contentful Paint | p75 < 1.5s |
| `web_vitals_lcp` | Histogram | `page`, `device` | Largest Contentful Paint | p75 < 2.5s |
| `web_vitals_fid` | Histogram | `page`, `device` | First Input Delay | p75 < 100ms |
| `web_vitals_cls` | Histogram | `page`, `device` | Cumulative Layout Shift | p75 < 0.1 |
| `web_vitals_ttfb` | Histogram | `page` | Time to First Byte | p75 < 600ms |
| `web_vitals_tti` | Histogram | `page` | Time to Interactive | p75 < 3.5s |

| Métrica | Tipo | Labels | Descripción |
|---------|------|--------|-------------|
| `page_load_duration_seconds` | Histogram | `route`, `device` | Tiempo carga completa |
| `api_call_duration_seconds` | Histogram | `endpoint`, `method` | Latencia llamadas API |
| `api_call_errors_total` | Counter | `endpoint`, `method`, `error_type` | Errores API |
| `bundle_size_bytes` | Gauge | `chunk`, `type` (js/css) | Tamaño bundles |
| `cache_hit_ratio` | Gauge | `cache_type` (browser/sw) | Cache hit ratio |

### Application Performance (Backend)

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `dotnet_gc_collections_total` | Counter | `gen` (0/1/2) | GC collections | - |
| `dotnet_gc_heap_size_bytes` | Gauge | - | Heap size | < 500MB |
| `dotnet_threadpool_threads` | Gauge | `type` (worker/completion) | Threads pool | - |
| `dotnet_threadpool_queue_length` | Gauge | - | Queue length | < 100 |
| `dotnet_exception_total` | Counter | `exception_type`, `method` | Excepciones | - |

### Business Performance

| Métrica | Tipo | Labels | Descripción | Objetivo |
|---------|------|--------|-------------|----------|
| `reservation_created_total` | Counter | `resource_type`, `user_profile`, `status` | Reservas creadas | - |
| `reservation_duration_seconds` | Histogram | `resource_type` | Duración media reserva | - |
| `reservation_lead_time_hours` | Histogram | `resource_type` | Antelación reserva | - |
| `checkin_latency_seconds` | Histogram | `resource_type` | Tiempo check-in | < 2s |
| `availability_search_duration_seconds` | Histogram | `resource_type`, `floor` | Búsqueda disponibilidad | p95 < 300ms |
| `qr_validation_duration_seconds` | Histogram | `resource_type` | Validación QR | < 500ms |

---

## Resumen KPIs Críticos (Resumen Ejecutivo)

| Categoría | KPI | Objetivo | Criticidad |
|-----------|-----|----------|------------|
| **API Latency p95** | `http_request_duration_seconds` | < 300ms | P0 |
| **API Error Rate** | `rate(http_requests_total{status=~"5.."}[5m])` | < 0.1% | P0 |
| **API Availability** | `up{job="api"}` | 99.9% | P0 |
| **DB Query p95** | `pg_query_duration_seconds` | < 50ms | P0 |
| **DB Connections** | `pg_connections_active / pg_settings.max_connections` | < 80% | P0 |
| **DB Deadlocks** | `pg_deadlocks_total` | 0 | P0 |
| **Reservation Conflicts** | `exclusion_conflicts_total` | 0 (handled by DB) | P0 |
| **Frontend FCP** | `web_vitals_fcp` | p75 < 1.5s | P1 |
| **Frontend LCP** | `web_vitals_lcp` | p75 < 2.5s | P1 |
| **Container CPU** | `container_cpu_usage_percent` | < 80% | P1 |
| **Container Memory** | `container_memory_usage_percent` | < 80% | P1 |
| **Container Restarts** | `container_restarts_total` | 0 | P1 |
| **Auth Failure Rate** | `auth_attempts_total{result="failure"}` | < 0.5% | P1 |
| **Rate Limit Denied** | `rate_limit_hits_total{result="denied"}` | < 1% | P1 |

---

*Documento versión 1.0 | Stack: React 18, .NET 8, PostgreSQL 16, Docker, Microsoft Entra ID*