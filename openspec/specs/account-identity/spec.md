# Account Identity Specification

## Purpose

Establish the user-account lifecycle for LifeTrack: registration with password, login, and logout using ASP.NET Core Identity via a custom MVC `AccountController` with Razor views. Covers HU-01 (registro), HU-02 (inicio de sesión), and HU-03 (cierre de sesión). Enforces OWASP A07 password policy and account lockout.

## Requirements

### Requirement: User Registration — Valid Input

The system SHALL allow a visitor to register a new account by providing a valid email address and a password that meets the configured policy.

The `AccountController` MUST validate the registration input model before processing. The password MUST be at least 12 characters long. The email MUST be a valid email format and MUST NOT already exist in the system. Upon successful registration, the user account MUST be created in the ASP.NET Core Identity store and the user MUST be automatically signed in.

#### Scenario: Successful Registration with Valid Email and Compliant Password

- GIVEN a visitor is on the registration page at `/Account/Register`
- WHEN the visitor submits the form with an email that does not exist in the system and a password of 12 or more characters
- THEN the system creates the user account in the Identity store, signs the user in automatically, and redirects to the home page

#### Scenario: Registration Fails — Duplicate Email

- GIVEN a visitor is on the registration page at `/Account/Register`
- AND an account with the email `user@example.com` already exists
- WHEN the visitor submits the form with email `user@example.com` and a valid password
- THEN the system returns the registration page with a model error indicating the email is already in use, and no new account is created

#### Scenario: Registration Fails — Password Too Short

- GIVEN a visitor is on the registration page at `/Account/Register`
- WHEN the visitor submits the form with a valid email and a password of 11 characters
- THEN the system returns the registration page with a model error indicating the password does not meet the minimum length requirement

#### Scenario: Registration Fails — Missing Required Fields

- GIVEN a visitor is on the registration page at `/Account/Register`
- WHEN the visitor submits the form with empty email and empty password fields
- THEN the system returns the registration page with model errors for each missing required field, and no account is created

### Requirement: User Login — Valid Credentials

The system SHALL allow a registered user to log in by presenting valid credentials.

The `AccountController` MUST accept email and password. The system MUST verify the password against the Identity store using ASP.NET Core Identity's `PasswordSignInAsync`. Upon successful login, the user MUST be authenticated and redirected to the home page. The login failure message MUST be generic ("Invalid email or password.") to avoid user enumeration.

#### Scenario: Successful Login with Correct Credentials

- GIVEN a registered user exists with email `user@example.com` and password `ValidPassword123!`
- AND the user is on the login page at `/Account/Login`
- WHEN the user submits the form with `user@example.com` and `ValidPassword123!`
- THEN the system authenticates the user, creates a session cookie, and redirects to the home page

#### Scenario: Login Fails — Incorrect Password

- GIVEN a registered user exists with email `user@example.com` and password `ValidPassword123!`
- AND the user is on the login page at `/Account/Login`
- WHEN the user submits the form with `user@example.com` and `WrongPassword456!`
- THEN the system returns the login page with a generic error message "Invalid email or password.", and the user remains unauthenticated

#### Scenario: Login Fails — Non-Existent Email

- GIVEN no account exists with email `nonexistent@example.com`
- AND the visitor is on the login page at `/Account/Login`
- WHEN the visitor submits the form with `nonexistent@example.com` and `AnyPassword123!`
- THEN the system returns the login page with a generic error message "Invalid email or password.", and no account is created

#### Scenario: Login Fails — Account Locked Out

- GIVEN a registered user exists with email `locked@example.com`
- AND the account has been locked out due to excessive failed login attempts
- AND the visitor is on the login page at `/Account/Login`
- WHEN the visitor submits the form with `locked@example.com` and the correct password
- THEN the system returns the login page with a generic error message "Invalid email or password.", and the user remains unauthenticated

### Requirement: User Logout

The system SHALL allow an authenticated user to log out, destroying the local session cookie and invalidating the authentication ticket.

The `AccountController` logout action MUST call `SignOutAsync` on the `SignInManager<ApplicationUser>`. After logout, the user MUST be redirected to the home page and MUST NOT be able to access protected resources without re-authenticating.

#### Scenario: Successful Logout from Authenticated Session

- GIVEN an authenticated user is logged in
- WHEN the user navigates to `/Account/Logout` and confirms the logout action
- THEN the system signs the user out, clears the authentication cookie, and redirects to the home page

#### Scenario: Logout Destroys Session

- GIVEN an authenticated user is logged in
- WHEN the user performs logout
- THEN subsequent requests from the same browser session MUST return `401` or redirect to login when accessing a protected controller action

### Requirement: Account Controller Uses MediatR ISender

The `AccountController` MUST delegate all business logic to MediatR `ISender`. The controller MUST NOT call repositories, `DbContext`, or `UserManager`/`SignInManager` directly for business rules. Password validation and Identity operations are infrastructure concerns handled within Application-layer handlers.

The controller SHALL inject `ISender` and `ILogger<AccountController>`. Each action method SHALL call `sender.Send(command, cancellationToken)` where `command` is a MediatR request record.

#### Scenario: Controller Delegates Registration to ISender

- GIVEN a visitor submits the registration form with valid input
- WHEN `AccountController.Register(RegisterViewModel model, ...)` is invoked
- THEN the controller creates a MediatR command record and dispatches it via `ISender.Send`, returning the appropriate view or redirect

#### Scenario: Controller Delegates Login to ISender

- GIVEN a user submits the login form with valid credentials
- WHEN `AccountController.Login(LoginViewModel model, ...)` is invoked
- THEN the controller creates a MediatR command record and dispatches it via `ISender.Send`, returning the appropriate view or redirect

### Requirement: Registration Input Validation

The system MUST validate all registration inputs before creating a user account.

The `RegisterViewModel` MUST use DataAnnotations for validation. The email field MUST be non-empty and must be a valid email format. The password field MUST meet the Identity password policy (minimum 12 characters). Client-side validation MUST be enabled and server-side validation MUST re-enforce the same rules on every POST.

#### Scenario: Empty Email Rejected

- GIVEN a visitor is on the registration page at `/Account/Register`
- WHEN the visitor submits the form with an empty email field
- THEN the system returns the registration page with a validation error on the email field, without posting to the server handler

#### Scenario: Invalid Email Format Rejected

- GIVEN a visitor is on the registration page at `/Account/Register`
- WHEN the visitor submits the form with an email of `not-an-email`
- THEN the system returns the registration page with a validation error indicating the email format is invalid

#### Scenario: Password Under Minimum Length Rejected

- GIVEN a visitor is on the registration page at `/Account/Register`
- WHEN the visitor submits the form with a password of exactly 11 characters
- THEN the system returns the registration page with a validation error indicating the password does not meet the minimum length requirement

### Requirement: OWASP A07 Password Policy Compliance

The system SHALL enforce OWASP A07 password requirements through ASP.NET Core Identity's built-in password validator.

Passwords MUST have at least 12 characters. The system MUST use Identity's `PasswordValidator<TUser>` with default configuration. Account lockout MUST be enabled with a maximum of 5 failed attempts and a lockout duration of 5 minutes.

#### Scenario: Password Without Uppercase Character Rejected

- GIVEN a visitor is on the registration page at `/Account/Register`
- WHEN the visitor submits the form with a password of `onlylowercase123`
- THEN the system returns the registration page with a validation error indicating the password does not meet the password requirements

#### Scenario: Account Locks After Maximum Failed Attempts

- GIVEN a registered user exists
- AND the visitor attempts to log in with the correct email but incorrect password 5 consecutive times
- WHEN the visitor attempts to log in a 6th time with the correct password
- THEN the system returns a generic login error and the account remains locked for the configured lockout duration

### Requirement: Login Input Validation

The system MUST validate login inputs before processing authentication.

The `LoginViewModel` MUST require both email and password fields. Both fields MUST be non-empty. The email MUST be a valid format. Validation errors MUST be displayed without revealing whether the account exists.

#### Scenario: Empty Password Rejected

- GIVEN a visitor is on the login page at `/Account/Login`
- WHEN the visitor submits the form with a valid email and an empty password
- THEN the system returns the login page with a validation error on the password field

### Requirement: Anti-Forgery Protection on Account Forms

The system MUST enforce anti-forgery token validation on all account-related POST forms.

Every form rendered by `AccountController` actions MUST include an anti-forgery token via the `asp-antiforgery` attribute or Tag Helpers. The `Program.cs` MUST have `AutoValidateAntifgeryToken` registered as a global filter. POST requests to account actions without a valid anti-forgery token MUST be rejected with HTTP 400.

#### Scenario: POST Without Anti-Forgery Token Rejected

- GIVEN an unauthenticated user attempts to submit a login POST request to `/Account/Login` without an anti-forgery token
- WHEN the request is received by the server
- THEN the server returns HTTP 400 Bad Request

### Requirement: Generic Error Messages on Login and Registration

The system MUST return generic, user-enumeration-resistant error messages on authentication failures.

Login failure messages MUST say "Invalid email or password." and MUST NOT indicate whether the failure was due to a non-existent email or a wrong password. Registration failure messages for duplicate emails MUST say "An account with this email already exists." and MUST NOT reveal whether the account is active, locked, or disabled.

#### Scenario: Login Error Message Does Not Reveal Account Existence

- GIVEN a visitor attempts to log in with email `nonexistent@example.com` and any password
- WHEN the system returns a login failure
- THEN the displayed message is "Invalid email or password." and does not indicate whether the email is registered
