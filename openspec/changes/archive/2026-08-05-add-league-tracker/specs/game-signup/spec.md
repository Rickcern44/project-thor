## ADDED Requirements

### Requirement: Player self-service sign-up
The system SHALL allow an authenticated Player to sign themselves up for the live game while capacity remains, adding them to the game roster.

#### Scenario: Sign up with space available
- **WHEN** a Player signs up for the live game and the roster is below capacity
- **THEN** the system adds the Player to the roster

### Requirement: Player self-service cancellation
The system SHALL allow an authenticated Player to cancel their own sign-up, removing them from the roster or the waitlist.

#### Scenario: Player cancels their roster spot
- **WHEN** a rostered Player cancels their sign-up
- **THEN** the system removes them from the roster and their spot becomes open

#### Scenario: Player cancels their waitlist spot
- **WHEN** a waitlisted Player cancels
- **THEN** the system removes them from the waitlist

### Requirement: Waitlist overflow
The system SHALL place a Player on the waitlist, rather than the roster, when they sign up for a game that is at capacity.

#### Scenario: Sign up when full
- **WHEN** a Player signs up for a game whose roster is at capacity
- **THEN** the system adds the Player to the waitlist in arrival order

### Requirement: Admin-decided waitlist promotion
The system SHALL NOT auto-promote from the waitlist; when a roster spot is open and a waitlist exists, the system SHALL let an Admin choose which waitlisted player to promote.

#### Scenario: Spot opens with a waitlist present
- **WHEN** a roster spot opens and one or more players are waitlisted
- **THEN** the system does not automatically move anyone up and instead lets an Admin select who to promote

#### Scenario: Admin promotes a waitlisted player
- **WHEN** an Admin promotes a chosen waitlisted player into an open roster spot
- **THEN** the system moves that player to the roster and removes them from the waitlist

### Requirement: Admin roster management
The system SHALL allow an Admin to add or remove any player to or from any game's roster or waitlist.

#### Scenario: Admin adds a player
- **WHEN** an Admin adds a player to a game
- **THEN** the system places the player on the roster if space remains, otherwise on the waitlist

#### Scenario: Admin removes a player
- **WHEN** an Admin removes a player from a game
- **THEN** the system removes them and the spot becomes open
