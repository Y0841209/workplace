# Tabla de Componentes - Workplace Booking Platform

| Componente | Responsabilidad | Tecnología |
|------------|-----------------|------------|
| Frontend | Interfaz responsive, reserva, administración, check-in QR | React + TypeScript + Material UI |
| Backend | Reglas de negocio, APIs, autorización y auditoría | .NET 8 Web API |
| Base de datos | Persistencia transaccional, constraints e inventario | PostgreSQL 16 |
| Identidad | Autenticación OIDC y obtención de correo corporativo | Microsoft Entra ID |
| Infraestructura | Despliegue inicial en VM | Ubuntu 24.04 + Docker Compose + Nginx |
| Reportes | Visualización de ocupación y métricas | Power BI |