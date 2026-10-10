# Current User Abstraction Specification

## Purpose

Define the `ICurrentUser` interface in the Application layer that exposes the current authenticated user's identity. The implementation lives in the Web layer and reads from the authenticated `ClaimsPrincipal`. The abstraction fails closed when no authenticated principal is present, ensuring that unauthenticated callers cannot silently access user-scoped data.

## Requirements

### Requirement: ICurrentUser Exposes UserId Property

The `ICurrentUser` interface SHALL declare a `UserId` property of type `Guid?` that returns the identifier of the currently authenticated user.

The `ICurrentUser` interface MUST be defined in `LifeTrack.Application/Abstractions/ICurrentUser.cs` and MUST NOT depend on ASP.NET Core, HTTP, or Identity types. The `UserId` property returns `null` when no user is authenticated.

#### Scenario: UserId Returns Correct Guid for Authenticated User

- GIVEN a user is authenticated with a valid `ClaimsPrincipal` containing a `NameIdentifier` claim with a valid Guid value
- WHEN `ICurrentUser.UserId` is accessed
- THEN the returned value is the Guid from the `NameIdentifier` claim

#### Scenario: UserId Returns Null for Unauthenticated Request

- GIVEN no user is authenticated (no `ClaimsPrincipal` or an anonymous principal)
- WHEN `ICurrentUser.UserId` is accessed
- THEN the returned value is `null`

### Requirement: ICurrentUser Implementation Reads ClaimsPrincipal

The `CurrentUser` service implementation in the Web layer SHALL read the authenticated `ClaimsPrincipal` from the current HTTP context.

The implementation MUST be in `LifeTrack.Web/Services/CurrentUser.cs` and MUST implement `ICurrentUser`. It MUST retrieve the `NameIdentifier` claim from `HttpContext.User`. The implementation MUST NOT throw exceptions when no user is authenticated — it MUST return `null` for `UserId`.

#### Scenario: CurrentUser Reads NameIdentifier Claim from HttpContext

- GIVEN an HTTP request is in progress with an authenticated `ClaimsPrincipal` containing `ClaimTypes.NameIdentifier` equal to `d9b53e1a-7c3d-4f2b-a1c0-3f8e7d6a9b2e`
- WHEN `CurrentUserService.UserId` is accessed
- THEN the returned value is `Guid.Parse("d9b53e1a-7c3d-4f2b-a1c0-3f8e7d6a9b2e")`

### Requirement: ICurrentUser Fails Closed When No Principal Is Present

The `ICurrentUser` implementation MUST NOT throw an exception when `HttpContext.User` is unauthenticated or missing. It MUST return `null` for `UserId`.

Application-layer code that depends on `ICurrentUser` MUST handle the `null` case explicitly (fail-closed: no identity means no data access).

#### Scenario: CurrentUser Returns Null When HttpContext Has No Authenticated User

- GIVEN the HTTP context has an unauthenticated (anonymous) `ClaimsPrincipal`
- WHEN `CurrentUserService.UserId` is accessed
- THEN `null` is returned and no exception is thrown

#### Scenario: CurrentUser Returns Null When HttpContext.User Is Null

- GIVEN the `HttpContext.User` is `null`
- WHEN `CurrentUserService.UserId` is accessed
- THEN `null` is returned and no exception is thrown

### Requirement: ICurrentUser Is Registered as Scoped Service

The `ICurrentUser` abstraction MUST be registered in the DI container as a scoped service so that each HTTP request gets its own instance.

`Program.cs` or the composition root MUST register `CurrentUser` implementation as `Scoped` lifetime and bind it to `ICurrentUser`.

#### Scenario: ICurrentUser Injected Per Request

- GIVEN two concurrent HTTP requests are processed by the application
- WHEN `ICurrentUser` is resolved from the DI container for each request
- THEN each resolved instance is independent (request-scoped), and the `UserId` of one request does not affect the other

### Requirement: Application Handlers Use ICurrentUser for Authorization

All Application-layer handlers that access user-scoped data MUST receive `ICurrentUser` and MUST filter queries by `ICurrentUser.UserId`.

Handlers that require authentication MUST check `ICurrentUser.UserId` and MUST NOT proceed when it is `null`, raising `ResourceNotFoundException` or returning an empty result rather than executing a query without a user filter.

#### Scenario: Handler Fails Closed When UserId Is Null

- GIVEN a query handler requires user context (e.g., fetching habits for the current user)
- AND `ICurrentUser.UserId` is `null`
- WHEN the handler is invoked
- THEN the handler raises `ResourceNotFoundException` or returns an empty result set rather than executing an unfiltered query

### Requirement: ICurrentUser NameIdentifier Claim Resolution Is Robust

The `CurrentUser` implementation MUST handle missing or malformed `NameIdentifier` claims gracefully.

If the `NameIdentifier` claim exists but its value is not a valid `Guid`, the implementation MUST return `null` for `UserId` and MUST NOT throw a format exception.

#### Scenario: Malformed NameIdentifier Claim Returns Null

- GIVEN the authenticated `ClaimsPrincipal` contains a `NameIdentifier` claim with value `not-a-guid`
- WHEN `CurrentUserService.UserId` is accessed
- THEN `null` is returned and no exception is thrown
