# payment-tracking

## Purpose
TBD - defines how per-game fees are charged, waived, paid, and tracked as player balances.

## Requirements

### Requirement: Flat per-game fee
The system SHALL apply a single configurable per-game fee that is the same for every game.

#### Scenario: Fee applied uniformly
- **WHEN** a charge is created for any game
- **THEN** the charge amount equals the configured per-game fee

### Requirement: Charge attaches on sign-up
The system SHALL create a charge for the per-game fee against a player when they sign up for a game.

#### Scenario: Sign-up creates a charge
- **WHEN** a Player signs up for a game (roster or waitlist)
- **THEN** the system records a charge for the per-game fee against that player for that game

### Requirement: Cancellation erases the charge
The system SHALL remove a player's charge for a game, with no penalty, when they cancel before game time.

#### Scenario: Cancel before game removes charge
- **WHEN** a Player cancels their sign-up before the game's start time
- **THEN** the system removes the associated charge and the player owes nothing for that game

### Requirement: Charge is owed if on roster at game time
The system SHALL treat a charge as owed for any player who is on the game roster at the game's start time.

#### Scenario: Rostered at tip-off
- **WHEN** the game's start time passes and a player is on the roster
- **THEN** the player's charge for that game stands as owed

### Requirement: Admin post-game waiver (attendance reconciliation)
The system SHALL allow an Admin to remove (waive) a charge after the game for a player who was on the roster but did not attend, serving as attendance reconciliation.

#### Scenario: Admin waives a no-show
- **WHEN** an Admin removes the charge for a rostered player who did not show
- **THEN** the system waives that charge and the player no longer owes for that game

### Requirement: Payment status and balance
The system SHALL let an Admin mark a charge as paid and SHALL maintain each player's running balance of owed-but-unpaid charges.

#### Scenario: Admin marks a charge paid
- **WHEN** an Admin marks a player's charge as paid
- **THEN** the system records it as paid and reduces the player's outstanding balance accordingly

### Requirement: Balance never blocks sign-up
The system SHALL treat outstanding balances as informational only and SHALL NOT prevent a player from signing up for future games because of an unpaid balance.

#### Scenario: Player with a balance signs up
- **WHEN** a player with an outstanding balance signs up for the live game
- **THEN** the system allows the sign-up and does not block it on the basis of the balance
