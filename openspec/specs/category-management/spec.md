# Category Management Specification

## Purpose

Define the behavior for creating and listing user-owned categories in LifeTrack. Categories are isolated per authenticated user. The name is mandatory, normalized with `Trim()` before canonical storage and uniqueness comparison, limited to 50 characters maximum, and must be unique per user. Rename and delete are out of scope.

## Requirements

### Requirement: Category Name Is Mandatory

The system SHALL require a non-empty, non-whitespace-only name when creating a category.

The domain entity MUST reject an empty string or a string that becomes empty after applying `Trim()`. The rejection MUST throw a `DomainException` with a message safe to display to the user.

#### Scenario: Create Category with Valid Name Succeeds

- GIVEN the authenticated user provides a valid category name
- WHEN the system processes the create category request
- THEN the category is created with that name stored in canonical form (trimmed)

#### Scenario: Create Category with Empty Name Fails

- GIVEN the authenticated user submits a create category request with an empty name
- WHEN the system processes the request
- THEN a `DomainException` is raised indicating the name is mandatory

#### Scenario: Create Category with Whitespace-Only Name Fails

- GIVEN the authenticated user submits a create category request with a name that contains only spaces
- WHEN the system processes the request
- THEN a `DomainException` is raised indicating the name is mandatory

### Requirement: Category Name Maximum Length

The system SHALL reject category names that exceed 50 characters after `Trim()` is applied.

The domain entity MUST reject a name longer than 50 characters. The rejection MUST throw a `DomainException` with a message safe to display to the user.

#### Scenario: Create Category with 50 Characters Succeeds

- GIVEN the authenticated user provides a category name of exactly 50 characters
- WHEN the system processes the create category request
- THEN the category is created successfully

#### Scenario: Create Category with 51 Characters Fails

- GIVEN the authenticated user provides a category name of 51 characters
- WHEN the system processes the create category request
- THEN a `DomainException` is raised indicating the maximum length is 50 characters

### Requirement: Category Name Is Normalized Before Storage

The system SHALL apply `Trim()` to the category name before storing it as canonical value.

The domain entity factory MUST call `Trim()` on the input name. All subsequent comparisons, including uniqueness checks, MUST use the trimmed value. Storage and retrieval MUST return the trimmed value.

#### Scenario: Create Category Trims Leading and Trailing Whitespace

- GIVEN the authenticated user provides a name with leading spaces "  Comida" and trailing spaces "Comida  "
- WHEN the system processes the create category request
- THEN the stored category name is "Comida" (whitespace removed)

### Requirement: Category Names Must Be Unique Per User

The system SHALL enforce uniqueness of category names per authenticated user.

The uniqueness check MUST compare trimmed names. Two names that differ only in leading or trailing whitespace MUST be treated as duplicates. The system MUST return the exact message "Ya existe una categoría con ese nombre." when a duplicate trimmed name is detected for the same user.

#### Scenario: Create Category with Duplicate Trimmed Name Fails

- GIVEN the authenticated user already has a category named "Comida"
- WHEN the user submits a create request for "  Comida  " (same name with whitespace)
- THEN a `BusinessRuleException` is raised with the message "Ya existe una categoría con ese nombre."

#### Scenario: Create Category with Same Name for Different Users Succeeds

- GIVEN user A has a category named "Comida"
- AND user B is authenticated
- WHEN user B creates a category named "Comida"
- THEN user B's category is created successfully

#### Scenario: Create Category with Different Trimmed Names Succeeds

- GIVEN the authenticated user already has a category named "Comida"
- WHEN the user creates a category named "Transporte" (different canonical name)
- THEN the category is created successfully

### Requirement: List Categories Returns Only Current User's Categories

The system SHALL return only the categories belonging to the currently authenticated user.

The list query MUST filter by the current user's identifier. The system MUST NOT return categories owned by other users. If the user has no categories, the system SHALL return an empty list.

#### Scenario: List Categories Returns Only User's Own Categories

- GIVEN the authenticated user has categories named "Comida" and "Transporte"
- AND another user has categories named "Gastos" and "Ingresos"
- WHEN the user requests the list of categories
- THEN only "Comida" and "Transporte" are returned

#### Scenario: List Categories Returns Empty List for New User

- GIVEN the authenticated user has no categories
- WHEN the user requests the list of categories
- THEN an empty list is returned

### Requirement: Unauthenticated Access Is Rejected

The system SHALL deny access to create or list categories when no user is authenticated.

Requests without a valid authenticated session MUST be rejected with HTTP 401 or redirected to the login page. The system MUST NOT create or return any category data for unauthenticated requests.

#### Scenario: Create Category Without Authentication Fails

- GIVEN no user is authenticated
- WHEN a request to create a category is submitted
- THEN the system returns HTTP 401 or redirects to login

#### Scenario: List Categories Without Authentication Fails

- GIVEN no user is authenticated
- WHEN a request to list categories is submitted
- THEN the system returns HTTP 401 or redirects to login

### Requirement: User Isolation Prevents IDOR

The system SHALL ensure that a user cannot access another user's categories through direct object reference.

Each category query MUST include the current user's identifier in the filter condition. A user attempting to access a category by manipulating an identifier belonging to another user MUST receive the same response as if the category did not exist.

#### Scenario: User Cannot Access Another User's Category by ID

- GIVEN user A has a category with a specific ID
- AND user B is authenticated
- WHEN user B attempts to access or manipulate user A's category using that ID
- THEN the system returns a "not found" response without revealing the category exists
