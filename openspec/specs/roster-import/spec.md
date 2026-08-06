# roster-import

## Purpose
TBD - defines the one-time, admin-run import of the league's existing spreadsheet roster into the app.

## Requirements

### Requirement: One-time spreadsheet import
The system SHALL provide an admin-run, one-time import that reads the league's existing spreadsheet and creates player roster records with their names, contact info, and starting balances.

#### Scenario: Import a valid spreadsheet
- **WHEN** an Admin runs the import against a well-formed spreadsheet
- **THEN** the system creates one roster record per player row, preserving name, contact info, and starting balance

#### Scenario: App becomes source of truth after import
- **WHEN** the import completes and cutover is confirmed
- **THEN** the app is the authoritative source of truth and the spreadsheet is no longer read or written by the system

### Requirement: Import validation and review
The system SHALL validate imported rows and surface unmatched, ambiguous, or malformed rows for admin review rather than silently dropping or guessing them.

#### Scenario: Malformed or ambiguous row
- **WHEN** a row is missing required fields or cannot be unambiguously interpreted
- **THEN** the system flags that row for admin review and does not create a partial or guessed record

#### Scenario: Row missing required contact info
- **WHEN** an imported row is missing a required email or phone number
- **THEN** the system flags that row for admin review rather than importing an incomplete record

#### Scenario: Admin resolves flagged rows
- **WHEN** an Admin reviews and corrects a flagged row
- **THEN** the system imports the corrected record

### Requirement: Idempotent, non-duplicating import
The system SHALL prevent the one-time import from creating duplicate roster records if run against already-imported data.

#### Scenario: Import run twice
- **WHEN** the import is run a second time against data already imported
- **THEN** the system does not create duplicate roster records
