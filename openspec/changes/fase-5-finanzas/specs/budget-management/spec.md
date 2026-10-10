# Budget Management Specification

## Purpose

Define the behavior for monthly per-category budgets in LifeTrack (RF-F06, RN-F06, HU-07). A budget is unique per user, category, year and month, with a limit greater than 0 and 2 decimals. Consumption shows a warning at 80 % and an exceeded notice at 100 %.

## Requirements

### Requirement: Budget Limit Is Valid and Unique per Period

The system SHALL require a limit greater than 0 with at most 2 decimals, and SHALL enforce one budget per user, category, year and month (RN-F06).

The domain factory MUST reject zero or negative limits, limits above 1.000.000.000, and limits with more than 2 decimals with a `DomainException`. A duplicate period for the same user and category MUST be rejected with `BusinessRuleException`.

#### Scenario: Set Budget with Valid Limit Succeeds

- GIVEN the authenticated user owns a category
- WHEN the user sets limit 100 for October 2026
- THEN the budget is stored for that period

#### Scenario: Set Budget with Zero Limit Fails

- GIVEN the authenticated user owns a category
- WHEN the user sets limit 0
- THEN a `DomainException` is raised and nothing is saved

#### Scenario: Set Duplicate Budget for Same Period Fails

- GIVEN the user already has a budget for a category and month
- WHEN the user sets another budget for the same category and month
- THEN a `BusinessRuleException` is raised and the original is kept

#### Scenario: Set Budget with Another User's Category Fails

- GIVEN user A owns a category
- AND user B is authenticated
- WHEN user B sets a budget on that category
- THEN a `ResourceNotFoundException` is raised and nothing is saved

### Requirement: Budget Consumption Warns at 80 and 100 Percent

The system SHALL report spent amount, percentage and state (`ok`, `warning`, `exceeded`) per budget (HU-07).

With limit 100 and spent 80 the state MUST be `warning`; with spent greater than or equal to 100 the state MUST be `exceeded`. The view MUST always show text, never color alone.

#### Scenario: Spending at 80 Percent Warns

- GIVEN a budget of 100 with accumulated spending of 80
- WHEN the user opens the month view
- THEN a consumption warning at 80 % is shown

#### Scenario: Spending at or Above Limit Shows Exceeded

- GIVEN a budget of 100 with accumulated spending of 100 or more
- WHEN the user opens the month view
- THEN a budget-exceeded notice is shown

### Requirement: Budget Period Is Valid

The system SHALL validate year (2000–2100) and month (1–12) and reject invalid periods with `BusinessRuleException`.

### Requirement: Unauthenticated Budget Access Is Rejected

The system SHALL deny budget operations without an authenticated session. Handlers MUST fail closed and persist nothing.
