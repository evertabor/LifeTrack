# ADR-0005: "Hoy" según una zona horaria configurable

- **Estado:** Aceptada
- **Fecha:** 2026-10-02

## Contexto
Azure ejecuta la aplicación en UTC. Rachas de hábitos y la regla "sin fechas futuras" dependen del día local del usuario; con UTC, por la noche se rechazarían check-ins legítimos o se contarían en el día equivocado.

## Decisión
Se usa `TimeProvider` (BCL). En producción se registra un `ZonedTimeProvider` que sobrescribe `LocalTimeZone` con la zona de `App:TimeZoneId`. Los casos de uso obtienen la fecha con `timeProvider.Today()`. El dominio recibe `today` como parámetro, nunca consulta el reloj.

## Consecuencias
- (+) Pruebas deterministas con un reloj fijo; comportamiento correcto sin importar la zona del servidor.
- (−) `App__TimeZoneId` pasa a ser configuración obligatoria del despliegue (está en el checklist).
- (−) Modelo de **una zona por despliegue**: si hubiera usuarios en varias zonas, haría falta guardar la zona por usuario.
