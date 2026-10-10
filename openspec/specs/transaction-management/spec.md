# Transaction Management Specification

## Purpose

Define the behavior for recording income and expense movements and for consulting the monthly summary in LifeTrack. Movements belong to the authenticated user, use only the user's own categories, and respect the invariants RN-F01..RN-F03. The monthly listing and summary cover RF-F03, RF-F05 and RF-D01. Editing, deleting, budgets and CSV import are out of scope.

## Requirements

### Requirement: Transaction Amount Is Valid

The system SHALL require an amount greater than 0, not exceeding 1.000.000.000, with at most 2 decimals (RN-F01).

The domain factory MUST reject amounts that are zero, negative, greater than the maximum, or that carry more than 2 decimals. Each rejection MUST throw a `DomainException` with a message safe to display to the user.

#### Scenario: Register Movement with Valid Amount Succeeds

- GIVEN the authenticated user provides an owned category, amount 25,50 and today's date
- WHEN the system processes the create transaction request
- THEN the movement is stored and returned with a new identifier

#### Scenario: Register Movement with Zero or Negative Amount Fails

- GIVEN the authenticated user provides amount 0 or a negative amount
- WHEN the system processes the create transaction request
- THEN a `DomainException` is raised indicating the amount must be greater than 0

#### Scenario: Register Movement with Three Decimals Fails

- GIVEN the authenticated user provides an amount with 3 decimals
- WHEN the system processes the create transaction request
- THEN a `DomainException` is raised indicating at most 2 decimals are accepted

### Requirement: Transaction Date Is Never in the Future

The system SHALL reject movement dates after "today" in the configured time zone (RN-F02, RN-G01).

The domain factory MUST receive `today` as a parameter and MUST reject any date greater than it. Today's date MUST be accepted.

#### Scenario: Register Movement with Tomorrow's Date Fails

- GIVEN the authenticated user provides tomorrow's date
- WHEN the system processes the create transaction request
- THEN a `DomainException` is raised indicating the date cannot be in the future

#### Scenario: Register Movement with Today's Date Succeeds

- GIVEN the authenticated user provides today's date
- WHEN the system processes the create transaction request
- THEN the movement is stored successfully

### Requirement: Description Is Trimmed and Limited

The system SHALL trim the description and reject descriptions longer than 200 characters (RN-F03).

A null description MUST be stored as an empty string. A description of 201 characters MUST be rejected with a `DomainException`.

#### Scenario: Register Movement Trims Description Whitespace

- GIVEN the authenticated user provides description "  Compra  "
- WHEN the system processes the create transaction request
- THEN the stored description is "Compra"

### Requirement: Movement Uses Only Owned Categories

The system SHALL only accept categories belonging to the authenticated user (RN-F05).

When the category does not exist or belongs to another user, the system MUST respond with `ResourceNotFoundException` and MUST NOT persist anything.

#### Scenario: Register Movement with Another User's Category Fails

- GIVEN user A owns a category
- AND user B is authenticated
- WHEN user B registers a movement with that category
- THEN a `ResourceNotFoundException` is raised and nothing is saved

### Requirement: Monthly Listing Returns the Requested Month

The system SHALL list the movements of the requested year and month, optionally filtered by one category (RF-F03).

The query MUST validate the period (year 2000–2100, month 1–12) and reject invalid periods with `BusinessRuleException`. When a category filter is provided, the system MUST first verify the category belongs to the current user. Only movements with dates in `[first day of month, first day of next month)` MUST be returned.

#### Scenario: List October Movements Excludes September

- GIVEN the user has movements in September and October
- WHEN the user requests October
- THEN only October movements are returned

#### Scenario: List with Invalid Period Fails

- GIVEN the user requests month 13
- WHEN the system processes the query
- THEN a `BusinessRuleException` is raised indicating the period is not valid

#### Scenario: List with Another User's Category Filter Fails

- GIVEN user A owns a category
- AND user B is authenticated
- WHEN user B lists with that category as filter
- THEN a `ResourceNotFoundException` is raised

### Requirement: Monthly Summary Shows Totals and Balance

The system SHALL show income, expenses, balance and expense grouped by category for the month (RF-F05, RF-D01, HU-06).

The balance MUST equal income minus expenses. The grouped expense MUST sum to the total expense. A month without movements MUST return zeros without errors.

#### Scenario: Summary Adds Up

- GIVEN income 1000 and expenses 400 in the month
- WHEN the user requests the monthly summary
- THEN the balance shown is 600 and the grouped expense sums to 400

#### Scenario: Empty Month Shows Zeros

- GIVEN a month without movements
- WHEN the user requests the monthly summary
- THEN income, expenses and balance are zero and the list is empty

### Requirement: Unauthenticated Access Is Rejected

The system SHALL deny listing and creating movements without an authenticated session.

Anonymous requests MUST be redirected to the login page. Handlers MUST reject a missing user with `BusinessRuleException` and persist nothing.

#### Scenario: Anonymous List Redirects to Login

- GIVEN no user is authenticated
- WHEN a request for the movements page is submitted
- THEN the system redirects to `/Account/Login`

#### Scenario: Create Movement Without User Fails Closed

- GIVEN no user is authenticated
- WHEN the create handler runs
- THEN a `BusinessRuleException` is raised and nothing is saved

### Requirement: User Isolation Prevents IDOR

The system SHALL ensure a user cannot read or use another user's movements or categories through direct object reference.

Every transaction query MUST include the current user's identifier in the filter condition. Cross-user access MUST receive the same response as a non-existent resource.

#### Scenario: User Cannot List Another User's Movements

- GIVEN user A has movements in the month
- AND user B is authenticated
- WHEN user B requests the same month
- THEN user A's movements are not returned
