# ADR-0003: EF Core con Azure SQL (oferta gratuita)

- **Estado:** Aceptada
- **Fecha:** 2026-10-02

## Contexto
Hace falta una base relacional, a coste cero y con herramientas conocidas.

## Decisión
**EF Core** con proveedor SQL Server y **Azure SQL Database free offer** (serverless con autopausa). En local, SQL Server en Docker. Reintentos ante errores transitorios (`EnableRetryOnFailure`). Migraciones aplicadas desde el pipeline, no al arrancar en producción.

## Consecuencias
- (+) Relacional, transacciones, integridad referencial y borrado en cascada para el derecho al olvido.
- (−) Primera consulta lenta tras la pausa; límites mensuales de cómputo. Se documentan y se aceptan.
- (−) La oferta puede no estar disponible en ciertos tipos de suscripción.

## Alternativas descartadas
- Cosmos DB free tier: modelo documental poco adecuado para agregaciones financieras.
- SQLite en Azure App Service: persistencia frágil en el plan gratuito.
