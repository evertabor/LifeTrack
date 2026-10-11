# Transaction Import Specification

## Purpose

Define the behavior for importing movements from CSV with preview and validation in LifeTrack (RF-F07). Nothing is persisted before explicit confirmation; only valid rows are imported.

## Requirements

### Requirement: Uploaded File Is Constrained

The system SHALL accept files up to 1 MB, with `.csv` extension, and at most 500 rows.

Oversized files, wrong extensions and empty files MUST be rejected with `BusinessRuleException` before parsing. The server MUST NOT trust the client-provided MIME type.

#### Scenario: Upload Oversized File Fails

- GIVEN the authenticated user uploads a file larger than 1 MB
- WHEN the system processes the upload
- THEN a `BusinessRuleException` is raised and nothing is parsed

### Requirement: Preview Shows Valid and Invalid Rows

The system SHALL parse the CSV with its own parser (no new packages) and validate every row with the same invariants RN-F01..RN-F05.

The preview MUST list valid rows and invalid rows with the per-row error. Category lookup MUST resolve owned categories by name (trimmed, case-insensitive); unknown or foreign categories MUST mark the row invalid.

#### Scenario: Preview Marks Invalid Rows

- GIVEN a CSV with one row with amount 0 and one valid row
- WHEN the user uploads it
- THEN the preview shows one invalid row with its error and one valid row

#### Scenario: Preview with Foreign Category Marks Row Invalid

- GIVEN a CSV row referencing a category of another user
- WHEN the user uploads it
- THEN the row is shown as invalid and nothing is saved

### Requirement: Confirm Imports Only Valid Rows

The system SHALL persist only the valid rows upon explicit confirmation, revalidating each one.

If no row is valid, the system MUST import nothing and report it. Accidental repeats on refresh are prevented by Post/Redirect/Get on confirmation.

#### Scenario: Confirm Imports Valid Rows Only

- GIVEN a preview with two valid rows and one invalid row
- WHEN the user confirms
- THEN exactly the two valid movements are stored and the confirmation redirects without resubmitting on refresh (Post/Redirect/Get)

### Requirement: Unauthenticated Import Is Rejected

The system SHALL deny upload, preview and confirm without an authenticated session, persisting nothing.
