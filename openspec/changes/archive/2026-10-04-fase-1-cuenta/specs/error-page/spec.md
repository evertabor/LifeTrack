# Error Page Specification

## Purpose

Define the application error page that displays a unique request identifier for correlation without leaking internal stack traces or exception details to end users. All exceptions are logged server-side with full detail; the user receives a safe, generic error message.

## Requirements

### Requirement: Error Page Displays Request Identifier

The system SHALL display a unique request identifier on the error page to enable users to report it when contacting support.

The request identifier MUST be a non-sequential, non-guessable identifier (e.g., a GUID or correlation ID) generated per request. The identifier MUST be included in the error page view and MUST be present in server-side log entries for that request.

#### Scenario: Error Page Shows Request ID

- GIVEN an unhandled exception occurs during request processing
- WHEN the error page is rendered for the user
- THEN the page displays a unique request identifier that can be used to look up the error in server logs

#### Scenario: Request ID Included in Server Logs

- GIVEN an unhandled exception occurs during request processing
- WHEN the exception is logged by the server
- THEN the logged entry includes the same request identifier that is displayed to the user

### Requirement: Error Page Does Not Leak Internal Exception Details

The system SHALL display a generic, safe error message to end users. The error page MUST NOT display `ex.Message`, stack traces, `StackTrace`, `InnerException` details, SQL queries, or any other internal information.

The error page view MUST display only a generic message such as "An unexpected error occurred. Please try again later." or a equivalent message in Spanish ("Ha ocurrido un error inesperado. Por favor, inténtelo de nuevo más tarde.").

#### Scenario: Error Page Does Not Show Stack Trace

- GIVEN an unhandled `NullReferenceException` occurs with message "Object reference not set to object"
- WHEN the error page is rendered for the user
- THEN the page does not contain the text "Object reference not set" or any stack trace information

#### Scenario: Error Page Does Not Show SQL Query Details

- GIVEN an unhandled database exception occurs with a SQL error message
- WHEN the error page is rendered for the user
- THEN the page does not contain SQL query text, table names, or database connection details

### Requirement: Error Page Is Accessible Without Authentication

The error page MUST be accessible to unauthenticated users. An unauthenticated user who encounters an error MUST be able to see the error page without being redirected to login.

The `ErrorController` MUST be annotated with `[AllowAnonymous]` or the route must be excluded from the global authorization policy.

#### Scenario: Unauthenticated User Can View Error Page

- GIVEN no user is authenticated
- AND an unhandled exception occurs
- WHEN the error page is rendered
- THEN the user sees the error page without being redirected to the login page

### Requirement: ErrorController Handles Unhandled Exceptions

The `ErrorController` SHALL have an action (e.g., `Index`) that receives an exception or error indicator and renders the error view.

The controller MUST be registered in `Program.cs` as the endpoint for unhandled exceptions via `UseExceptionHandler` or `app.UseStatusCodePagesWithReExecute` for non-success status codes.

#### Scenario: 500 Internal Server Error Renders Error Page

- GIVEN an unhandled exception occurs during request processing
- WHEN `UseExceptionHandler` transfers control to `ErrorController`
- THEN the `Index` action is invoked and the error view is rendered

#### Scenario: 404 Not Found Renders Error Page

- GIVEN a request is made to a non-existent route
- WHEN the routing middleware returns 404
- THEN the error page is rendered with the appropriate HTTP status code

### Requirement: Request ID Is Passed to Error View via Model or ViewData

The error view MUST receive the request identifier via its model (`ErrorViewModel`) or `ViewData["RequestId"]`.

The error view MUST NOT generate its own identifier — it MUST use the identifier provided by the controller, which is derived from the logging scope or HTTP context items.

#### Scenario: Error View Receives Request ID From Controller

- GIVEN an error has occurred and the `ErrorController` has resolved the request identifier from the current `HttpContext.TraceIdentifier`
- WHEN the controller passes the identifier to the error view
- THEN the view renders the identifier exactly as provided, without modification

### Requirement: Logging Captures Full Exception Detail Server-Side

The system SHALL log the full exception (type, message, stack trace, inner exceptions) with structured logging using `ILogger` with the request identifier as a log scope property.

Log entries MUST include the request identifier so that operators can correlate user-reported identifiers with specific exceptions in the log store.

#### Scenario: Structured Log Entry Contains Request ID and Exception Detail

- GIVEN an unhandled `InvalidOperationException` occurs during request processing
- WHEN the exception is logged via `ILogger.LogError`
- THEN the log entry contains structured properties including `requestId`, `exceptionType`, and the full exception message and stack trace

### Requirement: Error Page Message Is User-Friendly in Spanish

The error page SHALL display its user-facing message in Spanish. All labels, headings, and user-visible text on the error page MUST be in Spanish.

The generic error message shown to the user MUST be: "Ha ocurrido un error inesperado. Por favor, inténtelo de nuevo más tarde." The request ID label MUST be in Spanish (e.g., "Identificador de solicitud:").

#### Scenario: Error Page Text Is in Spanish

- GIVEN an unhandled exception occurs
- WHEN the error page is rendered
- THEN the displayed message is "Ha ocurrido un error inesperado. Por favor, inténtelo de nuevo más tarde."
- AND the request ID label is in Spanish

### Requirement: Error Page Layout Matches Application Layout

The error page SHOULD be rendered using the application's shared layout (or a minimal layout) so that navigation and branding remain consistent.

The error view MUST be placed in `Views/Error/Index.cshtml` and MUST use the application's `_Layout.cshtml` or a minimal safe fallback layout.

#### Scenario: Error Page Uses Application Layout

- GIVEN an unhandled exception occurs
- WHEN the error page is rendered
- THEN the page includes the application header, navigation, and footer (if present in the layout)
