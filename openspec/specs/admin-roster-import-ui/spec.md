# admin-roster-import-ui

## Purpose
TBD - defines the admin-facing import wizard: CSV upload, flagged-row review with email/phone assignment and selective submission, and submit (resolve + invite), plus the role-gated admin route it lives in.

## Requirements

### Requirement: Admin-only access to the import wizard
The system SHALL restrict the import wizard to authenticated users with the Admin role, and SHALL deny or redirect any other visitor rather than showing the wizard.

#### Scenario: Authenticated non-admin is denied
- **WHEN** an authenticated player (non-Admin) navigates to the import wizard
- **THEN** the system denies access rather than showing wizard content

#### Scenario: Unauthenticated visitor is redirected to login
- **WHEN** an unauthenticated visitor navigates to the import wizard
- **THEN** the system redirects them to the login screen

### Requirement: CSV import step
The system SHALL let an Admin choose a CSV file and a season year and submit both for import, and SHALL show the resulting counts of games created, rows flagged for review, and rows skipped as already-imported duplicates.

#### Scenario: Successful import
- **WHEN** an Admin submits a well-formed CSV with a season year
- **THEN** the system shows the number of games created, rows flagged for review, and rows skipped as duplicates

#### Scenario: Re-importing already-imported data
- **WHEN** an Admin submits a CSV containing rows that were already imported in a prior run
- **THEN** the system shows those rows counted as skipped duplicates rather than flagging or re-importing them

#### Scenario: Non-CSV file is rejected before upload
- **WHEN** an Admin selects a file that is not a `.csv` file
- **THEN** the system rejects the selection with a clear message and does not submit it for import

### Requirement: Flagged row review
The system SHALL list every currently pending flagged row awaiting resolution, decode and display each row's parsed name, attended dates, total due, and amount paid, and SHALL require an Admin to confirm the player's name and supply an email and phone for a row before it can be submitted.

#### Scenario: Review list shows parsed row detail
- **WHEN** an Admin opens the review step
- **THEN** the system lists every pending flagged row with its parsed name, attended dates, total due, and amount paid

#### Scenario: A row missing email or phone cannot be submitted
- **WHEN** an Admin attempts to submit a row without an email or without a phone filled in
- **THEN** the system blocks submission of that row and indicates the missing field

### Requirement: Selective submission of reviewed rows
The system SHALL let an Admin select which pending rows to submit, rather than requiring every pending row to be processed in the same action, and SHALL leave unselected rows in the pending review queue.

#### Scenario: No rows are selected by default
- **WHEN** an Admin opens the review step
- **THEN** none of the rows are pre-selected for submission

#### Scenario: Submitting a subset leaves the rest pending
- **WHEN** an Admin selects some of the pending rows and submits
- **THEN** only the selected rows are resolved and invited, and the unselected rows remain in the pending review queue

### Requirement: Submit resolves selected rows and sends their invite
The system SHALL, for each selected row an Admin submits, resolve it into a real player account and send that player their first login-link invite in the same action, and SHALL report each row's outcome independently so one row's failure does not block the others.

#### Scenario: Submitting a fully reviewed row succeeds
- **WHEN** an Admin submits a selected row with a confirmed name, email, and phone
- **THEN** the system resolves the row into a Pending player account, sends that player's first login-link invite, and shows the row as succeeded

#### Scenario: One row's submission fails without blocking the others
- **WHEN** an Admin submits multiple selected rows and one row's resolution fails
- **THEN** the system reports that row as failed while still submitting and reporting the outcome of the remaining rows

### Requirement: Duplicate email is rejected, not silently duplicated
The system SHALL prevent a submitted row's email from creating a second player account when a player with that email already exists, and SHALL report this clearly on that row rather than failing unexpectedly.

#### Scenario: Submitted row's email matches an existing player
- **WHEN** an Admin submits a row whose email matches an existing player's email
- **THEN** the system does not create a duplicate account, reports that row as failed with a clear message, and the failure does not block the other selected rows
