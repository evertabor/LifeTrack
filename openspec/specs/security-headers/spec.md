# Security Headers Specification

## Purpose

Define the HTTP security header middleware that LifeTrack applies to every HTTP response. The middleware sets Content-Security-Policy (with `'unsafe-inline'` for Identity UI compatibility), X-Content-Type-Options, X-Frame-Options, Referrer-Policy, and Permissions-Policy headers to harden the application against OWASP A01/A02/A05 attacks.

## Requirements

### Requirement: Content-Security-Policy Header Is Set on All Responses

The system SHALL set the `Content-Security-Policy` header on every HTTP response.

The CSP MUST include `default-src 'self'`. The CSP MUST allow `unsafe-inline` for style elements (`'unsafe-inline'` in `style` directive) to support Identity UI's inline styles. The CSP SHOULD restrict `script-src` to `'self'`. The CSP MUST NOT allow `unsafe-eval`. The CSP MUST be configurable via `IConfiguration` so that values can be changed without recompilation.

#### Scenario: CSP Header Present on Normal Page Response

- GIVEN the application processes an HTTP GET request for a Razor view
- WHEN the response is generated
- THEN the response includes a `Content-Security-Policy` header with at least `default-src 'self'`

#### Scenario: CSP Header Allows Unsafe-Inline for Styles

- GIVEN the application serves a page that includes Identity UI or any inline `<style>` element
- WHEN the response is generated
- THEN the CSP header includes `'unsafe-inline'` in the `style` directive

#### Scenario: CSP Header Does Not Allow Unsafe-Eval

- GIVEN the application processes an HTTP request
- WHEN the response is generated
- THEN the `Content-Security-Policy` header does not include `unsafe-eval` in any directive

### Requirement: X-Content-Type-Options Header Is Set to Nosniff

The system SHALL set the `X-Content-Type-Options` header to `nosniff` on every HTTP response.

This header prevents browsers from MIME-sniffing a response away from the declared content-type, mitigating type-confusion attacks.

#### Scenario: X-Content-Type-Options Header Set to Nosniff

- GIVEN the application processes any HTTP request
- WHEN the response is generated
- THEN the response includes header `X-Content-Type-Options: nosniff`

### Requirement: X-Frame-Options Header Is Set to Deny

The system SHALL set the `X-Frame-Options` header to `DENY` on every HTTP response.

This header prevents the application from being embedded in an iframe, mitigating clickjacking attacks.

#### Scenario: X-Frame-Options Header Set to Deny

- GIVEN the application processes any HTTP request
- WHEN the response is generated
- THEN the response includes header `X-Frame-Options: DENY`

### Requirement: Referrer-Policy Header Is Set

The system SHALL set the `Referrer-Policy` header to `strict-origin-when-cross-origin` on every HTTP response.

This header controls information leakage in the Referer header for cross-origin requests.

#### Scenario: Referrer-Policy Header Set Correctly

- GIVEN the application processes any HTTP request
- WHEN the response is generated
- THEN the response includes header `Referrer-Policy: strict-origin-when-cross-origin`

### Requirement: Permissions-Policy Header Restricts Sensitive Features

The system SHALL set the `Permissions-Policy` header to restrict access to sensitive browser features.

The policy MUST disable `geolocation`, `microphone`, `camera`, and `payment` unless explicitly required by a future feature. The default policy MUST be as restrictive as possible.

#### Scenario: Permissions-Policy Header Restricts Geolocation and Microphone

- GIVEN the application processes any HTTP request
- WHEN the response is generated
- THEN the response includes a `Permissions-Policy` header that includes `geolocation=()`, `microphone=()`, and `camera=()`

### Requirement: Security Headers Middleware Executes Before Response Body Is Written

The security headers middleware MUST add its headers to every response before the response body is committed, ensuring headers are present on all responses including error pages.

The middleware MUST be registered in `Program.cs` before `UseRouting` and before `UseResponseCompression` (if any) so that headers are applied to compressed responses as well.

#### Scenario: Security Headers Present on Error Page Response

- GIVEN an unhandled exception occurs during request processing
- AND the error page is rendered
- WHEN the error page response is generated
- THEN the response includes all required security headers (CSP, X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy)

### Requirement: Health Endpoint Excluded from CSP Reporting or Relaxed CSP

The `/health` endpoint responses SHOULD either exclude CSP headers or include a relaxed CSP configuration that does not interfere with monitoring tools.

If the CSP middleware applies to all responses, the `/health` endpoint MUST still include the `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and `Permissions-Policy` headers but MAY omit or relax the CSP.

#### Scenario: Health Endpoint Response Includes Non-CSP Security Headers

- GIVEN a GET request to `/health` is processed
- WHEN the response is generated
- THEN the response includes `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, and `Permissions-Policy` headers

### Requirement: Security Headers Are Configurable

All security header values MUST be configurable via `appsettings.json` or environment variables so that operators can adjust them without code changes.

The middleware MUST read header values from `IConfiguration` sections such as `SecurityHeaders:Csp`, `SecurityHeaders:ReferrerPolicy`, etc.

#### Scenario: CSP Value Read from Configuration

- GIVEN `appsettings.json` contains `{"SecurityHeaders": {"Csp": "default-src 'self'; style-src 'self' 'unsafe-inline'"}}`
- WHEN the middleware processes a request
- THEN the `Content-Security-Policy` header value matches the configured value
