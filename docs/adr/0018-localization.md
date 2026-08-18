# ADR-0018: Spanish (es-CO) Primary Localization

## Status
Accepted

## Context
Localization requirements:
- Colombian law firm context
- Spanish primary language (es-CO locale)
- English for technical terms, fallback
- Date/time in Colombia timezone (America/Bogota)
- Currency: COP (if needed)
- Number formatting: Spanish conventions

## Decision
**Spanish (es-CO) as primary locale**, English (en-US) as fallback.

### Backend Localization

```csharp
// Program.cs
var supportedCultures = new[]
{
    new CultureInfo("es-CO"),
    new CultureInfo("en-US")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("es-CO");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});

// Timezone
builder.Services.AddSingleton(TimeProvider.System); // Or custom for testing

// In Use Cases - use TimeZoneInfo for Colombia
private static readonly TimeZoneInfo ColombiaTimeZone = 
    TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");

public DateTimeOffset GetColombiaNow() => TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ColombiaTimeZone);
```

### Resource Files

```resx
<!-- Resources/Strings.es-CO.resx -->
<data name="ReservationCreated" xml:space="preserve">
  <value>Reserva creada exitosamente</value>
</data>
<data name="ReservationConflict" xml:space="preserve">
  <value>El recurso no está disponible en el horario seleccionado</value>
</data>
<data name="MaxReservationsExceeded" xml:space="preserve">
  <value>Ha superado el límite de {0} reservas futuras activas</value>
</data>
<data name="CheckInSuccess" xml:space="preserve">
  <value>Check-in realizado correctamente</value>
</data>
<data name="ValidationMinDuration" xml:space="preserve">
  <value>La reserva debe durar al menos 1 hora</value>
</data>
```

```csharp
// Localization Service
public interface ILocalizer
{
    string GetString(string key, params object[] args);
    string GetString(string key, CultureInfo culture, params object[] args);
}

public class StringLocalizer : ILocalizer
{
    private readonly IStringLocalizer _localizer;

    public string GetString(string key, params object[] args)
        => _localizer[key, args].Value;

    public string GetString(string key, CultureInfo culture, params object[] args)
        => _localizer.WithCulture(culture)[key, args].Value;
}
```

### Frontend Localization (i18next)

```typescript
// src/i18n.ts
import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import esCO from './locales/es-CO.json';
import enUS from './locales/en-US.json';

i18n
  .use(initReactI18next)
  .init({
    resources: {
      'es-CO': { translation: esCO },
      'en-US': { translation: enUS },
    },
    lng: 'es-CO',
    fallbackLng: 'en-US',
    interpolation: { escapeValue: false },
    detection: {
      order: ['querystring', 'cookie', 'localStorage', 'navigator', 'htmlTag'],
      caches: ['cookie'],
    },
  });

export default i18n;
```

```json
// src/locales/es-CO.json
{
  "common": {
    "save": "Guardar",
    "cancel": "Cancelar",
    "delete": "Eliminar",
    "edit": "Editar",
    "confirm": "Confirmar",
    "loading": "Cargando...",
    "error": "Error",
    "success": "Éxito"
  },
  "reservation": {
    "create": "Crear reserva",
    "modify": "Modificar reserva",
    "cancel": "Cancelar reserva",
    "checkIn": "Registrar entrada",
    "checkOut": "Registrar salida",
    "title": "Título",
    "description": "Descripción",
    "date": "Fecha",
    "startTime": "Hora de inicio",
    "endTime": "Hora de fin",
    "resource": "Recurso",
    "type": "Tipo",
    "capacity": "Capacidad",
    "attendees": "Asistentes",
    "status": {
      "CONFIRMED": "Confirmada",
      "CHECKED_IN": "En uso",
      "CHECKED_OUT": "Finalizada",
      "CANCELLED": "Cancelada",
      "COMPLETED": "Completada",
      "NOT_CHECKED_IN": "No registró entrada"
    }
  },
  "resource": {
    "openWorkspace": "Oficina abierta",
    "closedOffice": "Oficina cerrada",
    "meetingRoom": "Sala de juntas",
    "available": "Disponible",
    "occupied": "Ocupado",
    "maintenance": "Mantenimiento"
  },
  "validation": {
    "required": "Este campo es obligatorio",
    "minDuration": "La reserva debe durar al menos 1 hora",
    "maxEndTime": "La hora máxima de finalización es 23:59",
    "sameDay": "La reserva debe iniciar y finalizar el mismo día",
    "futureLimit": "Máximo {{count}} reservas futuras activas",
    "conflict": "Ya tienes una reserva en ese horario"
  },
  "notifications": {
    "created": "Reserva creada exitosamente",
    "modified": "Reserva modificada",
    "cancelled": "Reserva cancelada",
    "reminder": "Tu reserva comienza en 15 minutos",
    "checkInSuccess": "Check-in registrado",
    "checkOutSuccess": "Check-out registrado"
  }
}
```

### Date/Time Formatting

```typescript
// src/utils/date.ts
import { formatInTimeZone, toZonedTime } from 'date-fns-tz';
import { esCO, enUS } from 'date-fns/locale';

const COLOMBIA_TZ = 'America/Bogota';

export function formatDateTime(date: Date, locale: 'es-CO' | 'en-US' = 'es-CO'): string {
  const zoned = toZonedTime(date, COLOMBIA_TZ);
  const localeObj = locale === 'es-CO' ? esCO : enUS;
  return formatInTimeZone(zoned, COLOMBIA_TZ, "PPP 'a las' p", { locale: localeObj });
}

export function formatDate(date: Date, locale: 'es-CO' | 'en-US' = 'es-CO'): string {
  const localeObj = locale === 'es-CO' ? esCO : enUS;
  return format(date, 'PPP', { locale: localeObj });
}

export function formatTime(date: Date): string {
  return formatInTimeZone(date, COLOMBIA_TZ, 'HH:mm');
}

// Usage in components
const reservationDate = new Date(reservation.reservationDate + 'T' + reservation.startTime);
<div>{formatDateTime(reservationDate)}</div>
// Output: "15 de enero de 2026 a las 09:00"
```

### Number/Currency Formatting

```typescript
// src/utils/number.ts
export function formatNumber(value: number, locale: 'es-CO' | 'en-US' = 'es-CO'): string {
  return new Intl.NumberFormat(locale).format(value);
}

export function formatCurrency(value: number, locale: 'es-CO' | 'en-US' = 'es-CO'): string {
  return new Intl.NumberFormat(locale, {
    style: 'currency',
    currency: locale === 'es-CO' ? 'COP' : 'USD',
    minimumFractionDigits: 0,
  }).format(value);
}
```

### Timezone Handling

```csharp
// Backend - All timestamps stored as UTC (timestamptz)
// Convert to Colombia for display
public class TimeZoneService
{
    private readonly TimeZoneInfo _colombia = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
    
    public DateTimeOffset ToColombiaTime(DateTimeOffset utc) 
        => TimeZoneInfo.ConvertTime(utc, _colombia);
    
    public DateOnly GetColombiaDate(DateTimeOffset utc) 
        => TimeZoneInfo.ConvertTime(utc, _colombia).DateOnly;
    
    public TimeOnly GetColombiaTime(DateTimeOffset utc) 
        => TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, _colombia).DateTime);
}
```

## Consequences

### Positive
- **User Experience**: Native Spanish Colombian formatting
- **Consistency**: Single source of truth for translations
- **Fallback**: English for untranslated keys
- **Timezone Correct**: All times in America/Bogota
- **Extensible**: Add more locales via resource files

### Negative
- **Maintenance**: Translation files must stay in sync
- **Testing**: Need to verify both locales
- **Dynamic Content**: User-generated content not translated

### Neutral
- Date-fns-tz handles timezone on frontend
- Backend stores UTC, converts on output
- ICU message format for complex plurals/genders if needed later

## Alternatives Considered

1. **English Only**
   - Rejected: Colombian users, legal requirement for Spanish

2. **Runtime Translation API (Google/Azure Translator)**
   - Rejected: Cost, latency, quality, offline unavailable

3. **Browser Built-in (Intl) Only**
   - Rejected: No message catalog, inconsistent across browsers

## References
- [i18next](https://www.i18next.com/)
- [date-fns-tz](https://github.com/marnusw/date-fns-tz)
- [ASP.NET Core Localization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization)
- [Unicode CLDR - Spanish Colombia](https://cldr.unicode.org/)