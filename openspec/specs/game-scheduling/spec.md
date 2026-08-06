# game-scheduling

## Purpose
TBD - defines how recurring and ad-hoc league games are scheduled, generated, and opened for sign-up.

## Requirements

### Requirement: Recurring game template
The system SHALL allow an Admin to define a recurring game template holding default day, time, capacity, fee, and a configurable sign-up lead time, from which game instances are generated.

#### Scenario: Admin defines the template
- **WHEN** an Admin sets the recurring day, time, default capacity, fee, and sign-up lead time
- **THEN** the system stores the template and uses it to generate future game instances

### Requirement: Configurable sign-up lead time
The system SHALL open a game for sign-up at its start time minus a configurable sign-up lead time (default 1 day), and SHALL keep the game materialized but closed before that moment.

#### Scenario: Game not yet within lead window
- **WHEN** the current time is earlier than a game's start time minus the sign-up lead time
- **THEN** the game exists but is closed and does not accept sign-ups

#### Scenario: Lead window begins
- **WHEN** the current time reaches a game's start time minus the sign-up lead time
- **THEN** the game opens for sign-up

#### Scenario: Lead time must be shorter than the inter-game interval
- **WHEN** an Admin sets a sign-up lead time equal to or longer than the interval between recurring games
- **THEN** the system rejects the value to preserve the single-live-game invariant

### Requirement: Single live game at a time
The system SHALL keep at most one game open for sign-up at any given time, and MAY have a quiet gap during which no game is open (between a game's start time and the next game's lead window opening).

#### Scenario: Only one game accepts sign-ups
- **WHEN** a player views games available to sign up for
- **THEN** at most one open game is presented

#### Scenario: Quiet gap between games
- **WHEN** a game's start time has passed and the next game's lead window has not yet begun
- **THEN** no game is open for sign-up

### Requirement: Time-based auto-generation of the next game
The system SHALL automatically materialize the next game instance from the template when the current live game's start time passes, without requiring admin action.

#### Scenario: Current game time passes
- **WHEN** the current game's start time passes
- **THEN** the system automatically creates the next game instance from the template (materialized and closed until its own lead window begins)

#### Scenario: Completed game awaits reconciliation
- **WHEN** a game's start time has passed
- **THEN** the system moves that game into a "past games awaiting reconciliation" list available to the Admin

### Requirement: Ad-hoc games
The system SHALL allow an Admin to create one-off ad-hoc games independent of the recurring template.

#### Scenario: Admin creates a one-off game
- **WHEN** an Admin creates an ad-hoc game with its own date, time, capacity, and fee
- **THEN** the system creates that game instance without altering the recurring template

### Requirement: Per-game capacity with default and override
The system SHALL apply the template's default capacity to generated games and SHALL allow an Admin to override capacity for an individual game.

#### Scenario: Admin overrides capacity for one game
- **WHEN** an Admin sets a different capacity on a specific game
- **THEN** that game uses the overridden capacity while other games keep the default

### Requirement: Independent game instances
The system SHALL treat each generated game as an independent instance, such that editing, skipping, or cancelling one game does not affect other games or require series-level editing.

#### Scenario: Admin cancels a single game
- **WHEN** an Admin cancels or edits one game instance
- **THEN** only that instance is affected and no other games or the template change
