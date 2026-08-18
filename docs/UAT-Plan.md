# Workplace Booking Platform - UAT Plan

## 1. Objetivo

Validar que el Workplace Booking Platform cumple con los requerimientos funcionales, de seguridad y usabilidad definidos en el FRD antes del despliegue a producción.

## 2. Alcance

### En Scope
- Reserva de Oficinas Abiertas (OA), Oficinas Cerradas (OC), Salas de Juntas
- Check-in/Check-out via QR
- Cancelación y modificación de reservas
- Roles: USER, ROOM_ADMIN, SUPPORT, GLOBAL_ADMIN
- Auditoría y notificaciones

### Fuera de Scope
- Integración con sistemas externos (RR.HH., Active Directory sync)
- App móvil nativa
- Reportes avanzados / Power BI

## 3. Criterios de Entrada

| Criterio | Estado |
|----------|--------|
| Código deployado en ambiente UAT | ☐ |
| Base de datos con datos semilla (91 recursos, 3 pisos, 7 salas) | ☐ |
| Usuarios de prueba creados en Entra ID / BD | ☐ |
| Configuración Entra ID (App Registration, redirect URIs) | ☐ |
| SMTP configurado (smtp4dev para UAT) | ☐ |
| Nginx + SSL configurado | ☐ |
| Datos semilla cargados (91 recursos, 3 pisos, 7 salas) | ☐ |

## 4. Roles de Prueba

| Usuario | Entra ID | Perfil | Rol Admin | Propósito |
|---------|----------|--------|-----------|-----------|
| `uat.colaborador@company.com` | Sí | COLLABORATOR | USER | Usuario estándar |
| `uat.lider@company.com` | Sí | LEADER | USER | Usuario con acceso OC |
| `uat.roomadmin@company.com` | Sí | COLLABORATOR | ROOM_ADMIN | Admin salas |
| `uat.globaladmin@company.com` | Sí | PARTNER | GLOBAL_ADMIN | Admin global |
| `uat.support@company.com` | Sí | COLLABORATOR | SUPPORT | Soporte TI |

## 4. Calendario UAT

| Fase | Fechas | Responsable |
|------|--------|-------------|
| Preparación datos | Día 1 | QA Lead |
| Ejecución Casos Core | Días 2-4 | QA Team + Business Users |
| Ejecución Casos Admin | Día 5 | QA Team + Admin Users |
| Retesting / Bug Fixes | Días 6-7 | Dev Team + QA |
| Firma UAT | Día 8 | Product Owner + QA Lead |

## 5. Criterios de Salida

| Criterio | Criterio de Aceptación |
|----------|------------------------|
| Casos Pass | ≥ 95% casos Pass |
| Defectos Críticos | 0 abiertos |
| Defectos Altos | ≤ 2 abiertos (con workaround) |
| Cobertura Requisitos | 100% FRD cubierto |
| Performance | API p95 < 300ms, Check-in < 2s |
| Seguridad | 0 vulnerabilidades Críticas/High |

## 5. Matriz de Trazabilidad

| Requisito FRD | Casos UAT | Estado |
|---------------|-----------|--------|
| RF-001 Crear reserva | UAT-RSV-001 a 005 | ☐ |
| RF-002 Modificar reserva | UAT-RSV-006 a 008 | ☐ |
| RF-003 Cancelar reserva | UAT-RSV-009 a 011 | ☐ |
| RF-004 Disponibilidad | UAT-RSV-012 a 015 | ☐ |
| RF-005 Exclusividad | UAT-RSV-016 a 018 | ☐ |
| RF-006 Duración mínima | UAT-RSV-019 a 021 | ☐ |
| RF-007 Mismo día | UAT-RSV-022 a 024 | ☐ |
| RF-008 Hora máxima | UAT-RSV-025 a 027 | ☐ |
| RF-009 Límite 5 reservas | UAT-RSV-028 a 031 | ☐ |
| RF-010 Excepción ROOM_ADMIN | UAT-RSV-032 a 034 | ☐ |
| RF-011 GLOBAL_ADMIN | UAT-ADM-001 a 005 | ☐ |
| RF-012 Recordatorio | UAT-NOT-001 a 003 | ☐ |
| QR Check-in | UAT-CHK-001 a 008 | ☐ |
| QR OA/OC only | UAT-CHK-009 a 011 | ☐ |
| Salas sin check-in | UAT-CHK-012 a 014 | ☐ |
| Perfiles OA | UAT-RSV-001, 002 | ☐ |
| Perfiles OC | UAT-RSV-003, 004 | ☐ |
| Perfiles Salas | UAT-RSV-005 | ☐ |
| ROOM_ADMIN salas | UAT-ADM-006 a 008 | ☐ |
| Auditoría | UAT-AUD-001 a 005 | ☐ |

---

## 6. Defectos y Triage

| Severidad | Definición | SLA Resolución |
|-----------|------------|----------------|
| Crítica | Bloquea funcionalidad core, sin workaround | 4 horas |
| Alta | Funcionalidad importante afectada, workaround parcial | 1 día |
| Media | Funcionalidad menor, workaround completo | 3 días |
| Baja | Cosmético, mejora UX | Próximo sprint |

---

*Documento versión 1.0 | UAT Plan Workplace Booking Platform*