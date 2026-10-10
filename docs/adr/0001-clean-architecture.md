# ADR-0001: Clean Architecture con cuatro proyectos

- **Estado:** Aceptada
- **Fecha:** 2026-10-02

## Contexto
El proyecto se construye con asistencia de IA. Sin límites claros, los agentes tienden a mezclar acceso a datos, lógica y HTTP. Además, el TFM debe demostrar una arquitectura defendible.

## Decisión
Cuatro proyectos: `Domain`, `Application`, `Infrastructure`, `Web`, con dependencias solo hacia dentro. `Domain` no usa paquetes NuGet; `Application` solo referencia `Domain` y el paquete `MediatR` (ADR-0004), sin frameworks de infraestructura ni de web. La regla se verifica con `LifeTrack.Architecture.Tests`.

## Consecuencias
- (+) Núcleo testeable sin base de datos ni web. Cambios de framework aislados.
- (+) La IA tiene límites verificables automáticamente.
- (−) Más archivos y algo de ceremonia para un proyecto pequeño. Se acepta por el valor didáctico y de calidad.
- (−) `Application` requiere `dotnet restore`: con `MediatR` como único paquete, el núcleo ya no compila sin acceso a internet.

## Alternativas descartadas
- Un único proyecto con carpetas: más simple, pero los límites no se pueden hacer cumplir al compilar.
- Vertical Slice puro: válido, pero menos alineado con lo estudiado en el máster.
