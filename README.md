# Workplace Booking Platform

Sistema corporativo para la administración y reserva de espacios físicos de trabajo.

---

## Descripción

Workplace Booking Platform es una aplicación web corporativa diseñada para administrar la ocupación de espacios de trabajo dentro de la organización.

La solución permite reservar:

- Oficinas Abiertas (OA)
- Oficinas Cerradas (OC)
- Salas de Juntas (SJ)

La plataforma utiliza Microsoft Entra ID para autenticación corporativa y está diseñada bajo principios de seguridad, trazabilidad, auditoría y experiencia responsive.

---

## Objetivos

- Centralizar la gestión de espacios corporativos.
- Optimizar la ocupación física de las oficinas.
- Reducir conflictos de reserva.
- Facilitar el check-in mediante QR.
- Generar trazabilidad completa de las reservas.
- Integrarse con el ecosistema Microsoft 365.

---

## Inventario Inicial

### Piso 3

| Recurso | Cantidad |
|----------|----------:|
| Oficinas Abiertas | 30 |
| Oficinas Cerradas | 9 |
| Salas de Juntas | 2 |

Salas:

- SJ-06 (8 personas)
- SJ-07 (8 personas)

---

### Piso 6

| Recurso | Cantidad |
|----------|----------:|
| Oficinas Abiertas | 18 |
| Oficinas Cerradas | 10 |
| Salas de Juntas | 5 |

Salas:

- SJ-01 (12 personas)
- SJ-02 (12 personas)
- SJ-03 (6 personas)
- SJ-04 (5 personas)
- SJ-05 (24 personas)

---

### Piso 10

| Recurso | Cantidad |
|----------|----------:|
| Oficinas Abiertas | 12 |
| Oficinas Cerradas | 5 |

---

### Totales

| Tipo | Cantidad |
|--------|--------:|
| Oficinas Abiertas | 60 |
| Oficinas Cerradas | 24 |
| Salas de Juntas | 7 |
| Recursos Reservables | 91 |

---

## Reglas Funcionales Principales

### Reservas

- Reserva por horas.
- Duración mínima: 1 hora.
- Duración máxima: mismo día hasta las 23:59.
- No se permiten reservas que crucen al día siguiente.
- Máximo 5 reservas futuras activas por usuario.

### Administrador de Salas

Los usuarios con rol:

```text
ROOM_ADMIN
```

podrán superar el límite de 5 reservas futuras únicamente para:

```text
MEETING_ROOM
```

---

### Oficinas Abiertas

Pueden reservar:

- Colaboradores
- Asociados
- Líderes
- Directores
- Socios

---

### Oficinas Cerradas

Pueden reservar:

- Líderes
- Directores
- Socios

---

### Salas de Juntas

Pueden reservar:

- Todos los usuarios autenticados

---

## Check-In QR

Aplica únicamente a:

- OPEN_WORKSPACE
- CLOSED_OFFICE

No aplica a:

- MEETING_ROOM

Flujo:

```text
Reserva
    ↓
Escanear QR
    ↓
Microsoft Entra ID
    ↓
Validar Reserva
    ↓
Confirmar Check-In
```

---

## Notificaciones

El sistema enviará correos corporativos para:

- Reserva creada
- Reserva modificada
- Reserva cancelada
- Recordatorio 15 minutos antes de iniciar la reserva

---

## Identidad Visual

### Colores Corporativos

```text
Amarillo Corporativo: #FFD800
Negro Corporativo:    #0E0E0E
Negro:                #000000
Blanco:               #FFFFFF
Blanco Cálido:        #F6F0CB
Gris Oscuro:          #2A2A2A
Gris Claro:           #F5F5F5
```

### Principios de Diseño

- Responsive First
- Mobile First
- Accesibilidad WCAG AA
- Diseño sobrio y corporativo
- Compatible con dispositivos móviles y escritorio

---

## Arquitectura Tecnológica

### Frontend

```text
React
TypeScript
Material UI
```

### Backend

```text
.NET 8
ASP.NET Core Web API
Entity Framework Core
FluentValidation
Serilog
```

### Base de Datos

```text
PostgreSQL 16
```

### Identidad

```text
Microsoft Entra ID
```

### Infraestructura

```text
Ubuntu Server 24.04
Docker
Docker Compose
Nginx
```

### Reportería

```text
Power BI
```

---

## Estructura Esperada del Proyecto

```text
workplace-booking-platform

├── docs
├── backend
│   ├── Domain
│   ├── Application
│   ├── Infrastructure
│   └── API
│
├── frontend
│   └── src
│
├── database
│   ├── migrations
│   └── seed
│
├── infrastructure
│
├── tests
│
└── .github
```

---

## Seguridad

Implementar:

- OWASP Top 10
- RBAC
- JWT Validation
- Auditoría
- HTTPS Only
- Rate Limiting
- Secret Scanning
- Dependency Scanning
- CodeQL
- DevSecOps Pipeline

---

## Documentación Funcional

La fuente oficial de requerimientos es:

```text
FRD_Modelo_Datos_Workplace_Booking_OpenCode.docx
```

Todo el desarrollo deberá alinearse con dicho documento.

---

## Objetivo para OpenCode

Utilizar este repositorio como base para generar:

- Arquitectura Técnica
- Modelo PostgreSQL
- Backend .NET 8
- Frontend React
- Docker Compose
- CI/CD GitHub Actions
- Seguridad
- Pruebas
- Documentación Técnica

---

## Estado del Proyecto

```text
Fase Actual:
Levantamiento de Requerimientos y Arquitectura

Estado:
En Diseño

Tecnología Objetivo:
React + .NET 8 + PostgreSQL

Infraestructura:
Ubuntu 24.04 + Docker Compose
```
