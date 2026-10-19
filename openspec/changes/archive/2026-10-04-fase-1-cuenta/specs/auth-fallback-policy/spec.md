# Authentication Fallback Policy Specification

## Purpose

Define the global fallback authorization policy that requires an authenticated user for all controller routes in LifeTrack, with explicit anonymous access granted only to designated endpoints. This establishes the deny-by-default access model required by OWASP A01.

## Requirements

### Requirement: Global Fallback Policy Requires Authentication

The system SHALL apply a fallback authorization policy requiring an authenticated user to all controller actions by default.

`Program.cs` MUST configure `AddAuthorization` with a default policy that calls `RequireAuthenticatedUser()`. This policy MUST be set as the fallback policy using `options.DefaultPolicy` or `options.FallbackPolicy`. All controller actions that do not have an explicit `[AllowAnonymous]` attribute MUST require authentication.

#### Scenario: Default Policy Enforced on Unauthenticated Request

- GIVEN no user is authenticated
- WHEN an unauthenticated request is made to any controller action without `[AllowAnonymous]`
- THEN the server returns HTTP 401 Unauthorized or redirects to the login page

#### Scenario: Authenticated Request Allowed

- GIVEN a user is authenticated with a valid session cookie
- WHEN an authenticated request is made to a protected controller action
- THEN the request is authorized and the action executes normally

### Requirement: Anonymous Access Exemption for Health Endpoint

The system SHALL allow unauthenticated access to the `/health` endpoint without any authorization check.

The `AccountController` or health endpoint definition MUST be annotated with `[AllowAnonymous]`. The `/health` endpoint MUST NOT perform any database queries, session checks, or authentication lookups.

#### Scenario: Health Endpoint Accessible Without Authentication

- GIVEN no user is authenticated
- WHEN an unauthenticated GET request is made to `/health`
- THEN the server returns HTTP 200 OK with `{"status":"ok"}` and does not redirect to login

#### Scenario: Health Endpoint Returns Correct Payload

- GIVEN any request (authenticated or not) to `/health`
- WHEN the server processes the request
- THEN the response body is exactly `{"status":"ok"}` with HTTP status code 200

### Requirement: Anonymous Access Exemption for Error Page

The system SHALL allow unauthenticated access to the error handling path without triggering an authorization redirect.

The `ErrorController` actions MUST be annotated with `[AllowAnonymous]`. An unauthenticated user MUST be able to view the error page without being redirected to login.

#### Scenario: Error Page Accessible Without Authentication

- GIVEN no user is authenticated
- AND an unhandled exception occurs during request processing
- WHEN the error page is rendered
- THEN the user sees the error page without being redirected to login

### Requirement: AllowAnonymous Attribute Overrides Default Policy

The system SHALL allow individual controller actions to opt out of the default authenticated-user policy by applying `[AllowAnonymous]`.

Any action annotated with `[AllowAnonymous]` MUST be accessible without authentication. The presence of `[AllowAnonymous]` on any action within a controller MUST NOT change the authorization requirement of other actions within the same controller that do not have the attribute.

#### Scenario: AllowAnonymous on Specific Action Only

- GIVEN a controller has two actions: `ActionA` (no attribute) and `ActionB` (with `[AllowAnonymous]`)
- WHEN an unauthenticated request is made to `ActionB`
- THEN `ActionB` executes and returns its result
- AND an unauthenticated request to `ActionA` returns HTTP 401 or redirects to login

### Requirement: Fallback Policy Blocks Access to Dashboard and All Business Controllers

The system SHALL deny unauthenticated access to all protected business controllers that expose user data.

Controllers created in subsequent phases that do not have `[AllowAnonymous]` MUST inherit the fallback authentication requirement from the global policy.

#### Scenario: Unauthenticated Request to Future Protected Controller Denied

- GIVEN no user is authenticated
- AND a future business controller has no `[AllowAnonymous]` attribute
- WHEN an unauthenticated request is made to any action on that controller
- THEN the server returns HTTP 401 Unauthorized or redirects to login

### Requirement: Authorization Failure Redirects to Login Page

When an unauthenticated user attempts to access a protected resource, the system SHALL redirect the user to the configured login page URL.

The `Program.cs` MUST configure `options.LoginPath` or equivalent so that browser requests (HTML) receive a redirect to the login page rather than a raw 401 JSON response.

#### Scenario: Browser Request Redirected to Login

- GIVEN an unauthenticated user makes a browser (HTML Accept header) request to a protected controller action
- WHEN the authorization middleware denies access
- THEN the user is redirected to the login page URL configured in Identity options

### Requirement: API Request Returns 401 on Unauthorized Access

The system SHALL return HTTP 401 Unauthorized without a body for API clients (non-browser) that attempt to access protected resources without authentication.

When the `AuthorizationMiddleware` denies access to a request that does not include an HTML Accept header, the response MUST be HTTP 401 with no redirect.

#### Scenario: API Client Receives 401 Without Redirect

- GIVEN an unauthenticated API client makes a request to a protected controller action with `Accept: application/json`
- WHEN the authorization middleware denies access
- THEN the response is HTTP 401 with an empty body or a minimal JSON `{"error":"unauthorized"}`
