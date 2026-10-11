# Transaction Editing Specification

## Purpose

Define the behavior for editing and deleting user-owned movements in LifeTrack (RF-F04). Editing reuses the invariants RN-F01..RN-F03 and RN-F05. A foreign or non-existent identifier responds "not found" without modifying anything.

## Requirements

### Requirement: Edit Movement Revalidates Invariants

The system SHALL revalidate amount, date, description and category ownership when editing a movement.

The domain mutator MUST apply the same rules as creation (RN-F01..RN-F03) and MUST keep the identifier unchanged. The category MUST belong to the current user (RN-F05).

#### Scenario: Edit Movement with Valid Data Succeeds

- GIVEN the authenticated user owns a movement
- WHEN the user edits it with a valid amount, date and owned category
- THEN the movement is updated and the month view reflects the change

#### Scenario: Edit Movement with Zero Amount Fails

- GIVEN the authenticated user owns a movement
- WHEN the user edits it with amount 0
- THEN a `BusinessRuleException` is raised with a clear message and nothing is saved

#### Scenario: Edit Movement with Future Date Fails

- GIVEN the authenticated user owns a movement
- WHEN the user edits it with tomorrow's date
- THEN a `BusinessRuleException` is raised and nothing is saved

#### Scenario: Edit Movement with Another User's Category Fails

- GIVEN user A owns a category
- AND user B is authenticated and owns a movement
- WHEN user B edits the movement with user A's category
- THEN a `ResourceNotFoundException` is raised and nothing is saved

### Requirement: Delete Movement Removes Only Owned Data

The system SHALL delete a movement only when it belongs to the authenticated user.

Deletion of a foreign or non-existent identifier MUST respond "not found" without modifying anything.

#### Scenario: Delete Own Movement Succeeds

- GIVEN the authenticated user owns a movement
- WHEN the user confirms deletion
- THEN the movement disappears from the month view

#### Scenario: Delete Another User's Movement Fails

- GIVEN user A owns a movement
- AND user B is authenticated
- WHEN user B deletes it by identifier
- THEN a `ResourceNotFoundException` is raised and user A's movement is intact

### Requirement: Unauthenticated Editing Is Rejected

The system SHALL deny editing and deleting without an authenticated session. Handlers MUST fail closed with `BusinessRuleException` and persist nothing.
