# Health Endpoint Specification

## Purpose

Define the liveness probe endpoint at `/health` for LifeTrack. This endpoint is anonymous-accessible, returns a minimal JSON payload, performs no database queries, and is suitable for Azure App Service health probes.

## Requirements

### Requirement: Health Endpoint Returns 200 OK

The system SHALL expose a GET endpoint at `/health` that returns HTTP 200 OK with a JSON body `{"status":"ok"}` regardless of database state.

The endpoint MUST be registered in `Program.cs` using `MapGet("/health", ...)`. The endpoint MUST be accessible without authentication (anonymous liveness probe).

#### Scenario: Health Endpoint Returns OK When Application Is Running

- GIVEN the LifeTrack application is running
- WHEN a GET request is made to `/health`
- THEN the response is HTTP 200 OK with `Content-Type: application/json` and body `{"status":"ok"}`

#### Scenario: Health Endpoint Accessible Without Authentication

- GIVEN no user is authenticated
- WHEN an unauthenticated GET request is made to `/health`
- THEN the response is HTTP 200 OK with `{"status":"ok"}` and no redirect to login occurs

### Requirement: Health Endpoint Does Not Check Database Connectivity

The `/health` endpoint MUST NOT execute any database queries, EF Core operations, or SQL connectivity checks.

The handler for `/health` MUST return a hardcoded `{"status":"ok"}` response without accessing `DbContext`, repositories, or any I/O-bound service.

#### Scenario: Health Endpoint Returns OK Even When Database Is Unavailable

- GIVEN the LifeTrack application is running but the SQL Server database is unreachable
- WHEN a GET request is made to `/health`
- THEN the response is HTTP 200 OK with `{"status":"ok"}` — the database unavailability does not affect the liveness response

### Requirement: Health Endpoint Has No Side Effects

The `/health` endpoint MUST NOT modify any application state, write logs (beyond minimum infrastructure), or perform any mutating operations.

Calling `/health` repeatedly MUST produce the same result with no cumulative side effects on the application.

#### Scenario: Repeated Health Checks Have No Side Effects

- GIVEN the LifeTrack application is running
- WHEN 100 consecutive GET requests are made to `/health`
- THEN each response is HTTP 200 OK with `{"status":"ok"}`
- AND no user sessions, database records, or application state are modified by the health checks

### Requirement: Health Endpoint Is Excluded from Global Authorization

The `/health` endpoint MUST be excluded from the fallback authorization policy that requires authentication for all other controller routes.

The endpoint registration in `Program.cs` MUST be placed before the `UseAuthorization` middleware, or the endpoint MUST be explicitly marked with `[AllowAnonymous]`.

#### Scenario: Health Endpoint Bypasses Authorization Middleware

- GIVEN the global fallback authorization policy requiring authentication is configured
- WHEN a GET request to `/health` is processed
- THEN the request is handled before the authorization middleware runs, or the route is explicitly excluded from authorization
- AND the response is returned without a 401 or redirect

### Requirement: Health Endpoint Response Content-Type Is JSON

The `/health` endpoint SHALL set the `Content-Type` response header to `application/json`.

#### Scenario: Health Endpoint Sets Correct Content-Type

- GIVEN the LifeTrack application is running
- WHEN a GET request is made to `/health`
- THEN the response includes header `Content-Type: application/json`
